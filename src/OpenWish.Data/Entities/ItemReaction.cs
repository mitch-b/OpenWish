namespace OpenWish.Data.Entities;

public class ItemReaction : BaseEntity
{
    public int WishlistItemId { get; set; }
    public WishlistItem WishlistItem { get; set; }

    public int UserId { get; set; }
    public User User { get; set; }

    public string ReactionType { get; set; } // e.g., "Like", "Love", "Wow"
}