namespace OpenWish.Data.Entities;

public class WishlistComment : BaseEntity
{
    public string Text { get; set; }
    public int WishlistId { get; set; }
    public Wishlist Wishlist { get; set; }
    public int UserId { get; set; }
    public OpenWishUser OpenWishUser { get; set; }
}