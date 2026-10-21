using System.ComponentModel.DataAnnotations;

namespace OpenWish.Data.Entities;

/// <summary>
/// A short-lived, single-use grant created when a person approves an app on the consent screen.
/// Only the SHA-256 hash of the code is stored.
/// </summary>
public class IntegrationAuthorizationCode
{
    [Key]
    public int Id { get; set; }
    public required string CodeHash { get; set; }
    public required string UserId { get; set; }
    public ApplicationUser? User { get; set; }
    public required string ClientId { get; set; }
    public required string RedirectUri { get; set; }
    public string? CodeChallenge { get; set; }
    public required string GrantedWishlistIds { get; set; }
    public int DefaultWishlistId { get; set; }
    public DateTimeOffset CreatedOn { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset ExpiresOn { get; set; }
    public DateTimeOffset? ConsumedOn { get; set; }
}