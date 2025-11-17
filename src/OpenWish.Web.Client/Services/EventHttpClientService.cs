using System;
using System.Linq;
using System.Net.Http.Json;
using OpenWish.Shared.Models;
using OpenWish.Shared.RequestModels;
using OpenWish.Shared.Services;

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

    public async Task<IEnumerable<WishlistModel>> GetEventWishlistsAsync(int eventId)
    {
        return await httpClient.GetFromJsonAsync<IEnumerable<WishlistModel>>($"api/events/{eventId}/wishlists")
            ?? Enumerable.Empty<WishlistModel>();
    }

    public async Task<WishlistModel> CreateEventWishlistAsync(int eventId, WishlistModel wishlistModel, string ownerId)
    {
        var response = await httpClient.PostAsJsonAsync($"api/events/{eventId}/wishlists", wishlistModel);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<WishlistModel>()
            ?? throw new InvalidOperationException("Unable to deserialize created wishlist.");
    }

    public async Task<WishlistModel> AttachWishlistAsync(int eventId, int wishlistId, string userId)
    {
        // Note: This method uses integer IDs but the API now expects PublicIds
        // This is kept for backwards compatibility with internal components
        // The actual API call will need to be updated when the component is refactored
        var request = new { WishlistId = wishlistId };
        var response = await httpClient.PostAsJsonAsync($"api/events/{eventId}/wishlists/attach", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<WishlistModel>()
            ?? throw new InvalidOperationException("Unable to deserialize attached wishlist.");
    }

    public async Task<bool> DetachWishlistAsync(int eventId, int wishlistId, string userId)
    {
        var response = await httpClient.DeleteAsync($"api/events/{eventId}/wishlists/{wishlistId}");
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
        return await httpClient.GetFromJsonAsync<IEnumerable<EventUserModel>>($"api/events/{eventId}/invitations")
            ?? Enumerable.Empty<EventUserModel>();
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

    // PublicId-based methods
    public async Task<EventModel> GetEventByPublicIdAsync(string publicId)
    {
        return await httpClient.GetFromJsonAsync<EventModel>($"api/events/{publicId}");
    }

    public async Task<EventModel> UpdateEventByPublicIdAsync(string publicId, EventModel evt)
    {
        var response = await httpClient.PutAsJsonAsync($"api/events/{publicId}", evt);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<EventModel>();
    }

    public async Task DeleteEventByPublicIdAsync(string publicId)
    {
        var response = await httpClient.DeleteAsync($"api/events/{publicId}");
        response.EnsureSuccessStatusCode();
    }

    public async Task<bool> AddUserToEventByPublicIdAsync(string eventPublicId, string userId, string role = "Participant")
    {
        var request = new { UserId = userId, Role = role };
        var response = await httpClient.PostAsJsonAsync($"api/events/{eventPublicId}/users", request);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> RemoveUserFromEventByPublicIdAsync(string eventPublicId, string userId)
    {
        var response = await httpClient.DeleteAsync($"api/events/{eventPublicId}/users/{userId}");
        return response.IsSuccessStatusCode;
    }

    public async Task<IEnumerable<WishlistModel>> GetEventWishlistsByPublicIdAsync(string eventPublicId)
    {
        return await httpClient.GetFromJsonAsync<IEnumerable<WishlistModel>>($"api/events/{eventPublicId}/wishlists");
    }

    public async Task<WishlistModel> CreateEventWishlistByPublicIdAsync(string eventPublicId, WishlistModel wishlistModel, string ownerId)
    {
        var response = await httpClient.PostAsJsonAsync($"api/events/{eventPublicId}/wishlists", wishlistModel);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<WishlistModel>();
    }

    public async Task<WishlistModel> AttachWishlistByPublicIdAsync(string eventPublicId, string wishlistPublicId, string userId)
    {
        var request = new { WishlistPublicId = wishlistPublicId };
        var response = await httpClient.PostAsJsonAsync($"api/events/{eventPublicId}/wishlists/attach", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<WishlistModel>();
    }

    public async Task<bool> DetachWishlistByPublicIdAsync(string eventPublicId, string wishlistPublicId, string userId)
    {
        var response = await httpClient.DeleteAsync($"api/events/{eventPublicId}/wishlists/{wishlistPublicId}");
        return response.IsSuccessStatusCode;
    }

    public async Task<IEnumerable<EventReservedItemModel>> GetReservedItemsForUserByPublicIdAsync(string eventPublicId, string userId)
    {
        var items = await httpClient.GetFromJsonAsync<IEnumerable<EventReservedItemModel>>($"api/events/{eventPublicId}/reservations/mine");
        return items ?? Enumerable.Empty<EventReservedItemModel>();
    }

    public async Task<EventUserModel> InviteUserToEventByPublicIdAsync(string eventPublicId, string inviterId, string userId)
    {
        var response = await httpClient.PostAsync($"api/events/{eventPublicId}/invitations/user/{userId}", null);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<EventUserModel>();
    }

    public async Task<EventUserModel> InviteByEmailToEventByPublicIdAsync(string eventPublicId, string inviterId, string email)
    {
        var request = new { Email = email };
        var response = await httpClient.PostAsJsonAsync($"api/events/{eventPublicId}/invitations/email", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<EventUserModel>();
    }

    public async Task<IEnumerable<EventUserModel>> GetEventInvitationsByPublicIdAsync(string eventPublicId)
    {
        return await httpClient.GetFromJsonAsync<IEnumerable<EventUserModel>>($"api/events/{eventPublicId}/invitations");
    }
}