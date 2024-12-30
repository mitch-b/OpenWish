namespace OpenWish.Data.Entities;

public class WishlistReaction : BaseEntity
{
    public int WishlistId { get; set; }
    public Wishlist Wishlist { get; set; }
    public string UserId { get; set; }
    public ApplicationUser User { get; set; }
    public string ReactionType { get; set; } // e.g., "Like", "Love", "Wow"
}
