using System.Net.Http.Json;
using OpenWish.Shared.Models;
using OpenWish.Shared.Services;

namespace OpenWish.Web.Client.Services;

public class ActivityHttpClientService(HttpClient httpClient) : IActivityService
{
    private readonly HttpClient _httpClient = httpClient;
    private const string BaseUrl = "api/activities";

    public async Task<ActivityLogModel> LogActivityAsync(
        string userId,
        string activityType,
        string description,
        int? wishlistId = null,
        int? wishlistItemId = null)
    {
        throw new NotSupportedException("Activity entries can only be created by trusted server-side services.");
    }

    public async Task<IEnumerable<ActivityLogModel>> GetUserActivityFeedAsync(string userId, int count = 20, int skip = 0)
    {
        return await _httpClient.GetFromJsonAsync<IEnumerable<ActivityLogModel>>($"{BaseUrl}/user?count={count}&skip={skip}")
            ?? Array.Empty<ActivityLogModel>();
    }

    public async Task<IEnumerable<ActivityLogModel>> GetFriendsActivityFeedAsync(string userId, int count = 20, int skip = 0)
    {
        return await _httpClient.GetFromJsonAsync<IEnumerable<ActivityLogModel>>($"{BaseUrl}/friends?count={count}&skip={skip}")
            ?? Array.Empty<ActivityLogModel>();
    }

    public async Task<IEnumerable<ActivityLogModel>> GetWishlistActivityAsync(
        int wishlistId,
        int count = 20,
        int skip = 0,
        string? requestingUserId = null)
    {
        return await _httpClient.GetFromJsonAsync<IEnumerable<ActivityLogModel>>($"{BaseUrl}/wishlist/{wishlistId}?count={count}&skip={skip}")
            ?? Array.Empty<ActivityLogModel>();
    }
}