using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenWish.Shared.Services;
using OpenWish.Web.Services;

namespace OpenWish.Web.Controllers;

[ApiController]
[Route("api/dashboard")]
[Authorize]
public class DashboardController(IDashboardService dashboardService, ApiUserContextService userContextService) : ControllerBase
{
    [HttpGet("checklist")]
    public async Task<IActionResult> GetChecklist()
    {
        var userId = await userContextService.GetUserIdAsync();
        if (userId is null)
        {
            return Unauthorized();
        }

        return Ok(await dashboardService.GetFirstRunChecklistAsync(userId));
    }

    [HttpPost("checklist/dismiss")]
    public async Task<IActionResult> DismissChecklist()
    {
        var userId = await userContextService.GetUserIdAsync();
        if (userId is null)
        {
            return Unauthorized();
        }

        await dashboardService.DismissFirstRunChecklistAsync(userId);
        return NoContent();
    }
}