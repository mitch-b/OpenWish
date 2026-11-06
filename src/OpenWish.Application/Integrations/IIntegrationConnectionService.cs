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
    string? NewWishlistName);

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
    IReadOnlyList<ConnectedAppWishlist> Wishlists);

public interface IIntegrationConnectionService
{
    /// <summary>Lists the person's own wishlists for the consent screen, marking any the app already uses.</summary>
    Task<ConsentState> GetConsentStateAsync(string userId, string clientId);

    /// <summary>Records the person's approval and returns a single-use authorization code.</summary>
    Task<AuthorizationApprovalResult> ApproveAsync(string userId, AuthorizationApproval approval);

    /// <summary>Exchanges an authorization code for an access token, creating or renewing the connection.</summary>
    Task<TokenExchangeResult> ExchangeCodeAsync(IntegrationApp app, string? code, string? redirectUri, string? codeVerifier);

    /// <summary>Revokes the connection behind a token when it belongs to the calling app.</summary>
    Task RevokeTokenAsync(IntegrationApp app, string? token);

    /// <summary>Resolves an active connection from a bearer token.</summary>
    Task<IntegrationCaller?> AuthenticateAsync(string? token);

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