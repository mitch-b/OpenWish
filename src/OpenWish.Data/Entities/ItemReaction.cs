namespace OpenWish.Data.Entities;

public class ItemReaction : BaseEntity
{
    public int WishlistItemId { get; set; }
    public WishlistItem? WishlistItem { get; set; }

    public required string UserId { get; set; }
    public ApplicationUser? User { get; set; }

    public required string ReactionType { get; set; } // e.g., "Like", "Love", "Wow"
}