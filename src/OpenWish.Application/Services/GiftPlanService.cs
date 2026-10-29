using Microsoft.EntityFrameworkCore;
using OpenWish.Data;
using OpenWish.Shared.Models;
using OpenWish.Shared.Services;

namespace OpenWish.Application.Services;

public class GiftPlanService(IDbContextFactory<ApplicationDbContext> contextFactory) : IGiftPlanService
{
    private const int HistoryAfterDays = 30;
    private const string SharedListName = "Shared event list";

    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory = contextFactory;

    public async Task<GiftPlanModel> GetGiftPlanAsync(string userId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        await using var context = await _contextFactory.CreateDbContextAsync();
        var reservations = await context.ItemReservations
            .AsNoTracking()
            .Where(r => !r.Deleted &&
                        r.UserId == userId &&
                        r.WishlistItem != null &&
                        !r.WishlistItem.Deleted &&
                        !r.WishlistItem.IsPrivate &&
                        r.WishlistItem.Wishlist != null &&
                        !r.WishlistItem.Wishlist.Deleted &&
                        r.WishlistItem.Wishlist.OwnerId != userId)
            .Select(r => new
            {
                r.ReservationDate,
                r.PurchasedOn,
                r.WishlistItem!.ReceivedOn,
                EventDate = r.WishlistItem!.Wishlist!.Event != null && !r.WishlistItem.Wishlist.Event.Deleted
                    ? (DateTimeOffset?)r.WishlistItem.Wishlist.Event.Date : null,
                Item = r.WishlistItem!,
                Wishlist = r.WishlistItem!.Wishlist!,
                OwnerName = r.WishlistItem.Wishlist.Owner != null ? r.WishlistItem.Wishlist.Owner.UserName : null,
                // Event details are private to event members, like WishlistService.AttachEventSummary.
                EventName = r.WishlistItem.Wishlist.Event != null && !r.WishlistItem.Wishlist.Event.Deleted &&
                    (r.WishlistItem.Wishlist.Event.CreatedBy.Id == userId ||
                     r.WishlistItem.Wishlist.Event.EventUsers.Any(eu => !eu.Deleted && eu.Status == "Accepted" && eu.UserId == userId))
                    ? r.WishlistItem.Wishlist.Event.Name : null,
                EventPublicId = r.WishlistItem.Wishlist.Event != null && !r.WishlistItem.Wishlist.Event.Deleted &&
                    (r.WishlistItem.Wishlist.Event.CreatedBy.Id == userId ||
                     r.WishlistItem.Wishlist.Event.EventUsers.Any(eu => !eu.Deleted && eu.Status == "Accepted" && eu.UserId == userId))
                    ? r.WishlistItem.Wishlist.Event.PublicId : null
            })
            .ToListAsync();

        var accessible = await WishlistService.GetAccessibleWishlistIdsAsync(
            context, userId, reservations.Select(r => r.Wishlist.Id).Distinct().ToList());
        var visible = new List<(string OwnerKey, string RecipientName, GiftPlanItemModel Item)>();
        var archived = new List<(string OwnerKey, string RecipientName, GiftPlanItemModel Item)>();
        var now = DateTimeOffset.UtcNow;
        foreach (var row in reservations.OrderBy(r => r.ReservationDate))
        {
            if (!accessible.Contains(row.Wishlist.Id))
            {
                continue;
            }

            if (row.ReceivedOn is not null && row.PurchasedOn is null)
            {
                continue;
            }

            var isArchived = row.ReceivedOn is not null || row.PurchasedOn is { } boughtOn &&
                (row.EventDate is { } eventDate ? eventDate < now : boughtOn <= now.AddDays(-HistoryAfterDays));
            (isArchived ? archived : visible).Add((
                row.Wishlist.OwnerId,
                PublicDisplayName.From(row.OwnerName, SharedListName),
                new GiftPlanItemModel
                {
                    ItemPublicId = row.Item.PublicId,
                    ItemName = row.Item.Name,
                    Price = row.Item.Price,
                    Url = row.Item.Url,
                    Image = row.Item.Image,
                    WishlistName = row.Wishlist.Name,
                    WishlistPublicId = row.Wishlist.PublicId,
                    EventName = row.EventName,
                    EventPublicId = row.EventPublicId,
                    ReservedOn = row.ReservationDate,
                    PurchasedOn = row.PurchasedOn
                }));
        }

        var recipients = visible
            .GroupBy(entry => entry.OwnerKey)
            .Select(group => new GiftPlanRecipientModel
            {
                RecipientName = group.First().RecipientName,
                Items = group.Select(entry => entry.Item).ToList()
            })
            .OrderBy(recipient => recipient.RecipientName, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
        var history = archived
            .GroupBy(entry => entry.OwnerKey)
            .Select(group => new GiftPlanRecipientModel
            {
                RecipientName = group.First().RecipientName,
                Items = group.Select(entry => entry.Item).OrderByDescending(item => item.PurchasedOn).ToList()
            })
            .OrderBy(recipient => recipient.RecipientName, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        return new GiftPlanModel { Recipients = recipients, History = history };
    }

    public async Task<IReadOnlyList<GivenGiftMatchModel>> GetPreviouslyGivenMatchesAsync(string userId, string wishlistPublicId, int itemId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        await using var context = await _contextFactory.CreateDbContextAsync();
        var target = await context.WishlistItems
            .AsNoTracking()
            .Where(i => i.Id == itemId && !i.Deleted && !i.IsPrivate &&
                        i.Wishlist != null && !i.Wishlist.Deleted && i.Wishlist.PublicId == wishlistPublicId)
            .Select(i => new { i.Id, i.Name, i.Url, i.WishlistId, i.Wishlist!.OwnerId })
            .FirstOrDefaultAsync();
        if (target is null || target.OwnerId == userId ||
            !await WishlistService.CanUserAccessWishlistInternalAsync(context, target.WishlistId, userId))
        {
            return [];
        }

        var candidates = await context.ItemReservations
            .AsNoTracking()
            .Where(r => !r.Deleted &&
                        r.UserId == userId &&
                        r.PurchasedOn != null &&
                        r.WishlistItem != null &&
                        r.WishlistItem.Id != target.Id &&
                        !r.WishlistItem.Deleted &&
                        !r.WishlistItem.IsPrivate &&
                        r.WishlistItem.Wishlist != null &&
                        !r.WishlistItem.Wishlist.Deleted &&
                        r.WishlistItem.Wishlist.OwnerId == target.OwnerId)
            .Select(r => new
            {
                r.WishlistItem!.WishlistId,
                r.PurchasedOn,
                r.WishlistItem!.Name,
                r.WishlistItem.Url,
                OwnerName = r.WishlistItem.Wishlist!.Owner != null ? r.WishlistItem.Wishlist.Owner.UserName : null
            })
            .ToListAsync();

        var accessible = await WishlistService.GetAccessibleWishlistIdsAsync(
            context, userId, candidates.Select(c => c.WishlistId).Distinct().ToList());
        var targetUrl = NormalizeUrl(target.Url);
        return candidates
            .Where(c => accessible.Contains(c.WishlistId))
            .Where(c => targetUrl is not null
                ? NormalizeUrl(c.Url) == targetUrl
                : NormalizeUrl(c.Url) is null && string.Equals(c.Name.Trim(), target.Name.Trim(), StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(c => c.PurchasedOn)
            .Select(c => new GivenGiftMatchModel
            {
                RecipientName = PublicDisplayName.From(c.OwnerName, SharedListName),
                ItemName = c.Name,
                PurchasedOn = c.PurchasedOn!.Value
            })
            .ToList();
    }

    private static string? NormalizeUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return null;
        }

        var value = url.Trim().ToLowerInvariant();
        var hash = value.IndexOf('#');
        if (hash >= 0)
        {
            value = value[..hash];
        }

        foreach (var prefix in new[] { "https://", "http://" })
        {
            if (value.StartsWith(prefix, StringComparison.Ordinal))
            {
                value = value[prefix.Length..];
                break;
            }
        }

        if (value.StartsWith("www.", StringComparison.Ordinal))
        {
            value = value[4..];
        }

        value = value.TrimEnd('/');
        return value.Length == 0 ? null : value;
    }

    public async Task SetGiftPurchasedAsync(string userId, string wishlistPublicId, string itemPublicId, bool isPurchased)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        await using var context = await _contextFactory.CreateDbContextAsync();
        var reservation = await context.ItemReservations
            .Include(r => r.WishlistItem)
            .FirstOrDefaultAsync(r => !r.Deleted &&
                                      r.UserId == userId &&
                                      r.WishlistItem != null &&
                                      !r.WishlistItem.Deleted &&
                                      r.WishlistItem.PublicId == itemPublicId &&
                                      r.WishlistItem.Wishlist != null &&
                                      !r.WishlistItem.Wishlist.Deleted &&
                                      r.WishlistItem.Wishlist.PublicId == wishlistPublicId)
            ?? throw new KeyNotFoundException("You have not reserved that gift.");

        if (!await WishlistService.CanUserAccessWishlistInternalAsync(context, reservation.WishlistItem!.WishlistId, userId))
        {
            throw new KeyNotFoundException("You have not reserved that gift.");
        }

        reservation.PurchasedOn = isPurchased ? reservation.PurchasedOn ?? DateTimeOffset.UtcNow : null;
        await context.SaveChangesAsync();
    }
}