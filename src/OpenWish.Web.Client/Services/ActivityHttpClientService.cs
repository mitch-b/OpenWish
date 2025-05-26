using OpenWish.Shared.Models;
using OpenWish.Shared.Services;
using System.Net.Http.Json;

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
        var activity = new
        {
            UserId = userId,
            ActivityType = activityType,
            Description = description,
            WishlistId = wishlistId,
            WishlistItemId = wishlistItemId
        };
        
        var response = await _httpClient.PostAsJsonAsync(BaseUrl, activity);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ActivityLogModel>()
            ?? throw new HttpRequestException("Failed to log activity");
    }
    
    public async Task<IEnumerable<ActivityLogModel>> GetUserActivityFeedAsync(string userId, int count = 20, int skip = 0)
    {
        return await _httpClient.GetFromJsonAsync<IEnumerable<ActivityLogModel>>($"{BaseUrl}/user/{userId}?count={count}&skip={skip}")
            ?? Array.Empty<ActivityLogModel>();
    }
    
    public async Task<IEnumerable<ActivityLogModel>> GetFriendsActivityFeedAsync(string userId, int count = 20, int skip = 0)
    {
        return await _httpClient.GetFromJsonAsync<IEnumerable<ActivityLogModel>>($"{BaseUrl}/friends/{userId}?count={count}&skip={skip}")
            ?? Array.Empty<ActivityLogModel>();
    }
    
    public async Task<IEnumerable<ActivityLogModel>> GetWishlistActivityAsync(int wishlistId, int count = 20, int skip = 0)
    {
        return await _httpClient.GetFromJsonAsync<IEnumerable<ActivityLogModel>>($"{BaseUrl}/wishlist/{wishlistId}?count={count}&skip={skip}")
            ?? Array.Empty<ActivityLogModel>();
    }
}