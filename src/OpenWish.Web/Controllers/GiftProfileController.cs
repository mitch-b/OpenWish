using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenWish.Shared.Models;
using OpenWish.Shared.Services;
using OpenWish.Web.Services;

namespace OpenWish.Web.Controllers;

[ApiController]
[Route("api/gift-profile")]
[Authorize]
public class GiftProfileController(IGiftProfileService giftProfileService, ApiUserContextService userContextService) : ControllerBase
{
    [HttpGet("me")]
    public async Task<IActionResult> GetMine()
    {
        var userId = await userContextService.GetUserIdAsync();
        return userId is null ? Unauthorized() : Ok(await giftProfileService.GetMyProfileAsync(userId));
    }

    [HttpPut("me")]
    public async Task<IActionResult> SetMine([FromBody] GiftProfileModel profile)
    {
        var userId = await userContextService.GetUserIdAsync();
        if (userId is null)
        {
            return Unauthorized();
        }

        try
        {
            await giftProfileService.SetMyProfileAsync(userId, profile);
            return NoContent();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("wishlists/{wishlistPublicId}")]
    public async Task<IActionResult> GetForWishlist(string wishlistPublicId)
    {
        var userId = await userContextService.GetUserIdAsync();
        if (userId is null)
        {
            return Unauthorized();
        }

        var profile = await giftProfileService.GetProfileForWishlistAsync(wishlistPublicId, userId);
        return profile is null ? NotFound() : Ok(profile);
    }
}