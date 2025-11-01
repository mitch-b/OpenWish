using OpenWish.Shared.Models;

namespace OpenWish.Shared.Services;

public interface IEventService
{
    Task<EventModel> CreateEventAsync(EventModel evt, string creatorId);
    Task<EventModel> GetEventAsync(int id);
    Task<IEnumerable<EventModel>> GetUserEventsAsync(string userId);
    Task<EventModel> UpdateEventAsync(int id, EventModel evt);
    Task DeleteEventAsync(int id);
    Task<bool> AddUserToEventAsync(int eventId, string userId, string role = "Participant");
    Task<bool> RemoveUserFromEventAsync(int eventId, string userId);

    // Event Invitation methods
    Task<EventUserModel> InviteUserToEventAsync(int eventId, string inviterId, string userId);
    Task<EventUserModel> InviteByEmailToEventAsync(int eventId, string inviterId, string email);
    Task<IEnumerable<EventUserModel>> GetEventInvitationsAsync(int eventId);
    Task<bool> AcceptEventInvitationAsync(int eventUserId, string userId);
    Task<bool> RejectEventInvitationAsync(int eventUserId, string userId);
    Task<bool> CancelEventInvitationAsync(int eventUserId, string inviterId);
    Task<bool> ResendEventInvitationAsync(int eventUserId, string inviterId);
}