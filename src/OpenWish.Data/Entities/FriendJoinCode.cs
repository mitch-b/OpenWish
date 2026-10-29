namespace OpenWish.Data.Entities;

/// <summary>A short-lived, single-use code that lets someone nearby connect with the owner in person.</summary>
public class FriendJoinCode : BaseEntity
{
    public required string UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;

    /// <summary>SHA-256 of the normalized code; the code itself is never stored.</summary>
    public required string CodeHash { get; set; }

    public DateTimeOffset ExpiresOn { get; set; }
    public DateTimeOffset? UsedOn { get; set; }
}