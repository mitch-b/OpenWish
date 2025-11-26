using System.Net.Http.Json;
using OpenWish.Shared.Models;
using OpenWish.Shared.Services;

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

    public async Task<bool> CancelFriendRequestAsync(int requestId, string requesterId)
    {
        var response = await _httpClient.PostAsync($"{BaseUrl}/request/{requestId}/cancel/{requesterId}", null);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<bool>();
    }

    public async Task<FriendRequestModel> ResendFriendRequestAsync(int requestId, string requesterId)
    {
        var response = await _httpClient.PostAsync($"{BaseUrl}/request/{requestId}/resend/{requesterId}", null);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<FriendRequestModel>()
            ?? throw new HttpRequestException("Failed to resend friend request");
    }

    public async Task<bool> SendFriendInviteByEmailAsync(string senderUserId, string emailAddress)
    {
        var response = await _httpClient.PostAsync($"{BaseUrl}/invite/{senderUserId}?email={Uri.EscapeDataString(emailAddress)}", null);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<bool>();
    }

    public async Task<bool> SendFriendInvitesByEmailAsync(string senderUserId, IEnumerable<string> emailAddresses)
    {
        var response = await _httpClient.PostAsJsonAsync($"{BaseUrl}/invite/{senderUserId}/batch", emailAddresses);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<bool>();
    }

    public async Task<bool> CreateFriendshipFromInviteAsync(string newUserId, string inviterUserId)
    {
        var response = await _httpClient.PostAsync($"{BaseUrl}/invite/complete/{newUserId}/{inviterUserId}", null);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<bool>();
    }

    public async Task<IEnumerable<PendingFriendInviteModel>> GetPendingFriendInvitesAsync(string userId)
    {
        return await _httpClient.GetFromJsonAsync<IEnumerable<PendingFriendInviteModel>>($"{BaseUrl}/pending-invites/{userId}")
            ?? Array.Empty<PendingFriendInviteModel>();
    }

    public async Task<bool> CancelPendingFriendInviteAsync(int inviteId, string userId)
    {
        var response = await _httpClient.PostAsync($"{BaseUrl}/pending-invite/{inviteId}/cancel/{userId}", null);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<bool>();
    }

    public async Task<bool> ResendPendingFriendInviteAsync(int inviteId, string userId)
    {
        var response = await _httpClient.PostAsync($"{BaseUrl}/pending-invite/{inviteId}/resend/{userId}", null);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<bool>();
    }

    public Task<IEnumerable<ApplicationUserModel>> SearchUsersAsync(string searchTerm, string currentUserId, int maxResults = 10)
    {
        // Username search removed for privacy/security reasons
        return Task.FromResult<IEnumerable<ApplicationUserModel>>(Array.Empty<ApplicationUserModel>());
    }
}