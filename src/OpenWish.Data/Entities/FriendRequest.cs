namespace OpenWish.Data.Entities;

public class FriendRequest : BaseEntity
{
    public string RequesterId { get; set; }
    public ApplicationUser Requester { get; set; }

    public string ReceiverId { get; set; }
    public ApplicationUser Receiver { get; set; }

    public DateTimeOffset RequestDate { get; set; }
    public string Status { get; set; } // "Pending", "Accepted", "Rejected"
}