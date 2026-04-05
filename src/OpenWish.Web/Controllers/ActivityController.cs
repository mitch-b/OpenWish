using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenWish.Shared.Services;
using OpenWish.Web.Services;

namespace OpenWish.Web.Controllers;

[ApiController]
[Route("api/activities")]
[Authorize]
public class ActivityController : ControllerBase
{
    private readonly IActivityService _activityService;
    private readonly ApiUserContextService _userContextService;

    public ActivityController(IActivityService activityService, ApiUserContextService userContextService)
    {
        _activityService = activityService;
        _userContextService = userContextService;
    }

    [HttpGet("user/{userId}")]
    public async Task<IActionResult> GetUserActivities(string userId, [FromQuery] int count = 20, [FromQuery] int skip = 0)
    {
        var authenticatedUserId = await _userContextService.GetUserIdAsync();
        if (authenticatedUserId != userId)
        {
            return Forbid();
        }

        var activities = await _activityService.GetUserActivityFeedAsync(userId, count, skip);
        return Ok(activities);
    }

    [HttpGet("friends/{userId}")]
    public async Task<IActionResult> GetFriendsActivities(string userId, [FromQuery] int count = 20, [FromQuery] int skip = 0)
    {
        var authenticatedUserId = await _userContextService.GetUserIdAsync();
        if (authenticatedUserId != userId)
        {
            return Forbid();
        }

        var activities = await _activityService.GetFriendsActivityFeedAsync(userId, count, skip);
        return Ok(activities);
    }

    [HttpGet("wishlist/{wishlistId}")]
    public async Task<IActionResult> GetWishlistActivities(int wishlistId, [FromQuery] int count = 20, [FromQuery] int skip = 0)
    {
        var activities = await _activityService.GetWishlistActivityAsync(wishlistId, count, skip);
        return Ok(activities);
    }

    [HttpPost]
    public async Task<IActionResult> LogActivity([FromBody] ActivityLogRequest request)
    {
        var authenticatedUserId = await _userContextService.GetUserIdAsync();
        if (authenticatedUserId is null)
        {
            return Unauthorized();
        }

        // Always use the authenticated user's ID to prevent impersonation
        var activity = await _activityService.LogActivityAsync(
            authenticatedUserId,
            request.ActivityType,
            request.Description,
            request.WishlistId,
            request.WishlistItemId);

        return Ok(activity);
    }

    public class ActivityLogRequest
    {
        public string ActivityType { get; set; }
        public string Description { get; set; }
        public int? WishlistId { get; set; }
        public int? WishlistItemId { get; set; }
    }
}