using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenWish.Shared.Models;
using OpenWish.Shared.Services;
using OpenWish.Web.Services;

namespace OpenWish.Web.Controllers;

[ApiController]
[Route("api/circles")]
[Authorize]
public class CircleController(ICircleService circleService, ApiUserContextService userContextService) : ControllerBase
{
    [HttpGet]
    public Task<IActionResult> GetCircles() =>
        RunAsync(userId => circleService.GetCirclesAsync(userId));

    [HttpPost]
    public Task<IActionResult> Create([FromBody] CircleNameRequest request) =>
        RunAsync(userId => circleService.CreateCircleAsync(userId, request.Name));

    [HttpPut("{circlePublicId}")]
    public Task<IActionResult> Rename(string circlePublicId, [FromBody] CircleNameRequest request) =>
        RunAsync(userId => circleService.RenameCircleAsync(userId, circlePublicId, request.Name));

    [HttpDelete("{circlePublicId}")]
    public Task<IActionResult> Delete(string circlePublicId) =>
        RunAsync(async userId =>
        {
            await circleService.DeleteCircleAsync(userId, circlePublicId);
            return (object?)null;
        });

    [HttpPost("{circlePublicId}/members")]
    public Task<IActionResult> AddMember(string circlePublicId, [FromBody] CircleMemberRequest request) =>
        RunAsync(userId => circleService.AddMemberAsync(userId, circlePublicId, request.UserId));

    [HttpDelete("{circlePublicId}/members/{memberUserId}")]
    public Task<IActionResult> RemoveMember(string circlePublicId, string memberUserId) =>
        RunAsync(userId => circleService.RemoveMemberAsync(userId, circlePublicId, memberUserId));

    [HttpPost("{circlePublicId}/wishlists")]
    public Task<IActionResult> ShareWishlist(string circlePublicId, [FromBody] CircleWishlistRequest request) =>
        RunAsync(userId => circleService.ShareWishlistAsync(userId, circlePublicId, request.WishlistPublicId));

    [HttpDelete("{circlePublicId}/wishlists/{wishlistPublicId}")]
    public Task<IActionResult> UnshareWishlist(string circlePublicId, string wishlistPublicId) =>
        RunAsync(userId => circleService.UnshareWishlistAsync(userId, circlePublicId, wishlistPublicId));

    private async Task<IActionResult> RunAsync<T>(Func<string, Task<T>> action)
    {
        var userId = await userContextService.GetUserIdAsync();
        if (userId is null)
        {
            return Unauthorized();
        }

        try
        {
            var result = await action(userId);
            return result is null ? NoContent() : Ok(result);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }
    }
}