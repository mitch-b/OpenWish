using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using OpenWish.Data;
using OpenWish.Data.Entities;
using OpenWish.Shared.Models;
using OpenWish.Shared.Services;

namespace OpenWish.Application.Integrations;

public class IntegrationConnectionService(
    IDbContextFactory<ApplicationDbContext> contextFactory,
    IIntegrationClientStore clientStore,
    IWishlistService wishlistService,
    ILogger<IntegrationConnectionService> logger) : IIntegrationConnectionService
{
    public static readonly TimeSpan AuthorizationCodeLifetime = TimeSpan.FromMinutes(5);
    public static readonly TimeSpan McpAccessTokenLifetime = TimeSpan.FromHours(1);
    public const int NewWishlistNameMaxLength = 100;
    private static readonly TimeSpan _lastUsedPrecision = TimeSpan.FromMinutes(1);

    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory = contextFactory;
    private readonly IIntegrationClientStore _clientStore = clientStore;
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
                connection?.AllowAllWishlists == true || allowedIds.Contains(w.Id),
                connection?.DefaultWishlistId == w.Id,
                w.EventName))
            .ToList();

        return new ConsentState(options, connection is not null,
            connection?.AllowAllWishlists ?? false, connection?.ManageExistingItems ?? true);
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

        if (!approval.AllowAllWishlists && requestedIds.Count == 0 && string.IsNullOrEmpty(newWishlistName))
        {
            return new(null, AuthorizationApprovalError.NoWishlistSelected);
        }

        await using var context = await _contextFactory.CreateDbContextAsync();
        await using var transaction = await LockConnectionAsync(context, userId, approval.ClientId);
        var selected = await context.Wishlists
            .AsNoTracking()
            .Where(w => w.OwnerId == userId && !w.Deleted && (approval.AllowAllWishlists || requestedIds.Contains(w.PublicId)))
            .OrderBy(w => w.Id)
            .Select(w => new { w.Id, w.PublicId })
            .ToListAsync();

        if (requestedIds.Any(id => !selected.Any(w => w.PublicId == id)))
        {
            return new(null, AuthorizationApprovalError.WishlistNotFound);
        }

        var grantedIds = (approval.AllowAllWishlists ? selected.Select(w => w.PublicId) : requestedIds)
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
            defaultId = created.Id;
        }

        var now = DateTimeOffset.UtcNow;
        if (grantedIds.Count == 0)
        {
            return new(null, AuthorizationApprovalError.NoWishlistSelected);
        }

        var code = IntegrationTokens.NewAuthorizationCode();
        context.IntegrationAuthorizationCodes.Add(new IntegrationAuthorizationCode
        {
            CodeHash = IntegrationTokens.Hash(code),
            UserId = userId,
            ClientId = approval.ClientId,
            RedirectUri = approval.RedirectUri,
            CodeChallenge = string.IsNullOrWhiteSpace(approval.CodeChallenge) ? null : approval.CodeChallenge,
            Audience = approval.Audience,
            GrantedWishlistIds = string.Join(',', grantedIds),
            DefaultWishlistId = defaultId ?? grantedIds[0],
            AllowAllWishlists = approval.AllowAllWishlists,
            ManageExistingItems = approval.ManageExistingItems,
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

        if (transaction is not null)
        {
            await transaction.CommitAsync();
        }

        return new(code, AuthorizationApprovalError.None);
    }

    public async Task<TokenExchangeResult> ExchangeCodeAsync(
        IntegrationApp app,
        string? code,
        string? redirectUri,
        string? codeVerifier,
        IntegrationAudience? requestedAudience = null)
    {
        ArgumentNullException.ThrowIfNull(app);

        if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(redirectUri))
        {
            return TokenExchangeResult.Failed("invalid_request", "code and redirect_uri are required.");
        }

        await using var context = await _contextFactory.CreateDbContextAsync();
        var codeHash = IntegrationTokens.Hash(code);
        var grant = await context.IntegrationAuthorizationCodes
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.CodeHash == codeHash);

        if (grant is null || !string.Equals(grant.ClientId, app.ClientId, StringComparison.Ordinal))
        {
            return TokenExchangeResult.Failed("invalid_grant", "The authorization code is not valid.");
        }

        await using var transaction = await LockConnectionAsync(context, grant.UserId, app.ClientId);
        grant = await context.IntegrationAuthorizationCodes.FirstOrDefaultAsync(c => c.CodeHash == codeHash);
        if (grant is null)
        {
            return TokenExchangeResult.Failed("invalid_grant", "The authorization is no longer valid. Connect again.");
        }

        if (!string.Equals(grant.RedirectUri, redirectUri, StringComparison.Ordinal))
        {
            return TokenExchangeResult.Failed("invalid_grant", "redirect_uri does not match the authorization request.");
        }

        if (grant.CodeChallenge is not null && !IntegrationTokens.VerifyPkce(codeVerifier, grant.CodeChallenge))
        {
            return TokenExchangeResult.Failed("invalid_grant", "code_verifier does not match the code challenge.");
        }

        if (grant.CodeChallenge is null && (app.IsPublicClient || grant.Audience == IntegrationAudience.Mcp))
        {
            return TokenExchangeResult.Failed("invalid_grant", "This authorization requires PKCE.");
        }

        if (requestedAudience is not null && requestedAudience != grant.Audience)
        {
            return TokenExchangeResult.Failed("invalid_target", "resource does not match the authorization request.");
        }

        if (grant.ConsumedOn is { } consumedOn)
        {
            await RevokeAfterCodeReplayAsync(context, grant, consumedOn);
            if (transaction is not null)
            {
                await transaction.CommitAsync();
            }

            return TokenExchangeResult.Failed("invalid_grant", "The authorization code was already used.");
        }

        var now = DateTimeOffset.UtcNow;
        if (grant.ExpiresOn <= now)
        {
            return TokenExchangeResult.Failed("invalid_grant", "The authorization code has expired.");
        }

        grant.ConsumedOn = now;
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
        connection.Audience = grant.Audience;
        connection.AllowAllWishlists = grant.AllowAllWishlists;
        connection.ManageExistingItems = grant.ManageExistingItems;
        // A fresh authorization starts a new refresh-token family.
        connection.RefreshTokenHash = null;
        var tokens = IssueTokens(connection, now);
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

        var pending = await context.IntegrationAuthorizationCodes
            .Where(c => c.UserId == grant.UserId && c.ClientId == grant.ClientId && c.Id < grant.Id && c.ConsumedOn == null)
            .ToListAsync();
        var allowedSet = allowedIds.ToHashSet();
        context.IntegrationAuthorizationCodes.RemoveRange(pending.Where(c =>
            (c.AllowAllWishlists && !grant.AllowAllWishlists) ||
            (c.ManageExistingItems && !grant.ManageExistingItems) ||
            (!grant.AllowAllWishlists && !c.GrantedWishlistIds
                .Split(',', StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).All(allowedSet.Contains))));

        await context.SaveChangesAsync();
        if (app.IsSelfRegistered && context.Database.IsRelational())
        {
            await context.IntegrationClientRegistrations
                .Where(r => r.ClientId == app.ClientId)
                .ExecuteUpdateAsync(setters => setters.SetProperty(r => r.LastUsedOn, now));
        }

        if (transaction is not null)
        {
            await transaction.CommitAsync();
        }

        _logger.LogInformation(
            "Connected app {ClientId} was authorized for user {UserId} with {Count} wishlist(s).",
            app.ClientId,
            grant.UserId,
            allowedIds.Count);

        return tokens with { ConnectionPublicId = connection.PublicId };
    }

    public async Task<TokenExchangeResult> RefreshAsync(
        IntegrationApp app,
        string? refreshToken,
        IntegrationAudience? requestedAudience = null)
    {
        ArgumentNullException.ThrowIfNull(app);
        if (string.IsNullOrEmpty(refreshToken))
        {
            return TokenExchangeResult.Failed("invalid_request", "refresh_token is required.");
        }

        var tokenHash = IntegrationTokens.Hash(refreshToken);
        await using var context = await _contextFactory.CreateDbContextAsync();
        var userId = await context.IntegrationConnections
            .Where(c => (c.RefreshTokenHash == tokenHash || c.PreviousRefreshTokenHash == tokenHash) && c.ClientId == app.ClientId)
            .Select(c => c.UserId)
            .FirstOrDefaultAsync();
        if (userId is null)
        {
            return TokenExchangeResult.Failed("invalid_grant", "The refresh token is not valid. Connect again.");
        }

        await using var transaction = await LockConnectionAsync(context, userId, app.ClientId);
        var connection = await context.IntegrationConnections
            .FirstOrDefaultAsync(c =>
                (c.RefreshTokenHash == tokenHash || c.PreviousRefreshTokenHash == tokenHash) &&
                c.ClientId == app.ClientId &&
                c.RevokedOn == null &&
                !c.Deleted);
        if (connection is null)
        {
            return TokenExchangeResult.Failed("invalid_grant", "The refresh token is not valid. Connect again.");
        }

        var now = DateTimeOffset.UtcNow;
        if (connection.RefreshTokenHash != tokenHash)
        {
            // OAuth 2.0 Security BCP section 4.14.2: a rotated refresh token used again may be stolen.
            Revoke(connection, now);
            await InvalidatePendingGrantsAsync(context, connection);
            await context.SaveChangesAsync();
            if (transaction is not null)
            {
                await transaction.CommitAsync();
            }

            _logger.LogWarning(
                "A replaced refresh token for connected app {ClientId} was used again; the connection for user {UserId} was revoked.",
                app.ClientId,
                connection.UserId);
            return TokenExchangeResult.Failed("invalid_grant", "The refresh token was already used. Connect again.");
        }

        if (requestedAudience is not null && requestedAudience != connection.Audience)
        {
            return TokenExchangeResult.Failed("invalid_target", "resource does not match this connection.");
        }

        var tokens = IssueTokens(connection, now);
        connection.UpdatedOn = now;
        await context.SaveChangesAsync();
        if (transaction is not null)
        {
            await transaction.CommitAsync();
        }

        return tokens with { ConnectionPublicId = connection.PublicId };
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
        var userId = await context.IntegrationConnections
            .Where(c => (c.AccessTokenHash == tokenHash || c.RefreshTokenHash == tokenHash) && c.ClientId == app.ClientId)
            .Select(c => c.UserId)
            .FirstOrDefaultAsync();
        if (userId is null)
        {
            return;
        }

        await using var transaction = await LockConnectionAsync(context, userId, app.ClientId);
        var connection = await context.IntegrationConnections
            .FirstOrDefaultAsync(c => (c.AccessTokenHash == tokenHash || c.RefreshTokenHash == tokenHash) && c.ClientId == app.ClientId);

        if (connection is null)
        {
            return;
        }

        Revoke(connection, DateTimeOffset.UtcNow);
        await InvalidatePendingGrantsAsync(context, connection);
        await context.SaveChangesAsync();
        if (transaction is not null)
        {
            await transaction.CommitAsync();
        }
        _logger.LogInformation("Connected app {ClientId} revoked its access for user {UserId}.", app.ClientId, connection.UserId);
    }

    public async Task<IntegrationCaller?> AuthenticateAsync(string? token, IntegrationAudience audience = IntegrationAudience.Api)
    {
        if (string.IsNullOrEmpty(token) || !token.StartsWith(IntegrationTokens.AccessTokenPrefix, StringComparison.Ordinal))
        {
            return null;
        }

        var tokenHash = IntegrationTokens.Hash(token);
        await using var context = await _contextFactory.CreateDbContextAsync();
        var now = DateTimeOffset.UtcNow;
        var connection = await context.IntegrationConnections
            .AsNoTracking()
            .FirstOrDefaultAsync(c =>
                c.AccessTokenHash == tokenHash &&
                c.Audience == audience &&
                (c.AccessTokenExpiresOn == null || c.AccessTokenExpiresOn > now) &&
                c.RevokedOn == null &&
                !c.Deleted);
        if (connection is null)
        {
            return null;
        }

        var app = await _clientStore.FindAsync(connection.ClientId);
        if (app is null)
        {
            return null;
        }

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

        var apps = new Dictionary<string, IntegrationApp?>(StringComparer.Ordinal);
        foreach (var clientId in connections.Select(c => c.ClientId).Distinct(StringComparer.Ordinal))
        {
            apps[clientId] = await _clientStore.FindAsync(clientId);
        }

        return connections
            .Select(connection =>
            {
                var app = apps[connection.ClientId];
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
                    wishlists,
                    app?.IsSelfRegistered ?? IntegrationClientStore.IsSelfRegisteredId(connection.ClientId),
                    connection.Audience,
                    connection.AllowAllWishlists,
                    connection.ManageExistingItems);
            })
            .ToList();
    }

    public async Task<bool> UpdateAllowedWishlistsAsync(
        string userId,
        string connectionPublicId,
        IReadOnlyCollection<string> wishlistPublicIds,
        string? defaultWishlistPublicId,
        bool allowAllWishlists = false,
        bool manageExistingItems = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ArgumentNullException.ThrowIfNull(wishlistPublicIds);

        var requestedIds = wishlistPublicIds
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.Ordinal)
            .ToList();
        if (!allowAllWishlists && requestedIds.Count == 0)
        {
            return false;
        }

        await using var context = await _contextFactory.CreateDbContextAsync();
        var clientId = await context.IntegrationConnections
            .Where(c => c.PublicId == connectionPublicId && c.UserId == userId)
            .Select(c => c.ClientId)
            .FirstOrDefaultAsync();
        if (clientId is null)
        {
            return false;
        }

        await using var transaction = await LockConnectionAsync(context, userId, clientId);
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
            .Where(w => w.OwnerId == userId && !w.Deleted && (allowAllWishlists || requestedIds.Contains(w.PublicId)))
            .OrderBy(w => w.Id)
            .Select(w => new { w.Id, w.PublicId })
            .ToListAsync();
        if (selected.Count == 0 || requestedIds.Any(id => !selected.Any(w => w.PublicId == id)) ||
            (defaultWishlistPublicId is not null && !selected.Any(w => w.PublicId == defaultWishlistPublicId)))
        {
            return false;
        }

        connection.AllowedWishlists.Clear();
        foreach (var wishlist in selected)
        {
            connection.AllowedWishlists.Add(new IntegrationConnectionWishlist { WishlistId = wishlist.Id });
        }

        connection.DefaultWishlistId = selected.FirstOrDefault(w => w.PublicId == defaultWishlistPublicId)?.Id
            ?? selected[0].Id;
        connection.AllowAllWishlists = allowAllWishlists;
        connection.ManageExistingItems = manageExistingItems;
        connection.UpdatedOn = DateTimeOffset.UtcNow;
        await InvalidatePendingGrantsAsync(context, connection);
        await context.SaveChangesAsync();
        if (transaction is not null)
        {
            await transaction.CommitAsync();
        }

        return true;
    }

    public async Task<bool> DisconnectAsync(string userId, string connectionPublicId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        await using var context = await _contextFactory.CreateDbContextAsync();
        var clientId = await context.IntegrationConnections
            .Where(c => c.PublicId == connectionPublicId && c.UserId == userId)
            .Select(c => c.ClientId)
            .FirstOrDefaultAsync();
        if (clientId is null)
        {
            return false;
        }

        await using var transaction = await LockConnectionAsync(context, userId, clientId);
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
        await InvalidatePendingGrantsAsync(context, connection);
        await context.SaveChangesAsync();
        if (transaction is not null)
        {
            await transaction.CommitAsync();
        }
        _logger.LogInformation("User {UserId} disconnected app {ClientId}.", userId, connection.ClientId);
        return true;
    }

    /// <summary>Replaces the connection's tokens. MCP tokens are short-lived and come with a rotating refresh token.</summary>
    private static TokenExchangeResult IssueTokens(IntegrationConnection connection, DateTimeOffset now)
    {
        var accessToken = IntegrationTokens.NewAccessToken();
        connection.AccessTokenHash = IntegrationTokens.Hash(accessToken);
        if (connection.Audience != IntegrationAudience.Mcp)
        {
            connection.AccessTokenExpiresOn = null;
            connection.RefreshTokenHash = null;
            connection.PreviousRefreshTokenHash = null;
            return new TokenExchangeResult(accessToken, null, null, null);
        }

        var refreshToken = IntegrationTokens.NewRefreshToken();
        connection.AccessTokenExpiresOn = now.Add(McpAccessTokenLifetime);
        connection.PreviousRefreshTokenHash = connection.RefreshTokenHash;
        connection.RefreshTokenHash = IntegrationTokens.Hash(refreshToken);
        return new TokenExchangeResult(accessToken, null, null, null)
        {
            RefreshToken = refreshToken,
            ExpiresInSeconds = (int)McpAccessTokenLifetime.TotalSeconds
        };
    }

    private static void Revoke(IntegrationConnection connection, DateTimeOffset now)
    {
        connection.AccessTokenHash = null;
        connection.AccessTokenExpiresOn = null;
        connection.RefreshTokenHash = null;
        connection.PreviousRefreshTokenHash = null;
        connection.RevokedOn = now;
        connection.UpdatedOn = now;
    }

    private static async Task<IDbContextTransaction?> LockConnectionAsync(
        ApplicationDbContext context, string userId, string clientId)
    {
        if (!context.Database.IsRelational())
        {
            return null;
        }

        var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            // A database lock also covers the first connection, before a row exists, across web instances.
            var lockKey = $"openwish-integration:{userId.Length}:{userId}:{clientId}";
            await context.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT pg_advisory_xact_lock(hashtextextended({lockKey}, 0))");
            return transaction;
        }
        catch
        {
            await transaction.DisposeAsync();
            throw;
        }
    }

    private static async Task InvalidatePendingGrantsAsync(ApplicationDbContext context, IntegrationConnection connection)
    {
        var grants = await context.IntegrationAuthorizationCodes
            .Where(c => c.UserId == connection.UserId && c.ClientId == connection.ClientId && c.ConsumedOn == null)
            .ToListAsync();
        context.IntegrationAuthorizationCodes.RemoveRange(grants);
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
        await InvalidatePendingGrantsAsync(context, connection);
        await context.SaveChangesAsync();
        _logger.LogWarning(
            "An authorization code for connected app {ClientId} was replayed; the connection for user {UserId} was revoked.",
            grant.ClientId,
            grant.UserId);
    }
}