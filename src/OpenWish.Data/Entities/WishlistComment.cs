namespace OpenWish.Data.Entities;

public class WishlistComment
{
    public int WishlistCommentId { get; set; }
    public string Text { get; set; }
    public DateTime CreatedDate { get; set; }
    public int WishlistId { get; set; }
    public Wishlist Wishlist { get; set; }
    public int UserId { get; set; }
    public User User { get; set; }
}