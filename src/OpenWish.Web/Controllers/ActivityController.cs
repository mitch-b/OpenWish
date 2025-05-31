using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenWish.Shared.Services;

namespace OpenWish.Web.Controllers;

[ApiController]
[Route("api/activities")]
[Authorize]
public class ActivityController : ControllerBase
{
    private readonly IActivityService _activityService;

    public ActivityController(IActivityService activityService)
    {
        _activityService = activityService;
    }

    [HttpGet("user/{userId}")]
    public async Task<IActionResult> GetUserActivities(string userId, [FromQuery] int count = 20, [FromQuery] int skip = 0)
    {
        var activities = await _activityService.GetUserActivityFeedAsync(userId, count, skip);
        return Ok(activities);
    }

    [HttpGet("friends/{userId}")]
    public async Task<IActionResult> GetFriendsActivities(string userId, [FromQuery] int count = 20, [FromQuery] int skip = 0)
    {
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
        var activity = await _activityService.LogActivityAsync(
            request.UserId,
            request.ActivityType,
            request.Description,
            request.WishlistId,
            request.WishlistItemId);

        return Ok(activity);
    }

    public class ActivityLogRequest
    {
        public string UserId { get; set; }
        public string ActivityType { get; set; }
        public string Description { get; set; }
        public int? WishlistId { get; set; }
        public int? WishlistItemId { get; set; }
    }
}