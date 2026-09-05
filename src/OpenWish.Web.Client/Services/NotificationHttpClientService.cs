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
        return await _httpClient.GetFromJsonAsync<IEnumerable<NotificationModel>>($"{BaseUrl}?includeRead={includeRead}")
            ?? Array.Empty<NotificationModel>();
    }

    public async Task<int> GetUnreadNotificationCountAsync(string userId)
    {
        return await _httpClient.GetFromJsonAsync<int>($"{BaseUrl}/count");
    }

    public async Task<NotificationModel> CreateNotificationAsync(string userId, string message)
    {
        throw new NotSupportedException("Notifications can only be created by trusted server-side services.");
    }

    public async Task<NotificationModel> CreateNotificationAsync(
        string senderUserId,
        string targetUserId,
        string title,
        string message,
        string type,
        NotificationActionModel? action = null)
    {
        throw new NotSupportedException("Notifications can only be created by trusted server-side services.");
    }

    public async Task<bool> MarkNotificationAsReadAsync(string notificationPublicId, string userId)
    {
        var response = await _httpClient.PutAsync($"{BaseUrl}/{notificationPublicId}/read", null);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<bool>();
    }

    public async Task<bool> MarkAllNotificationsAsReadAsync(string userId)
    {
        var response = await _httpClient.PutAsync($"{BaseUrl}/read-all", null);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<bool>();
    }

    public async Task<bool> DeleteNotificationAsync(string notificationPublicId, string userId)
    {
        var response = await _httpClient.DeleteAsync($"{BaseUrl}/{notificationPublicId}");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<bool>();
    }
}