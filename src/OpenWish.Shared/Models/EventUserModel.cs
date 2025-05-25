namespace OpenWish.Shared.Models;

public class EventUserModel : BaseEntityModel
{
    public EventModel Event { get; set; }
    public ApplicationUserModel User { get; set; }
    public DateTimeOffset InvitationDate { get; set; }
    public bool IsAccepted { get; set; }
    public string Role { get; set; } // Role of the user in the event (e.g., Organizer, Viewer)
}