namespace OpenWish.Shared.RequestModels;

public class AddUserToEventRequest
{
    public string UserId { get; set; }
    public string Role { get; set; } = "Participant";
}