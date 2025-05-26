namespace OpenWish.Shared.Models;

public class ItemCommentModel : BaseEntityModel
{
    public string Text { get; set; }
    public int WishlistItemId { get; set; }
    public WishlistItemModel? WishlistItem { get; set; }
    public string UserId { get; set; }
    public ApplicationUserModel? User { get; set; }
}