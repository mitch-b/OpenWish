using OpenWish.Shared.Models;
using OpenWish.Shared.Services;
using System.Net.Http.Json;

namespace OpenWish.Web.Client.Services;

public class FriendHttpClientService(HttpClient httpClient) : IFriendService
{
    private readonly HttpClient _httpClient = httpClient;
    private const string BaseUrl = "api/friends";

    public async Task<IEnumerable<ApplicationUserModel>> GetFriendsAsync(string userId)
    {
        return await _httpClient.GetFromJsonAsync<IEnumerable<ApplicationUserModel>>($"{BaseUrl}/user/{userId}")
            ?? Array.Empty<ApplicationUserModel>();
    }

    public async Task<bool> AreFriendsAsync(string userId, string otherUserId)
    {
        return await _httpClient.GetFromJsonAsync<bool>($"{BaseUrl}/check/{userId}/{otherUserId}");
    }

    public async Task<bool> RemoveFriendAsync(string userId, string friendId)
    {
        var response = await _httpClient.DeleteAsync($"{BaseUrl}/{userId}/{friendId}");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<bool>();
    }

    public async Task<FriendRequestModel> SendFriendRequestAsync(string requesterId, string receiverId)
    {
        var response = await _httpClient.PostAsync($"{BaseUrl}/request/{requesterId}/{receiverId}", null);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<FriendRequestModel>()
            ?? throw new HttpRequestException("Failed to send friend request");
    }

    public async Task<IEnumerable<FriendRequestModel>> GetReceivedFriendRequestsAsync(string userId)
    {
        return await _httpClient.GetFromJsonAsync<IEnumerable<FriendRequestModel>>($"{BaseUrl}/requests/received/{userId}")
            ?? Array.Empty<FriendRequestModel>();
    }

    public async Task<IEnumerable<FriendRequestModel>> GetSentFriendRequestsAsync(string userId)
    {
        return await _httpClient.GetFromJsonAsync<IEnumerable<FriendRequestModel>>($"{BaseUrl}/requests/sent/{userId}")
            ?? Array.Empty<FriendRequestModel>();
    }

    public async Task<bool> AcceptFriendRequestAsync(int requestId, string userId)
    {
        var response = await _httpClient.PostAsync($"{BaseUrl}/request/{requestId}/accept/{userId}", null);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<bool>();
    }

    public async Task<bool> RejectFriendRequestAsync(int requestId, string userId)
    {
        var response = await _httpClient.PostAsync($"{BaseUrl}/request/{requestId}/reject/{userId}", null);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<bool>();
    }

    public async Task<IEnumerable<ApplicationUserModel>> SearchUsersAsync(string searchTerm, string currentUserId, int maxResults = 10)
    {
        return await _httpClient.GetFromJsonAsync<IEnumerable<ApplicationUserModel>>($"{BaseUrl}/search?term={Uri.EscapeDataString(searchTerm)}&userId={currentUserId}&max={maxResults}")
            ?? Array.Empty<ApplicationUserModel>();
    }
}