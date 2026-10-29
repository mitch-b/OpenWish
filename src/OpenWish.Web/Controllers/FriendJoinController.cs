using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OpenWish.Shared.Models;
using OpenWish.Shared.Services;
using OpenWish.Web.Services;

namespace OpenWish.Web.Controllers;

[ApiController]
[Route("api/friends/join")]
[Authorize]
public class FriendJoinController(IFriendJoinService friendJoinService, ApiUserContextService userContextService) : ControllerBase
{
    [HttpPost("code")]
    [EnableRateLimiting("invitations")]
    public async Task<IActionResult> CreateCode()
    {
        var userId = await userContextService.GetUserIdAsync();
        if (userId is null)
        {
            return Unauthorized();
        }

        var model = await friendJoinService.CreateJoinCodeAsync(userId, $"{Request.Scheme}://{Request.Host}");
        return Ok(model);
    }

    [HttpDelete("code")]
    public async Task<IActionResult> CancelCode()
    {
        var userId = await userContextService.GetUserIdAsync();
        if (userId is null)
        {
            return Unauthorized();
        }

        return Ok(await friendJoinService.CancelJoinCodeAsync(userId));
    }

    [HttpPost("preview")]
    [EnableRateLimiting("invitations")]
    public async Task<IActionResult> Preview([FromBody] FriendJoinRequestModel request)
    {
        var userId = await userContextService.GetUserIdAsync();
        if (userId is null)
        {
            return Unauthorized();
        }

        return Ok(await friendJoinService.PreviewJoinCodeAsync(userId, request.Code));
    }

    [HttpPost("redeem")]
    [EnableRateLimiting("invitations")]
    public async Task<IActionResult> Redeem([FromBody] FriendJoinRequestModel request)
    {
        var userId = await userContextService.GetUserIdAsync();
        if (userId is null)
        {
            return Unauthorized();
        }

        return Ok(await friendJoinService.RedeemJoinCodeAsync(userId, request.Code));
    }
}