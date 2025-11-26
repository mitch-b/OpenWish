namespace OpenWish.Shared.Models;

public class PendingFriendInviteModel : BaseEntityModel
{
    public string SenderUserId { get; set; } = string.Empty;
    public ApplicationUserModel? Sender { get; set; }

    public string Email { get; set; } = string.Empty;

    public DateTimeOffset InviteDate { get; set; }
    public string Status { get; set; } = "Pending"; // "Pending", "Accepted", "Cancelled"
}