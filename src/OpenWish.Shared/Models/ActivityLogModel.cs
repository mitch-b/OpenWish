using System.Text.Json.Serialization;

namespace OpenWish.Shared.Models;

public class ActivityLogModel : BaseEntityModel
{
    public string UserId { get; set; }
    public ApplicationUserModel? User { get; set; }

    public string ActivityType { get; set; } // "WishlistCreated", "ItemAdded", "FriendAdded", etc.
    public string Description { get; set; }

    [JsonIgnore]
    public int? WishlistId { get; set; }
    public WishlistModel? Wishlist { get; set; }

    [JsonIgnore]
    public int? WishlistItemId { get; set; }
    public WishlistItemModel? WishlistItem { get; set; }
}