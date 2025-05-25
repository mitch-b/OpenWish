namespace OpenWish.Data.Entities;

public class WishlistComment : BaseEntity
{
    public string Text { get; set; }
    public int WishlistId { get; set; }
    public Wishlist Wishlist { get; set; }
    public string UserId { get; set; }
    public ApplicationUser User { get; set; }
}