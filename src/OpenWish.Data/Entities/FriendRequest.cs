namespace OpenWish.Data.Entities;

public class FriendRequest : BaseEntity
{
    public required string RequesterId { get; set; }
    public ApplicationUser? Requester { get; set; }

    public required string ReceiverId { get; set; }
    public ApplicationUser? Receiver { get; set; }

    public DateTimeOffset RequestDate { get; set; }
    public required string Status { get; set; } // "Pending", "Accepted", "Rejected"
}