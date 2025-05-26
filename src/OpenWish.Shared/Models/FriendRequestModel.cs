namespace OpenWish.Shared.Models;

public class FriendRequestModel : BaseEntityModel
{
    public string RequesterId { get; set; }
    public ApplicationUserModel? Requester { get; set; }

    public string ReceiverId { get; set; }
    public ApplicationUserModel? Receiver { get; set; }

    public DateTimeOffset RequestDate { get; set; }
    public string Status { get; set; } // "Pending", "Accepted", "Rejected"
}