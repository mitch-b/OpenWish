namespace OpenWish.Data.Entities;

public class WishlistReaction : BaseEntity
{
    public int WishlistId { get; set; }
    public Wishlist Wishlist { get; set; }
    public int UserId { get; set; }
    public OpenWishUser OpenWishUser { get; set; }
    public string ReactionType { get; set; } // e.g., "Like", "Love", "Wow"
}