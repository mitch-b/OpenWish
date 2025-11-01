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

    public async Task<EventUserModel> InviteUserToEventAsync(int eventId, string inviterId, string userId)
    {
        var response = await httpClient.PostAsJsonAsync($"api/events/{eventId}/invitations/user/{userId}", new { });
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<EventUserModel>();
    }

    public async Task<EventUserModel> InviteByEmailToEventAsync(int eventId, string inviterId, string email)
    {
        var response = await httpClient.PostAsJsonAsync($"api/events/{eventId}/invitations/email", new { Email = email });
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<EventUserModel>();
    }

    public async Task<IEnumerable<EventUserModel>> GetEventInvitationsAsync(int eventId)
    {
        return await httpClient.GetFromJsonAsync<IEnumerable<EventUserModel>>($"api/events/{eventId}/invitations");
    }

    public async Task<bool> AcceptEventInvitationAsync(int eventUserId, string userId)
    {
        var response = await httpClient.PostAsync($"api/events/invitations/{eventUserId}/accept", null);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> RejectEventInvitationAsync(int eventUserId, string userId)
    {
        var response = await httpClient.PostAsync($"api/events/invitations/{eventUserId}/reject", null);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> CancelEventInvitationAsync(int eventUserId, string inviterId)
    {
        var response = await httpClient.DeleteAsync($"api/events/invitations/{eventUserId}");
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> ResendEventInvitationAsync(int eventUserId, string inviterId)
    {
        var response = await httpClient.PostAsync($"api/events/invitations/{eventUserId}/resend", null);
        return response.IsSuccessStatusCode;
    }
}