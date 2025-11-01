namespace OpenWish.Data.Entities;

public class EventUser : BaseEntity
{
    public int EventId { get; set; }
    public Event Event { get; set; }

    public string? UserId { get; set; }
    public ApplicationUser? User { get; set; }

    public string? InviteeEmail { get; set; } // Email for invitations to non-registered users

    public DateTimeOffset InvitationDate { get; set; }
    public string Status { get; set; } = "Pending"; // "Pending", "Accepted", "Rejected"
    public bool IsAccepted { get; set; } // Kept for backward compatibility, derived from Status
    public string Role { get; set; } = "Participant"; // Role of the user in the event (e.g., Organizer, Participant, Viewer)
}