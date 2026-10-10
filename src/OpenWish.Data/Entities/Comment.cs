namespace OpenWish.Data.Entities;

public class Comment : BaseEntity
{
    public required string Text { get; set; }
    public int WishlistItemId { get; set; }
    public WishlistItem? WishlistItem { get; set; }
    public required string UserId { get; set; }
    public ApplicationUser? User { get; set; }
}