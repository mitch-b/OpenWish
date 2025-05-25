using OpenWish.Shared.Models;
using OpenWish.Shared.RequestModels;
using OpenWish.Shared.Services;
using System.Net.Http.Json;

namespace OpenWish.Web.Client.Services;

public class EventHttpClientService(HttpClient httpClient) : IEventService
{
    public async Task<EventModel> CreateEventAsync(EventModel eventModel, string creatorId)
    {
        var response = await httpClient.PostAsJsonAsync("api/events", eventModel);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<EventModel>();
    }

    public async Task<EventModel> GetEventAsync(int id)
    {
        return await httpClient.GetFromJsonAsync<EventModel>($"api/events/{id}");
    }

    public async Task<IEnumerable<EventModel>> GetUserEventsAsync(string userId)
    {
        // Assuming the server uses the authenticated user, so userId may not be needed
        return await httpClient.GetFromJsonAsync<IEnumerable<EventModel>>("api/events");
    }

    public async Task<EventModel> UpdateEventAsync(int id, EventModel eventModel)
    {
        var response = await httpClient.PutAsJsonAsync($"api/events/{id}", eventModel);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<EventModel>();
    }

    public async Task DeleteEventAsync(int id)
    {
        var response = await httpClient.DeleteAsync($"api/events/{id}");
        response.EnsureSuccessStatusCode();
    }

    public async Task<bool> AddUserToEventAsync(int eventId, string userId, string role = "Participant")
    {
        var request = new AddUserToEventRequest { UserId = userId, Role = role };
        var response = await httpClient.PostAsJsonAsync($"api/events/{eventId}/users", request);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> RemoveUserFromEventAsync(int eventId, string userId)
    {
        var response = await httpClient.DeleteAsync($"api/events/{eventId}/users/{userId}");
        return response.IsSuccessStatusCode;
    }
}