using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenWish.Shared.RequestModels;
using OpenWish.Shared.Services;
using OpenWish.Web.Services;

namespace OpenWish.Web.Controllers;

[ApiController]
[Route("api/birthdays")]
[Authorize]
public class BirthdayController(IBirthdayService birthdayService, ApiUserContextService userContextService) : ControllerBase
{
    [HttpGet("me")]
    public async Task<IActionResult> GetMine()
    {
        var userId = await userContextService.GetUserIdAsync();
        return userId is null ? Unauthorized() : Ok(await birthdayService.GetBirthdayAsync(userId));
    }

    [HttpPut("me")]
    public async Task<IActionResult> SetMine([FromBody] UpdateBirthdayRequest request)
    {
        var userId = await userContextService.GetUserIdAsync();
        if (userId is null)
        {
            return Unauthorized();
        }

        try
        {
            await birthdayService.SetBirthdayAsync(userId, request.Month, request.Day);
            return NoContent();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("upcoming")]
    public async Task<IActionResult> GetUpcoming([FromQuery] int withinDays = 30)
    {
        var userId = await userContextService.GetUserIdAsync();
        return userId is null ? Unauthorized() : Ok(await birthdayService.GetUpcomingBirthdaysAsync(userId, Math.Clamp(withinDays, 1, 366)));
    }
}