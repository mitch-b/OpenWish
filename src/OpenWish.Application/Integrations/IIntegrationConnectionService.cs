using OpenWish.Data.Entities;

namespace OpenWish.Application.Integrations;

/// <summary>The connection a bearer token resolved to.</summary>
public sealed record IntegrationCaller(
    int ConnectionId,
    string ConnectionPublicId,
    string UserId,
    string ClientId,
    string AppName);

public sealed record ConsentWishlistOption(
    string PublicId,
    string Name,
    string? Icon,
    int ItemCount,
    bool IsAllowed,
    bool IsDefault,
    string? EventName);

public sealed record ConsentState(IReadOnlyList<ConsentWishlistOption> Wishlists, bool IsReconnecting);

public sealed record AuthorizationApproval(
    string ClientId,
    string RedirectUri,
    string? CodeChallenge,
    IReadOnlyCollection<string> WishlistPublicIds,
    string? DefaultWishlistPublicId,
    string? NewWishlistName,
    IntegrationAudience Audience = IntegrationAudience.Api);

public enum AuthorizationApprovalError
{
    None,
    NoWishlistSelected,
    WishlistNotFound,
    InvalidNewWishlistName
}

public sealed record AuthorizationApprovalResult(string? Code, AuthorizationApprovalError Error);

public sealed record TokenExchangeResult(
    string? AccessToken,
    string? ConnectionPublicId,
    string? Error,
    string? ErrorDescription)
{
    /// <summary>Returned with short-lived MCP tokens; null for tokens that last until revoked.</summary>
    public string? RefreshToken { get; init; }

    public int? ExpiresInSeconds { get; init; }

    public static TokenExchangeResult Failed(string error, string description) => new(null, null, error, description);
}

public sealed record ConnectedAppWishlist(string PublicId, string Name, string? Icon, bool IsDefault);

public sealed record ConnectedAppSummary(
    string ConnectionId,
    string ClientId,
    string AppName,
    string? Description,
    string? HomepageUrl,
    bool IsAvailable,
    DateTimeOffset ConnectedOn,
    DateTimeOffset? LastUsedOn,
    int IdeasAdded,
    IReadOnlyList<ConnectedAppWishlist> Wishlists,
    bool IsSelfRegistered = false,
    IntegrationAudience Audience = IntegrationAudience.Api);

public interface IIntegrationConnectionService
{
    /// <summary>Lists the person's own wishlists for the consent screen, marking any the app already uses.</summary>
    Task<ConsentState> GetConsentStateAsync(string userId, string clientId);

    /// <summary>Records the person's approval and returns a single-use authorization code.</summary>
    Task<AuthorizationApprovalResult> ApproveAsync(string userId, AuthorizationApproval approval);

    /// <summary>Exchanges an authorization code for an access token, creating or renewing the connection.</summary>
    /// <param name="requestedAudience">The audience named by an RFC 8707 resource parameter, if the app sent one.</param>
    Task<TokenExchangeResult> ExchangeCodeAsync(
        IntegrationApp app,
        string? code,
        string? redirectUri,
        string? codeVerifier,
        IntegrationAudience? requestedAudience = null);

    /// <summary>Rotates a refresh token. Presenting a replaced refresh token revokes the connection.</summary>
    Task<TokenExchangeResult> RefreshAsync(IntegrationApp app, string? refreshToken, IntegrationAudience? requestedAudience = null);

    /// <summary>Revokes the connection behind an access or refresh token when it belongs to the calling app.</summary>
    Task RevokeTokenAsync(IntegrationApp app, string? token);

    /// <summary>Resolves an active, unexpired connection from a bearer token issued for <paramref name="audience"/>.</summary>
    Task<IntegrationCaller?> AuthenticateAsync(string? token, IntegrationAudience audience = IntegrationAudience.Api);

    Task<IReadOnlyList<ConnectedAppSummary>> GetConnectedAppsAsync(string userId);

    /// <summary>Changes the lists an app may use. Returns false when the connection or lists are not the person's.</summary>
    Task<bool> UpdateAllowedWishlistsAsync(
        string userId,
        string connectionPublicId,
        IReadOnlyCollection<string> wishlistPublicIds,
        string? defaultWishlistPublicId);

    /// <summary>Revokes a connection. Ideas the app added stay on their lists.</summary>
    Task<bool> DisconnectAsync(string userId, string connectionPublicId);
}