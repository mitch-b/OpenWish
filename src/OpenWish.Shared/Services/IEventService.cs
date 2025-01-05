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
}
