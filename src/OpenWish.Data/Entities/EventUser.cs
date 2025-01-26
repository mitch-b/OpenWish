namespace OpenWish.Data.Entities;

public class EventUser : BaseEntity
{
    public int EventId { get; set; }
    public Event Event { get; set; } = null!;
    public string? UserId { get; set; }
    public ApplicationUser? User { get; set; }
    public string? Email { get; set; } = null!;
    public DateTimeOffset InvitationDate { get; set; }
    public bool IsAccepted { get; set; }
    public string Role { get; set; } = "Participant"; // Role of the user in the event (e.g., Organizer, Viewer)
}