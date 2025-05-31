namespace OpenWish.Data.Entities;

public class ItemComment : BaseEntity
{
    public string Text { get; set; }
    public int WishlistItemId { get; set; }
    public WishlistItem WishlistItem { get; set; }
    public string UserId { get; set; }
    public ApplicationUser User { get; set; }
}