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
    private readonly IWishlistService _wishlistService;
    private readonly ApiUserContextService _userContextService;

    public ActivityController(
        IActivityService activityService,
        IWishlistService wishlistService,
        ApiUserContextService userContextService)
    {
        _activityService = activityService;
        _wishlistService = wishlistService;
        _userContextService = userContextService;
    }

    [HttpGet("user")]
    public async Task<IActionResult> GetUserActivities([FromQuery] int count = 20, [FromQuery] int skip = 0)
    {
        var userId = await _userContextService.GetUserIdAsync();
        if (userId is null)
        {
            return Unauthorized();
        }

        var activities = await _activityService.GetUserActivityFeedAsync(userId, Math.Clamp(count, 1, 100), Math.Max(skip, 0));
        return Ok(activities);
    }

    [HttpGet("friends")]
    public async Task<IActionResult> GetFriendsActivities([FromQuery] int count = 20, [FromQuery] int skip = 0)
    {
        var userId = await _userContextService.GetUserIdAsync();
        if (userId is null)
        {
            return Unauthorized();
        }

        var activities = await _activityService.GetFriendsActivityFeedAsync(userId, Math.Clamp(count, 1, 100), Math.Max(skip, 0));
        return Ok(activities);
    }

    [HttpGet("wishlist/{wishlistId}")]
    public async Task<IActionResult> GetWishlistActivities(int wishlistId, [FromQuery] int count = 20, [FromQuery] int skip = 0)
    {
        var userId = await _userContextService.GetUserIdAsync();
        if (userId is null)
        {
            return Unauthorized();
        }

        if (!await _wishlistService.CanUserAccessWishlistAsync(wishlistId, userId))
        {
            return Forbid();
        }

        var activities = await _activityService.GetWishlistActivityAsync(
            wishlistId,
            Math.Clamp(count, 1, 100),
            Math.Max(skip, 0),
            userId);
        return Ok(activities);
    }
}