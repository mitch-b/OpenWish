namespace OpenWish.Data.Entities;

public class EventUser : BaseEntity
{
    public int EventId { get; set; }
    public Event Event { get; set; }

    public int UserId { get; set; }
    public User User { get; set; }

    public DateTimeOffset InvitationDate { get; set; }
    public bool IsAccepted { get; set; }
    public string Role { get; set; } // Role of the user in the event (e.g., Organizer, Viewer)
}