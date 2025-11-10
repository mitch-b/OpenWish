using System.Net.Http.Json;
using OpenWish.Shared.Models;
using OpenWish.Shared.Services;

namespace OpenWish.Web.Client.Services;

public class NotificationHttpClientService(HttpClient httpClient) : INotificationService
{
    private readonly HttpClient _httpClient = httpClient;
    private const string BaseUrl = "api/notifications";

    public async Task<IEnumerable<NotificationModel>> GetUserNotificationsAsync(string userId, bool includeRead = false)
    {
        return await _httpClient.GetFromJsonAsync<IEnumerable<NotificationModel>>($"{BaseUrl}/user/{userId}?includeRead={includeRead}")
            ?? Array.Empty<NotificationModel>();
    }

    public async Task<int> GetUnreadNotificationCountAsync(string userId)
    {
        return await _httpClient.GetFromJsonAsync<int>($"{BaseUrl}/user/{userId}/count");
    }

    public async Task<NotificationModel> CreateNotificationAsync(string userId, string message)
    {
        var response = await _httpClient.PostAsJsonAsync($"{BaseUrl}/user/{userId}", message);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<NotificationModel>()
            ?? throw new HttpRequestException("Failed to create notification");
    }

    public async Task<NotificationModel> CreateNotificationAsync(string senderUserId, string targetUserId, string title, string message, string type)
    {
        var notificationData = new
        {
            SenderUserId = senderUserId,
            Title = title,
            Message = message,
            Type = type
        };

        var response = await _httpClient.PostAsJsonAsync($"{BaseUrl}/user/{targetUserId}/detailed", notificationData);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<NotificationModel>()
            ?? throw new HttpRequestException("Failed to create notification");
    }

    public async Task<bool> MarkNotificationAsReadAsync(int notificationId)
    {
        var response = await _httpClient.PutAsync($"{BaseUrl}/{notificationId}/read", null);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<bool>();
    }

    public async Task<bool> MarkAllNotificationsAsReadAsync(string userId)
    {
        var response = await _httpClient.PutAsync($"{BaseUrl}/user/{userId}/read-all", null);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<bool>();
    }

    public async Task<bool> DeleteNotificationAsync(int notificationId)
    {
        var response = await _httpClient.DeleteAsync($"{BaseUrl}/{notificationId}");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<bool>();
    }
}