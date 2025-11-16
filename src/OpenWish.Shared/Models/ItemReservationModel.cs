using System.Text.Json.Serialization;

namespace OpenWish.Shared.Models;

public class ItemReservationModel : BaseEntityModel
{
    [JsonIgnore]
    public int WishlistItemId { get; set; }
    public WishlistItemModel? WishlistItem { get; set; }

    public string UserId { get; set; }
    public ApplicationUserModel? User { get; set; }

    public DateTimeOffset ReservationDate { get; set; }
    public bool IsAnonymous { get; set; }
}