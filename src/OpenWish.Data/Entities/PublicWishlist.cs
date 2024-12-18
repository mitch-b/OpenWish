namespace OpenWish.Data.Entities;

public class PublicWishlist
{
    public int PublicWishlistId { get; set; }
    public string Name { get; set; }
    public int OwnerId { get; set; }
    public User Owner { get; set; }
    public ICollection<WishlistItem> Items { get; set; }
    public string SharedLink { get; set; } // Unique link for sharing publicly
    public string Tags { get; set; } // Comma-separated tags (e.g., "Tech,Books")
}