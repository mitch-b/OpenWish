namespace OpenWish.Data.Entities;

public class EventUser
{
    public int EventId { get; set; }
    public Event Event { get; set; }

    public int UserId { get; set; }
    public User User { get; set; }

    public DateTime InvitationDate { get; set; }
    public bool IsAccepted { get; set; }
    public string Role { get; set; } // Role of the user in the event (e.g., Organizer, Viewer)
}