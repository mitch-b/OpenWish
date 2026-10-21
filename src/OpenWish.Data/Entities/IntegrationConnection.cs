namespace OpenWish.Data.Entities;

/// <summary>
/// A person's consent for an operator-registered app to manage ideas on their behalf.
/// There is one row per person and app; reconnecting reuses it so the app keeps the ideas it added.
/// </summary>
public class IntegrationConnection : BaseEntity
{
    public required string UserId { get; set; }
    public ApplicationUser? User { get; set; }
    public required string ClientId { get; set; }
    public required string AppName { get; set; }
    public string? AccessTokenHash { get; set; }
    public DateTimeOffset? TokenIssuedOn { get; set; }
    public DateTimeOffset? LastUsedOn { get; set; }
    public DateTimeOffset? RevokedOn { get; set; }
    public int? DefaultWishlistId { get; set; }
    public Wishlist? DefaultWishlist { get; set; }
    public ICollection<IntegrationConnectionWishlist> AllowedWishlists { get; set; } = [];
}