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

    [HttpGet("user")]
    public async Task<IActionResult> GetUserActivities([FromQuery] int count = 20, [FromQuery] int skip = 0)
    {
        var userId = await _userContextService.GetUserIdAsync();
        if (userId is null) return Unauthorized();
        var activities = await _activityService.GetUserActivityFeedAsync(userId, count, skip);
        return Ok(activities);
    }

    [HttpGet("friends")]
    public async Task<IActionResult> GetFriendsActivities([FromQuery] int count = 20, [FromQuery] int skip = 0)
    {
        var userId = await _userContextService.GetUserIdAsync();
        if (userId is null) return Unauthorized();
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
        var userId = await _userContextService.GetUserIdAsync();
        if (userId is null) return Unauthorized();
        var activity = await _activityService.LogActivityAsync(
            userId,
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