using OpenWish.Shared.Models;

namespace OpenWish.Shared.Services;

public interface IActivityService
{
    Task<ActivityLogModel> LogActivityAsync(
        string userId, 
        string activityType,
        string description,
        int? wishlistId = null,
        int? wishlistItemId = null);
        
    Task<IEnumerable<ActivityLogModel>> GetUserActivityFeedAsync(string userId, int count = 20, int skip = 0);
    Task<IEnumerable<ActivityLogModel>> GetFriendsActivityFeedAsync(string userId, int count = 20, int skip = 0);
    Task<IEnumerable<ActivityLogModel>> GetWishlistActivityAsync(int wishlistId, int count = 20, int skip = 0);
}