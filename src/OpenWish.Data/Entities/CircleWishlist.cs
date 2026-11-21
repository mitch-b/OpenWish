namespace OpenWish.Data.Entities;

/// <summary>A wishlist its owner shared with every current and future member of a circle.</summary>
public class CircleWishlist : BaseEntity
{
    public int CircleId { get; set; }
    public Circle? Circle { get; set; }

    public int WishlistId { get; set; }
    public Wishlist? Wishlist { get; set; }
}