namespace OpenWish.Data.Entities;

/// <summary>
/// A person's consent for a connected app to manage ideas on their behalf.
/// There is one row per person and app; reconnecting reuses it so the app keeps the ideas it added.
/// </summary>
public class IntegrationConnection : BaseEntity
{
    public required string UserId { get; set; }
    public ApplicationUser? User { get; set; }
    public required string ClientId { get; set; }
    public required string AppName { get; set; }
    public IntegrationAudience Audience { get; set; }
    public string? AccessTokenHash { get; set; }

    /// <summary>When the access token stops working, or null for tokens that last until revoked.</summary>
    public DateTimeOffset? AccessTokenExpiresOn { get; set; }
    public string? RefreshTokenHash { get; set; }

    /// <summary>The refresh token this one replaced. Presenting it again revokes the connection.</summary>
    public string? PreviousRefreshTokenHash { get; set; }
    public DateTimeOffset? TokenIssuedOn { get; set; }
    public DateTimeOffset? LastUsedOn { get; set; }
    public DateTimeOffset? RevokedOn { get; set; }
    public int? DefaultWishlistId { get; set; }
    public Wishlist? DefaultWishlist { get; set; }
    public ICollection<IntegrationConnectionWishlist> AllowedWishlists { get; set; } = [];
}