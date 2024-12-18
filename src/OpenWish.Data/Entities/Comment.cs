namespace OpenWish.Data.Entities;

public class Comment : BaseEntity
{
    public string Text { get; set; }
    public int WishlistItemId { get; set; }
    public WishlistItem WishlistItem { get; set; }
    public int UserId { get; set; }
    public OpenWishUser OpenWishUser { get; set; }
}