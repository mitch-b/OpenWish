using OpenWish.Shared.Models;

namespace OpenWish.Shared.Services;

/// <summary>
/// Service for tracking and querying activity logs across wishlists and friends.
/// Supports both personal activity feeds and shared activity with privacy filtering.
/// </summary>
public interface IActivityService
{
    /// <summary>Logs an activity event to the audit trail, linked to a user and optionally to specific wishlist or item contexts.</summary>
    /// <param name="userId">The user ID performing the action.</param>
    /// <param name="activityType">The type of activity (e.g., "WishlistCreated", "ItemReserved").</param>
    /// <param name="description">A human-readable description of what happened.</param>
    /// <param name="wishlistId">Optional internal wishlist ID this activity relates to.</param>
    /// <param name="wishlistItemId">Optional internal item ID this activity relates to.</param>
    /// <returns>The logged activity model.</returns>
    Task<ActivityLogModel> LogActivityAsync(
        string userId,
        string activityType,
        string description,
        int? wishlistId = null,
        int? wishlistItemId = null);

    /// <summary>Retrieves the activity feed for the specified user, including their own actions and events.</summary>
    /// <param name="userId">The user ID to fetch activity for.</param>
    /// <param name="count">Maximum number of recent activities to return.</param>
    /// <param name="skip">Number of activities to skip for pagination.</param>
    /// <returns>Collection of recent activity log models for the user.</returns>
    Task<IEnumerable<ActivityLogModel>> GetUserActivityFeedAsync(string userId, int count = 20, int skip = 0);

    /// <summary>Retrieves activity from the user's friends, filtered to only show activity the user is authorized to see.</summary>
    /// <param name="userId">The user ID requesting the feed.</param>
    /// <param name="count">Maximum number of recent activities to return.</param>
    /// <param name="skip">Number of activities to skip for pagination.</param>
    /// <returns>Collection of friend activity log models visible to the user.</returns>
    Task<IEnumerable<ActivityLogModel>> GetFriendsActivityFeedAsync(string userId, int count = 20, int skip = 0);

    /// <summary>Retrieves activity history for a specific wishlist, filtered to only show activity the requesting user is authorized to see.</summary>
    /// <param name="wishlistId">The internal wishlist ID to fetch activity for.</param>
    /// <param name="requestingUserId">The user ID requesting the history (access control is applied).</param>
    /// <param name="count">Maximum number of recent activities to return.</param>
    /// <param name="skip">Number of activities to skip for pagination.</param>
    /// <returns>Collection of wishlist activity log models.</returns>
    Task<IEnumerable<ActivityLogModel>> GetWishlistActivityAsync(
        int wishlistId,
        string requestingUserId,
        int count = 20,
        int skip = 0);
}