using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OpenWish.Data;
using OpenWish.Data.Entities;
using OpenWish.Shared.Models;
using OpenWish.Shared.Services;

namespace OpenWish.Application.Integrations;

public class IntegrationConnectionService(
    IDbContextFactory<ApplicationDbContext> contextFactory,
    IntegrationAppRegistry registry,
    IWishlistService wishlistService,
    ILogger<IntegrationConnectionService> logger) : IIntegrationConnectionService
{
    public static readonly TimeSpan AuthorizationCodeLifetime = TimeSpan.FromMinutes(5);
    public const int NewWishlistNameMaxLength = 100;
    private static readonly TimeSpan _lastUsedPrecision = TimeSpan.FromMinutes(1);

    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory = contextFactory;
    private readonly IntegrationAppRegistry _registry = registry;
    private readonly IWishlistService _wishlistService = wishlistService;
    private readonly ILogger<IntegrationConnectionService> _logger = logger;

    public async Task<ConsentState> GetConsentStateAsync(string userId, string clientId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ArgumentException.ThrowIfNullOrWhiteSpace(clientId);

        await using var context = await _contextFactory.CreateDbContextAsync();
        var wishlists = await context.Wishlists
            .AsNoTracking()
            .Where(w => w.OwnerId == userId && !w.Deleted)
            .OrderByDescending(w => w.UpdatedOn)
            .Select(w => new
            {
                w.Id,
                w.PublicId,
                w.Name,
                w.Icon,
                EventName = w.Event != null && !w.Event.Deleted ? w.Event.Name : null,
                ItemCount = w.Items!.Count(i => !i.Deleted && !i.IsHiddenFromOwner)
            })
            .ToListAsync();

        var connection = await context.IntegrationConnections
            .AsNoTracking()
            .Include(c => c.AllowedWishlists)
            .FirstOrDefaultAsync(c =>
                c.UserId == userId &&
                c.ClientId == clientId &&
                c.RevokedOn == null &&
                c.AccessTokenHash != null &&
                !c.Deleted);

        var allowedIds = connection?.AllowedWishlists.Select(a => a.WishlistId).ToHashSet() ?? [];
        var options = wishlists
            .Select(w => new ConsentWishlistOption(
                w.PublicId,
                w.Name,
                w.Icon,
                w.ItemCount,
                allowedIds.Contains(w.Id),
                connection?.DefaultWishlistId == w.Id,
                w.EventName))
            .ToList();

        return new ConsentState(options, connection is not null);
    }

    public async Task<AuthorizationApprovalResult> ApproveAsync(string userId, AuthorizationApproval approval)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ArgumentNullException.ThrowIfNull(approval);

        var newWishlistName = approval.NewWishlistName?.Trim();
        if (approval.NewWishlistName is not null &&
            (string.IsNullOrEmpty(newWishlistName) || newWishlistName.Length > NewWishlistNameMaxLength))
        {
            return new(null, AuthorizationApprovalError.InvalidNewWishlistName);
        }

        var requestedIds = approval.WishlistPublicIds
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (requestedIds.Count == 0 && string.IsNullOrEmpty(newWishlistName))
        {
            return new(null, AuthorizationApprovalError.NoWishlistSelected);
        }

        await using var context = await _contextFactory.CreateDbContextAsync();
        var selected = await context.Wishlists
            .AsNoTracking()
            .Where(w => w.OwnerId == userId && !w.Deleted && requestedIds.Contains(w.PublicId))
            .Select(w => new { w.Id, w.PublicId })
            .ToListAsync();

        if (selected.Count != requestedIds.Count)
        {
            return new(null, AuthorizationApprovalError.WishlistNotFound);
        }

        var grantedIds = requestedIds
            .Select(publicId => selected.First(w => w.PublicId == publicId).Id)
            .ToList();
        var defaultId = selected.FirstOrDefault(w => w.PublicId == approval.DefaultWishlistPublicId)?.Id;

        if (!string.IsNullOrEmpty(newWishlistName))
        {
            // New lists start private, matching the new-wishlist page.
            var created = await _wishlistService.CreateWishlistAsync(
                new WishlistModel { Name = newWishlistName, IsPrivate = true },
                userId);
            grantedIds.Insert(0, created.Id);
            defaultId ??= created.Id;
        }

        var now = DateTimeOffset.UtcNow;
        var code = IntegrationTokens.NewAuthorizationCode();
        context.IntegrationAuthorizationCodes.Add(new IntegrationAuthorizationCode
        {
            CodeHash = IntegrationTokens.Hash(code),
            UserId = userId,
            ClientId = approval.ClientId,
            RedirectUri = approval.RedirectUri,
            CodeChallenge = string.IsNullOrWhiteSpace(approval.CodeChallenge) ? null : approval.CodeChallenge,
            GrantedWishlistIds = string.Join(',', grantedIds),
            DefaultWishlistId = defaultId ?? grantedIds[0],
            CreatedOn = now,
            ExpiresOn = now.Add(AuthorizationCodeLifetime)
        });
        await context.SaveChangesAsync();

        if (context.Database.IsRelational())
        {
            var staleBefore = now.AddDays(-1);
            await context.IntegrationAuthorizationCodes
                .Where(c => c.ExpiresOn < staleBefore)
                .ExecuteDeleteAsync();
        }

        return new(code, AuthorizationApprovalError.None);
    }

    public async Task<TokenExchangeResult> ExchangeCodeAsync(
        IntegrationApp app,
        string? code,
        string? redirectUri,
        string? codeVerifier)
    {
        ArgumentNullException.ThrowIfNull(app);

        if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(redirectUri))
        {
            return TokenExchangeResult.Failed("invalid_request", "code and redirect_uri are required.");
        }

        await using var context = await _contextFactory.CreateDbContextAsync();
        var codeHash = IntegrationTokens.Hash(code);
        var grant = await context.IntegrationAuthorizationCodes
            .FirstOrDefaultAsync(c => c.CodeHash == codeHash);

        if (grant is null || !string.Equals(grant.ClientId, app.ClientId, StringComparison.Ordinal))
        {
            return TokenExchangeResult.Failed("invalid_grant", "The authorization code is not valid.");
        }

        if (grant.ConsumedOn is { } consumedOn)
        {
            await RevokeAfterCodeReplayAsync(context, grant, consumedOn);
            return TokenExchangeResult.Failed("invalid_grant", "The authorization code was already used.");
        }

        var now = DateTimeOffset.UtcNow;
        if (grant.ExpiresOn < now)
        {
            return TokenExchangeResult.Failed("invalid_grant", "The authorization code has expired.");
        }

        if (!string.Equals(grant.RedirectUri, redirectUri, StringComparison.Ordinal))
        {
            return TokenExchangeResult.Failed("invalid_grant", "redirect_uri does not match the authorization request.");
        }

        if (grant.CodeChallenge is not null && !IntegrationTokens.VerifyPkce(codeVerifier, grant.CodeChallenge))
        {
            return TokenExchangeResult.Failed("invalid_grant", "code_verifier does not match the code challenge.");
        }

        if (!await TryConsumeAsync(context, grant, now))
        {
            return TokenExchangeResult.Failed("invalid_grant", "The authorization code was already used.");
        }

        var grantedIds = grant.GrantedWishlistIds
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(int.Parse)
            .ToList();
        var stillOwned = await context.Wishlists
            .Where(w => w.OwnerId == grant.UserId && !w.Deleted && grantedIds.Contains(w.Id))
            .Select(w => w.Id)
            .ToListAsync();
        var allowedIds = grantedIds.Where(stillOwned.Contains).ToList();
        if (allowedIds.Count == 0)
        {
            return TokenExchangeResult.Failed("invalid_grant", "The wishlists chosen for this app are no longer available.");
        }

        var accessToken = IntegrationTokens.NewAccessToken();
        var connection = await context.IntegrationConnections
            .Include(c => c.AllowedWishlists)
            .FirstOrDefaultAsync(c => c.UserId == grant.UserId && c.ClientId == app.ClientId);

        if (connection is null)
        {
            connection = new IntegrationConnection
            {
                UserId = grant.UserId,
                ClientId = app.ClientId,
                AppName = app.DisplayName,
                CreatedOn = now
            };
            context.IntegrationConnections.Add(connection);
        }

        connection.AppName = app.DisplayName;
        connection.AccessTokenHash = IntegrationTokens.Hash(accessToken);
        connection.TokenIssuedOn = now;
        connection.RevokedOn = null;
        connection.Deleted = false;
        connection.UpdatedOn = now;
        connection.DefaultWishlistId = allowedIds.Contains(grant.DefaultWishlistId) ? grant.DefaultWishlistId : allowedIds[0];
        connection.AllowedWishlists.Clear();
        foreach (var wishlistId in allowedIds)
        {
            connection.AllowedWishlists.Add(new IntegrationConnectionWishlist { WishlistId = wishlistId });
        }

        await context.SaveChangesAsync();

        _logger.LogInformation(
            "Connected app {ClientId} was authorized for user {UserId} with {Count} wishlist(s).",
            app.ClientId,
            grant.UserId,
            allowedIds.Count);

        return new TokenExchangeResult(accessToken, connection.PublicId, null, null);
    }

    public async Task RevokeTokenAsync(IntegrationApp app, string? token)
    {
        ArgumentNullException.ThrowIfNull(app);
        if (string.IsNullOrEmpty(token))
        {
            return;
        }

        var tokenHash = IntegrationTokens.Hash(token);
        await using var context = await _contextFactory.CreateDbContextAsync();
        var connection = await context.IntegrationConnections
            .FirstOrDefaultAsync(c => c.AccessTokenHash == tokenHash && c.ClientId == app.ClientId);

        if (connection is null)
        {
            return;
        }

        Revoke(connection, DateTimeOffset.UtcNow);
        await context.SaveChangesAsync();
        _logger.LogInformation("Connected app {ClientId} revoked its access for user {UserId}.", app.ClientId, connection.UserId);
    }

    public async Task<IntegrationCaller?> AuthenticateAsync(string? token)
    {
        if (string.IsNullOrEmpty(token) || !token.StartsWith(IntegrationTokens.AccessTokenPrefix, StringComparison.Ordinal))
        {
            return null;
        }

        var tokenHash = IntegrationTokens.Hash(token);
        await using var context = await _contextFactory.CreateDbContextAsync();
        var connection = await context.IntegrationConnections
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.AccessTokenHash == tokenHash && c.RevokedOn == null && !c.Deleted);

        var app = _registry.Find(connection?.ClientId);
        if (connection is null || app is null)
        {
            return null;
        }

        var now = DateTimeOffset.UtcNow;
        if (connection.LastUsedOn is null || now - connection.LastUsedOn > _lastUsedPrecision)
        {
            if (context.Database.IsRelational())
            {
                await context.IntegrationConnections
                    .Where(c => c.Id == connection.Id)
                    .ExecuteUpdateAsync(setters => setters.SetProperty(c => c.LastUsedOn, now));
            }
            else
            {
                var tracked = await context.IntegrationConnections.FirstAsync(c => c.Id == connection.Id);
                tracked.LastUsedOn = now;
                await context.SaveChangesAsync();
            }
        }

        return new IntegrationCaller(connection.Id, connection.PublicId, connection.UserId, app.ClientId, app.DisplayName);
    }

    public async Task<IReadOnlyList<ConnectedAppSummary>> GetConnectedAppsAsync(string userId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        await using var context = await _contextFactory.CreateDbContextAsync();
        var connections = await context.IntegrationConnections
            .AsNoTracking()
            .Include(c => c.AllowedWishlists)
                .ThenInclude(a => a.Wishlist)
            .Where(c => c.UserId == userId && c.RevokedOn == null && c.AccessTokenHash != null && !c.Deleted)
            .OrderBy(c => c.AppName)
            .ToListAsync();

        if (connections.Count == 0)
        {
            return [];
        }

        var connectionIds = connections.Select(c => c.Id).ToList();
        var ideaCounts = await context.WishlistItems
            .AsNoTracking()
            .Where(i => i.SourceConnectionId != null && connectionIds.Contains(i.SourceConnectionId.Value) && !i.Deleted && !i.Wishlist!.Deleted)
            .GroupBy(i => i.SourceConnectionId!.Value)
            .Select(g => new { ConnectionId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.ConnectionId, g => g.Count);

        return connections
            .Select(connection =>
            {
                var app = _registry.Find(connection.ClientId);
                var wishlists = connection.AllowedWishlists
                    .Where(a => a.Wishlist is { Deleted: false })
                    .Select(a => new ConnectedAppWishlist(
                        a.Wishlist!.PublicId,
                        a.Wishlist.Name,
                        a.Wishlist.Icon,
                        a.WishlistId == connection.DefaultWishlistId))
                    .OrderByDescending(w => w.IsDefault)
                    .ThenBy(w => w.Name, StringComparer.CurrentCultureIgnoreCase)
                    .ToList();

                return new ConnectedAppSummary(
                    connection.PublicId,
                    connection.ClientId,
                    app?.DisplayName ?? connection.AppName,
                    app?.Description,
                    app?.HomepageUrl,
                    app is not null,
                    connection.TokenIssuedOn ?? connection.CreatedOn,
                    connection.LastUsedOn,
                    ideaCounts.GetValueOrDefault(connection.Id),
                    wishlists);
            })
            .ToList();
    }

    public async Task<bool> UpdateAllowedWishlistsAsync(
        string userId,
        string connectionPublicId,
        IReadOnlyCollection<string> wishlistPublicIds,
        string? defaultWishlistPublicId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ArgumentNullException.ThrowIfNull(wishlistPublicIds);

        var requestedIds = wishlistPublicIds
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.Ordinal)
            .ToList();
        if (requestedIds.Count == 0)
        {
            return false;
        }

        await using var context = await _contextFactory.CreateDbContextAsync();
        var connection = await context.IntegrationConnections
            .Include(c => c.AllowedWishlists)
            .FirstOrDefaultAsync(c =>
                c.PublicId == connectionPublicId &&
                c.UserId == userId &&
                c.RevokedOn == null &&
                c.AccessTokenHash != null &&
                !c.Deleted);
        if (connection is null)
        {
            return false;
        }

        var selected = await context.Wishlists
            .Where(w => w.OwnerId == userId && !w.Deleted && requestedIds.Contains(w.PublicId))
            .Select(w => new { w.Id, w.PublicId })
            .ToListAsync();
        if (selected.Count != requestedIds.Count)
        {
            return false;
        }

        connection.AllowedWishlists.Clear();
        foreach (var wishlist in selected)
        {
            connection.AllowedWishlists.Add(new IntegrationConnectionWishlist { WishlistId = wishlist.Id });
        }

        connection.DefaultWishlistId = selected.FirstOrDefault(w => w.PublicId == defaultWishlistPublicId)?.Id
            ?? selected.First(w => w.PublicId == requestedIds[0]).Id;
        connection.UpdatedOn = DateTimeOffset.UtcNow;
        await context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DisconnectAsync(string userId, string connectionPublicId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        await using var context = await _contextFactory.CreateDbContextAsync();
        var connection = await context.IntegrationConnections
            .FirstOrDefaultAsync(c =>
                c.PublicId == connectionPublicId &&
                c.UserId == userId &&
                c.RevokedOn == null &&
                !c.Deleted);
        if (connection is null)
        {
            return false;
        }

        Revoke(connection, DateTimeOffset.UtcNow);
        await context.SaveChangesAsync();
        _logger.LogInformation("User {UserId} disconnected app {ClientId}.", userId, connection.ClientId);
        return true;
    }

    private static void Revoke(IntegrationConnection connection, DateTimeOffset now)
    {
        connection.AccessTokenHash = null;
        connection.RevokedOn = now;
        connection.UpdatedOn = now;
    }

    private static async Task<bool> TryConsumeAsync(ApplicationDbContext context, IntegrationAuthorizationCode grant, DateTimeOffset now)
    {
        if (context.Database.IsRelational())
        {
            var consumed = await context.IntegrationAuthorizationCodes
                .Where(c => c.Id == grant.Id && c.ConsumedOn == null)
                .ExecuteUpdateAsync(setters => setters.SetProperty(c => c.ConsumedOn, now));
            return consumed == 1;
        }

        grant.ConsumedOn = now;
        await context.SaveChangesAsync();
        return true;
    }

    // RFC 6749 section 4.1.2: a replayed code should revoke the token it produced.
    private async Task RevokeAfterCodeReplayAsync(
        ApplicationDbContext context,
        IntegrationAuthorizationCode grant,
        DateTimeOffset consumedOn)
    {
        var connection = await context.IntegrationConnections
            .FirstOrDefaultAsync(c =>
                c.UserId == grant.UserId &&
                c.ClientId == grant.ClientId &&
                c.TokenIssuedOn == consumedOn &&
                c.RevokedOn == null);

        if (connection is null)
        {
            return;
        }

        Revoke(connection, DateTimeOffset.UtcNow);
        await context.SaveChangesAsync();
        _logger.LogWarning(
            "An authorization code for connected app {ClientId} was replayed; the connection for user {UserId} was revoked.",
            grant.ClientId,
            grant.UserId);
    }
}