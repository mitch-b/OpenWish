namespace OpenWish.Data.Entities;

public class ActivityLog : BaseEntity
{
    public string UserId { get; set; }
    public ApplicationUser User { get; set; }

    public string ActivityType { get; set; } // "WishlistCreated", "ItemAdded", "FriendAdded", etc.
    public string Description { get; set; }

    public int? WishlistId { get; set; }
    public Wishlist? Wishlist { get; set; }

    public int? WishlistItemId { get; set; }
    public WishlistItem? WishlistItem { get; set; }
}