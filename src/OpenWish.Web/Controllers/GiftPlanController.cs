using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenWish.Shared.RequestModels;
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

    [HttpPut("{wishlistPublicId}/items/{itemPublicId}/purchased")]
    public async Task<IActionResult> SetPurchased(string wishlistPublicId, string itemPublicId, [FromBody] UpdateGiftPurchasedRequest request)
    {
        var userId = await userContextService.GetUserIdAsync();
        if (userId is null)
        {
            return Unauthorized();
        }

        try
        {
            await giftPlanService.SetGiftPurchasedAsync(userId, wishlistPublicId, itemPublicId, request.IsPurchased);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpGet("{wishlistPublicId}/items/{itemId:int}/given")]
    public async Task<IActionResult> GetPreviouslyGiven(string wishlistPublicId, int itemId)
    {
        var userId = await userContextService.GetUserIdAsync();
        if (userId is null)
        {
            return Unauthorized();
        }

        return Ok(await giftPlanService.GetPreviouslyGivenMatchesAsync(userId, wishlistPublicId, itemId));
    }
}