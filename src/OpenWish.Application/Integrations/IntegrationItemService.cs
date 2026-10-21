using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OpenWish.Data;
using OpenWish.Data.Entities;
using OpenWish.Shared.Integrations;
using OpenWish.Shared.Models;
using OpenWish.Shared.Services;

namespace OpenWish.Application.Integrations;

public class IntegrationItemService(
    IDbContextFactory<ApplicationDbContext> contextFactory,
    IWishlistService wishlistService,
    IActivityService activityService,
    ILogger<IntegrationItemService> logger) : IIntegrationItemService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory = contextFactory;
    private readonly IWishlistService _wishlistService = wishlistService;
    private readonly IActivityService _activityService = activityService;
    private readonly ILogger<IntegrationItemService> _logger = logger;

    public async Task<IntegrationProfileModel> GetProfileAsync(IntegrationCaller caller)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var connection = await context.IntegrationConnections
            .AsNoTracking()
            .Include(c => c.User)
            .FirstAsync(c => c.Id == caller.ConnectionId);

        return new IntegrationProfileModel
        {
            ConnectionId = caller.ConnectionPublicId,
            App = new IntegrationProfileAppModel(caller.ClientId, caller.AppName),
            User = new IntegrationProfileUserModel(PublicDisplayName.From(connection.User?.UserName, "OpenWish member")),
            ConnectedOn = connection.TokenIssuedOn ?? connection.CreatedOn,
            Wishlists = await GetWishlistsAsync(context, caller)
        };
    }

    public async Task<IReadOnlyList<IntegrationWishlistModel>> GetWishlistsAsync(IntegrationCaller caller)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await GetWishlistsAsync(context, caller);
    }

    public async Task<IReadOnlyList<IntegrationItemModel>> GetItemsAsync(IntegrationCaller caller, string? wishlistId, string? externalId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var allowed = await GetAllowedWishlistsAsync(context, caller);
        var query = OwnedItems(context, caller, allowed.Keys);

        if (!string.IsNullOrWhiteSpace(wishlistId))
        {
            var match = allowed.Values.FirstOrDefault(w => w.PublicId == wishlistId);
            if (match is null)
            {
                return [];
            }

            query = query.Where(i => i.WishlistId == match.Id);
        }

        if (!string.IsNullOrWhiteSpace(externalId))
        {
            var normalized = externalId.Trim();
            query = query.Where(i => i.ExternalId == normalized);
        }

        var items = await query
            .OrderBy(i => i.CreatedOn)
            .ToListAsync();
        return items.Select(item => ToModel(item, allowed[item.WishlistId])).ToList();
    }

    public async Task<IntegrationItemModel?> GetItemAsync(IntegrationCaller caller, string itemId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var allowed = await GetAllowedWishlistsAsync(context, caller);
        var item = await OwnedItems(context, caller, allowed.Keys)
            .FirstOrDefaultAsync(i => i.PublicId == itemId);

        return item is null ? null : ToModel(item, allowed[item.WishlistId]);
    }

    public async Task<IntegrationWriteResult> CreateItemAsync(IntegrationCaller caller, IntegrationItemRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        await using var context = await _contextFactory.CreateDbContextAsync();
        var externalId = NormalizeOptional(request.ExternalId);
        if (externalId is not null)
        {
            await ReleaseExternalIdFromDeletedListsAsync(context, caller, externalId);
            var existingId = await FindActiveExternalItemIdAsync(context, caller, externalId);
            if (existingId is not null)
            {
                return new(IntegrationWriteStatus.DuplicateExternalId, ExistingItemId: existingId);
            }
        }

        return await CreateAsync(context, caller, request, externalId);
    }

    public async Task<IntegrationWriteResult> UpdateItemAsync(IntegrationCaller caller, string itemId, IntegrationItemRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        await using var context = await _contextFactory.CreateDbContextAsync();
        var allowed = await GetAllowedWishlistsAsync(context, caller);
        var item = await OwnedItems(context, caller, allowed.Keys)
            .FirstOrDefaultAsync(i => i.PublicId == itemId);

        return item is null
            ? new(IntegrationWriteStatus.NotFound)
            : await UpdateAsync(context, caller, item, allowed, request);
    }

    public async Task<IntegrationWriteResult> UpsertItemByExternalIdAsync(
        IntegrationCaller caller,
        string externalId,
        IntegrationItemRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var normalized = NormalizeOptional(externalId);
        if (normalized is null || normalized.Length > IntegrationItemRequest.ExternalIdMaxLength)
        {
            return new(IntegrationWriteStatus.NotFound);
        }

        await using var context = await _contextFactory.CreateDbContextAsync();
        await ReleaseExternalIdFromDeletedListsAsync(context, caller, normalized);
        var item = await context.WishlistItems
            .FirstOrDefaultAsync(i => i.SourceConnectionId == caller.ConnectionId && i.ExternalId == normalized && !i.Deleted);

        if (item is null)
        {
            var created = await CreateAsync(context, caller, request, normalized);
            if (created.Status != IntegrationWriteStatus.DuplicateExternalId)
            {
                return created;
            }

            // Another request created the same idea first; apply this one as an update instead.
            await using var retryContext = await _contextFactory.CreateDbContextAsync();
            item = await retryContext.WishlistItems
                .FirstAsync(i => i.SourceConnectionId == caller.ConnectionId && i.ExternalId == normalized && !i.Deleted);
            var retryAllowed = await GetAllowedWishlistsAsync(retryContext, caller);
            return retryAllowed.ContainsKey(item.WishlistId)
                ? await UpdateAsync(retryContext, caller, item, retryAllowed, request)
                : new(IntegrationWriteStatus.WishlistNotAllowed);
        }

        var allowed = await GetAllowedWishlistsAsync(context, caller);
        return allowed.ContainsKey(item.WishlistId)
            ? await UpdateAsync(context, caller, item, allowed, request)
            : new(IntegrationWriteStatus.WishlistNotAllowed);
    }

    public async Task<bool> DeleteItemAsync(IntegrationCaller caller, string itemId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var allowed = await GetAllowedWishlistsAsync(context, caller);
        var item = await OwnedItems(context, caller, allowed.Keys)
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.PublicId == itemId);

        return item is not null && await RemoveAsync(caller, item);
    }

    public async Task<bool> DeleteItemByExternalIdAsync(IntegrationCaller caller, string externalId)
    {
        var normalized = NormalizeOptional(externalId);
        if (normalized is null)
        {
            return false;
        }

        await using var context = await _contextFactory.CreateDbContextAsync();
        var allowed = await GetAllowedWishlistsAsync(context, caller);
        var item = await OwnedItems(context, caller, allowed.Keys)
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.ExternalId == normalized);

        return item is not null && await RemoveAsync(caller, item);
    }

    private async Task<IntegrationWriteResult> CreateAsync(
        ApplicationDbContext context,
        IntegrationCaller caller,
        IntegrationItemRequest request,
        string? externalId)
    {
        var allowed = await GetAllowedWishlistsAsync(context, caller);
        var target = ResolveTargetWishlist(allowed, request.WishlistId);
        if (target is null)
        {
            return new(IntegrationWriteStatus.WishlistNotAllowed);
        }

        var now = DateTimeOffset.UtcNow;
        var item = new WishlistItem
        {
            Name = request.Name!.Trim(),
            WishlistId = target.Id,
            SourceConnectionId = caller.ConnectionId,
            SourceAppName = caller.AppName,
            ExternalId = externalId,
            Priority = request.Priority,
            OrderIndex = await NextOrderIndexAsync(context, target.Id),
            CreatedOn = now,
            UpdatedOn = now
        };
        ApplyContent(item, request);
        context.WishlistItems.Add(item);

        try
        {
            await context.SaveChangesAsync();
        }
        catch (DbUpdateException) when (externalId is not null)
        {
            // A concurrent request created the same external idea first.
            await using var reconciliation = await _contextFactory.CreateDbContextAsync();
            var existingId = await FindActiveExternalItemIdAsync(reconciliation, caller, externalId);
            if (existingId is null)
            {
                throw;
            }

            return new(IntegrationWriteStatus.DuplicateExternalId, ExistingItemId: existingId);
        }

        await _activityService.LogActivityAsync(
            caller.UserId,
            "ItemAdded",
            $"Added item: {item.Name} to wishlist {target.Name} via {caller.AppName}",
            target.Id,
            item.Id);
        _logger.LogInformation("Connected app {ClientId} added an idea for user {UserId}.", caller.ClientId, caller.UserId);

        return new(IntegrationWriteStatus.Created, ToModel(item, target));
    }

    private async Task<IntegrationWriteResult> UpdateAsync(
        ApplicationDbContext context,
        IntegrationCaller caller,
        WishlistItem item,
        IReadOnlyDictionary<int, AllowedWishlist> allowed,
        IntegrationItemRequest request)
    {
        var target = allowed[item.WishlistId];
        if (!string.IsNullOrWhiteSpace(request.WishlistId) && request.WishlistId != target.PublicId)
        {
            var moveTo = allowed.Values.FirstOrDefault(w => w.PublicId == request.WishlistId);
            if (moveTo is null)
            {
                return new(IntegrationWriteStatus.WishlistNotAllowed);
            }

            item.WishlistId = moveTo.Id;
            item.OrderIndex = await NextOrderIndexAsync(context, moveTo.Id);
            item.IsPinned = false;
            target = moveTo;
        }

        item.Name = request.Name!.Trim();
        ApplyContent(item, request);
        if (request.Priority.HasValue)
        {
            item.Priority = request.Priority;
        }

        item.SourceAppName = caller.AppName;
        item.UpdatedOn = DateTimeOffset.UtcNow;
        await context.SaveChangesAsync();

        await _activityService.LogActivityAsync(
            caller.UserId,
            "ItemUpdated",
            $"Updated item: {item.Name} in wishlist {target.Name} via {caller.AppName}",
            target.Id,
            item.Id);

        return new(IntegrationWriteStatus.Updated, ToModel(item, target));
    }

    private async Task<bool> RemoveAsync(IntegrationCaller caller, WishlistItem item)
    {
        // Reuse the owner's removal so reservations, comments, and reactions are retired consistently.
        var removed = await _wishlistService.RemoveItemFromWishlistAsync(item.WishlistId, item.Id);
        if (removed)
        {
            _logger.LogInformation("Connected app {ClientId} removed an idea for user {UserId}.", caller.ClientId, caller.UserId);
        }

        return removed;
    }

    private static void ApplyContent(WishlistItem item, IntegrationItemRequest request)
    {
        item.Description = NormalizeOptional(request.Description);
        item.Url = NormalizeOptional(request.Url);
        item.Image = NormalizeOptional(request.ImageUrl);
        item.Price = request.Price;
        item.WhereToBuy = NormalizeOptional(request.WhereToBuy);
    }

    private static IQueryable<WishlistItem> OwnedItems(
        ApplicationDbContext context,
        IntegrationCaller caller,
        IEnumerable<int> allowedWishlistIds)
    {
        var allowedIds = allowedWishlistIds.ToList();
        return context.WishlistItems.Where(i =>
            i.SourceConnectionId == caller.ConnectionId &&
            !i.Deleted &&
            allowedIds.Contains(i.WishlistId));
    }

    private static Task<string?> FindActiveExternalItemIdAsync(ApplicationDbContext context, IntegrationCaller caller, string externalId) =>
        context.WishlistItems
            .Where(i => i.SourceConnectionId == caller.ConnectionId && i.ExternalId == externalId && !i.Deleted)
            .Select(i => i.PublicId)
            .FirstOrDefaultAsync();

    /// <summary>
    /// Deleting a wishlist leaves its ideas in place, so an idea on a deleted list would otherwise hold the
    /// app's external ID forever. Detach the ID so the app can add the idea again on a live list.
    /// </summary>
    private static async Task ReleaseExternalIdFromDeletedListsAsync(ApplicationDbContext context, IntegrationCaller caller, string externalId)
    {
        var orphaned = await context.WishlistItems
            .Where(i =>
                i.SourceConnectionId == caller.ConnectionId &&
                i.ExternalId == externalId &&
                !i.Deleted &&
                i.Wishlist!.Deleted)
            .ToListAsync();
        if (orphaned.Count == 0)
        {
            return;
        }

        foreach (var item in orphaned)
        {
            item.ExternalId = null;
            item.UpdatedOn = DateTimeOffset.UtcNow;
        }

        await context.SaveChangesAsync();
    }

    private static async Task<int> NextOrderIndexAsync(ApplicationDbContext context, int wishlistId)
    {
        var maxOrderIndex = await context.WishlistItems
            .Where(i => i.WishlistId == wishlistId && !i.Deleted)
            .MaxAsync(i => i.OrderIndex);
        return (maxOrderIndex ?? -1) + 1;
    }

    private static AllowedWishlist? ResolveTargetWishlist(
        IReadOnlyDictionary<int, AllowedWishlist> allowed,
        string? requestedWishlistId) =>
        !string.IsNullOrWhiteSpace(requestedWishlistId)
            ? allowed.Values.FirstOrDefault(w => w.PublicId == requestedWishlistId)
            : allowed.Values.FirstOrDefault(w => w.IsDefault);

    private static async Task<Dictionary<int, AllowedWishlist>> GetAllowedWishlistsAsync(ApplicationDbContext context, IntegrationCaller caller)
    {
        var defaultId = await context.IntegrationConnections
            .Where(c => c.Id == caller.ConnectionId)
            .Select(c => c.DefaultWishlistId)
            .FirstOrDefaultAsync();

        var wishlists = await context.IntegrationConnectionWishlists
            .AsNoTracking()
            .Where(a => a.IntegrationConnectionId == caller.ConnectionId)
            .Select(a => a.Wishlist!)
            .Where(w => w.OwnerId == caller.UserId && !w.Deleted)
            .OrderBy(w => w.Id)
            .Select(w => new { w.Id, w.PublicId, w.Name, w.Icon })
            .ToListAsync();

        // If the person deleted the default list, new ideas fall back to the oldest list still allowed.
        var effectiveDefaultId = wishlists.Any(w => w.Id == defaultId) ? defaultId : wishlists.FirstOrDefault()?.Id;
        return wishlists.ToDictionary(
            w => w.Id,
            w => new AllowedWishlist(w.Id, w.PublicId, w.Name, w.Icon, w.Id == effectiveDefaultId));
    }

    private static async Task<IReadOnlyList<IntegrationWishlistModel>> GetWishlistsAsync(ApplicationDbContext context, IntegrationCaller caller)
    {
        var allowed = await GetAllowedWishlistsAsync(context, caller);
        return allowed.Values
            .OrderByDescending(w => w.IsDefault)
            .ThenBy(w => w.Name, StringComparer.CurrentCultureIgnoreCase)
            .Select(w => new IntegrationWishlistModel
            {
                Id = w.PublicId,
                Name = w.Name,
                Icon = w.Icon,
                IsDefault = w.IsDefault,
                Url = WishlistPath(w.PublicId)
            })
            .ToList();
    }

    private static IntegrationItemModel ToModel(WishlistItem item, AllowedWishlist wishlist) => new()
    {
        Id = item.PublicId,
        ExternalId = item.ExternalId,
        WishlistId = wishlist.PublicId,
        WishlistName = wishlist.Name,
        Name = item.Name,
        Description = item.Description,
        Url = item.Url,
        ImageUrl = item.Image,
        Price = item.Price,
        WhereToBuy = item.WhereToBuy,
        Priority = item.Priority,
        CreatedOn = item.CreatedOn,
        UpdatedOn = item.UpdatedOn,
        OpenWishUrl = WishlistPath(wishlist.PublicId)
    };

    /// <summary>Relative to the OpenWish base address; the API layer makes it absolute.</summary>
    private static string WishlistPath(string wishlistPublicId) => $"wishlists/{Uri.EscapeDataString(wishlistPublicId)}";

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private sealed record AllowedWishlist(int Id, string PublicId, string Name, string? Icon, bool IsDefault);
}