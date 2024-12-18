namespace OpenWish.Data.Entities;

public class Comment
{
    public int CommentId { get; set; }
    public string Text { get; set; }
    public DateTime CreatedDate { get; set; }
    public int WishlistItemId { get; set; }
    public WishlistItem WishlistItem { get; set; }
    public int UserId { get; set; }
    public User User { get; set; }
}