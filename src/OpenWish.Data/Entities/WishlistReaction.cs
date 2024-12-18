namespace OpenWish.Data.Entities;

public class WishlistReaction
{
    public int WishlistReactionId { get; set; }
    public int WishlistId { get; set; }
    public Wishlist Wishlist { get; set; }
    public int UserId { get; set; }
    public User User { get; set; }
    public string ReactionType { get; set; } // e.g., "Like", "Love", "Wow"
}
