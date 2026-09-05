using System.Text.Json.Serialization;

namespace OpenWish.Shared.Models;

public class ItemCommentModel : BaseEntityModel
{
    public string Text { get; set; }
    [JsonIgnore]
    public int WishlistItemId { get; set; }
    [JsonIgnore]
    public WishlistItemModel? WishlistItem { get; set; }
    public string UserId { get; set; }
    public ApplicationUserModel? User { get; set; }
}