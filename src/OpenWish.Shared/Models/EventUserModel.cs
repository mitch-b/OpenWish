namespace OpenWish.Shared.Models;

public class EventUserModel : BaseEntityModel
{
    public int Id { get; set; }
    public int EventId { get; set; }
    public EventModel? Event { get; set; }
    public string? UserId { get; set; }
    public ApplicationUserModel? User { get; set; }
    public string? InviteeEmail { get; set; }
    public DateTimeOffset InvitationDate { get; set; }
    public string Status { get; set; } = "Pending"; // "Pending", "Accepted", "Rejected"
    public bool IsAccepted { get; set; } // Backward compatibility
    public string Role { get; set; } = "Participant"; // Role of the user in the event (e.g., Organizer, Participant, Viewer)
}