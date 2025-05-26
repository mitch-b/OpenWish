namespace OpenWish.Shared.Models;

public class FriendModel : BaseEntityModel
{
    public string UserId { get; set; }
    public ApplicationUserModel? User { get; set; }

    public string FriendUserId { get; set; }
    public ApplicationUserModel? FriendUser { get; set; }

    public DateTimeOffset FriendshipDate { get; set; }
}