using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenWish.Shared.Services;
using OpenWish.Web.Services;

namespace OpenWish.Web.Controllers;

[ApiController]
[Route("api/gift-plan")]
[Authorize]
public class GiftPlanController(IGiftPlanService giftPlanService, ApiUserContextService userContextService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetGiftPlan()
    {
        var userId = await userContextService.GetUserIdAsync();
        if (userId is null)
        {
            return Unauthorized();
        }

        return Ok(await giftPlanService.GetGiftPlanAsync(userId));
    }
}