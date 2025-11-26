namespace OpenWish.Data.Entities;

public class PendingFriendInvite : BaseEntity
{
    public required string SenderUserId { get; set; }
    public ApplicationUser Sender { get; set; } = null!;

    public required string Email { get; set; }

    public DateTimeOffset InviteDate { get; set; }
    public string Status { get; set; } = "Pending"; // "Pending", "Accepted", "Cancelled"
}