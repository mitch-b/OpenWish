using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenWish.Shared.Models;
using OpenWish.Shared.RequestModels;
using OpenWish.Shared.Services;
using OpenWish.Web.Services;

namespace OpenWish.Web.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]s")]
public class EventController(IEventService eventService, ApiUserContextService userContextService) : ControllerBase
{
    private readonly IEventService _eventService = eventService;
    private readonly ApiUserContextService _userContextService = userContextService;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<EventModel>>> GetEvents()
    {
        var userId = await _userContextService.GetUserIdAsync();
        if (userId is null)
        {
            return Unauthorized();
        }
        var events = await _eventService.GetUserEventsAsync(userId);
        return Ok(events);
    }

    [HttpGet("{publicId}")]
    public async Task<ActionResult<EventModel>> GetEvent(string publicId)
    {
        try
        {
            var evt = await _eventService.GetEventByPublicIdAsync(publicId);
            return Ok(evt);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPost]
    public async Task<ActionResult<EventModel>> CreateEvent(EventModel eventModel)
    {
        var userId = await _userContextService.GetUserIdAsync();
        if (userId is null)
        {
            return Unauthorized();
        }
        var createdEvent = await _eventService.CreateEventAsync(eventModel, userId);
        return CreatedAtAction(nameof(GetEvent), new { publicId = createdEvent.PublicId }, createdEvent);
    }

    [HttpPut("{publicId}")]
    public async Task<IActionResult> UpdateEvent(string publicId, EventModel eventModel)
    {
        try
        {
            await _eventService.UpdateEventByPublicIdAsync(publicId, eventModel);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpDelete("{publicId}")]
    public async Task<IActionResult> DeleteEvent(string publicId)
    {
        try
        {
            await _eventService.DeleteEventByPublicIdAsync(publicId);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPost("{eventPublicId}/users")]
    public async Task<IActionResult> AddUserToEvent(string eventPublicId, [FromBody] AddUserToEventRequest request)
    {
        var result = await _eventService.AddUserToEventByPublicIdAsync(eventPublicId, request.UserId, request.Role);
        if (!result)
        {
            return NotFound();
        }
        return NoContent();
    }

    [HttpDelete("{eventPublicId}/users/{userId}")]
    public async Task<IActionResult> RemoveUserFromEvent(string eventPublicId, string userId)
    {
        var result = await _eventService.RemoveUserFromEventByPublicIdAsync(eventPublicId, userId);
        if (!result)
        {
            return NotFound();
        }
        return NoContent();
    }

    [HttpGet("{eventPublicId}/wishlists")]
    public async Task<ActionResult<IEnumerable<WishlistModel>>> GetEventWishlists(string eventPublicId)
    {
        var wishlists = await _eventService.GetEventWishlistsByPublicIdAsync(eventPublicId);
        return Ok(wishlists);
    }

    [HttpPost("{eventPublicId}/wishlists")]
    public async Task<ActionResult<WishlistModel>> CreateEventWishlist(string eventPublicId, [FromBody] WishlistModel wishlist)
    {
        var userId = await _userContextService.GetUserIdAsync();
        if (userId is null)
        {
            return Unauthorized();
        }

        try
        {
            var createdWishlist = await _eventService.CreateEventWishlistByPublicIdAsync(eventPublicId, wishlist, userId);
            return Ok(createdWishlist);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
    }

    [HttpPost("{eventPublicId}/wishlists/attach")]
    public async Task<ActionResult<WishlistModel>> AttachWishlistToEvent(string eventPublicId, [FromBody] AttachWishlistToEventRequest request)
    {
        var userId = await _userContextService.GetUserIdAsync();
        if (userId is null)
        {
            return Unauthorized();
        }

        try
        {
            var wishlist = await _eventService.AttachWishlistByPublicIdAsync(eventPublicId, request.WishlistPublicId, userId);
            return Ok(wishlist);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpDelete("{eventPublicId}/wishlists/{wishlistPublicId}")]
    public async Task<IActionResult> DetachWishlistFromEvent(string eventPublicId, string wishlistPublicId)
    {
        var userId = await _userContextService.GetUserIdAsync();
        if (userId is null)
        {
            return Unauthorized();
        }

        try
        {
            var result = await _eventService.DetachWishlistByPublicIdAsync(eventPublicId, wishlistPublicId, userId);
            if (!result)
            {
                return NotFound();
            }

            return NoContent();
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
    }

    [HttpGet("{eventPublicId}/reservations/mine")]
    public async Task<ActionResult<IEnumerable<EventReservedItemModel>>> GetMyReservedItems(string eventPublicId)
    {
        var userId = await _userContextService.GetUserIdAsync();
        if (userId is null)
        {
            return Unauthorized();
        }

        try
        {
            var items = await _eventService.GetReservedItemsForUserByPublicIdAsync(eventPublicId, userId);
            return Ok(items);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
    }

    // Event Invitation Endpoints

    [HttpPost("{eventPublicId}/invitations/user/{userId}")]
    public async Task<ActionResult<EventUserModel>> InviteUserToEvent(string eventPublicId, string userId)
    {
        var inviterId = await _userContextService.GetUserIdAsync();
        if (inviterId is null)
        {
            return Unauthorized();
        }
        try
        {
            var invitation = await _eventService.InviteUserToEventByPublicIdAsync(eventPublicId, inviterId, userId);
            return Ok(invitation);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("{eventPublicId}/invitations/email")]
    public async Task<ActionResult<EventUserModel>> InviteByEmailToEvent(string eventPublicId, [FromBody] InviteByEmailRequest request)
    {
        var inviterId = await _userContextService.GetUserIdAsync();
        if (inviterId is null)
        {
            return Unauthorized();
        }
        try
        {
            var invitation = await _eventService.InviteByEmailToEventByPublicIdAsync(eventPublicId, inviterId, request.Email);
            return Ok(invitation);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("{eventPublicId}/invitations")]
    public async Task<ActionResult<IEnumerable<EventUserModel>>> GetEventInvitations(string eventPublicId)
    {
        var invitations = await _eventService.GetEventInvitationsByPublicIdAsync(eventPublicId);
        return Ok(invitations);
    }

    [HttpPost("invitations/{eventUserId}/accept")]
    public async Task<IActionResult> AcceptEventInvitation(int eventUserId)
    {
        var userId = await _userContextService.GetUserIdAsync();
        if (userId is null)
        {
            return Unauthorized();
        }
        var result = await _eventService.AcceptEventInvitationAsync(eventUserId, userId);
        if (!result)
        {
            return NotFound();
        }
        return NoContent();
    }

    [HttpPost("invitations/{eventUserId}/reject")]
    public async Task<IActionResult> RejectEventInvitation(int eventUserId)
    {
        var userId = await _userContextService.GetUserIdAsync();
        if (userId is null)
        {
            return Unauthorized();
        }
        var result = await _eventService.RejectEventInvitationAsync(eventUserId, userId);
        if (!result)
        {
            return NotFound();
        }
        return NoContent();
    }

    [HttpDelete("invitations/{eventUserId}")]
    public async Task<IActionResult> CancelEventInvitation(int eventUserId)
    {
        var inviterId = await _userContextService.GetUserIdAsync();
        if (inviterId is null)
        {
            return Unauthorized();
        }
        try
        {
            var result = await _eventService.CancelEventInvitationAsync(eventUserId, inviterId);
            if (!result)
            {
                return NotFound();
            }
            return NoContent();
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
    }

    [HttpPost("invitations/{eventUserId}/resend")]
    public async Task<IActionResult> ResendEventInvitation(int eventUserId)
    {
        var inviterId = await _userContextService.GetUserIdAsync();
        if (inviterId is null)
        {
            return Unauthorized();
        }
        try
        {
            var result = await _eventService.ResendEventInvitationAsync(eventUserId, inviterId);
            if (!result)
            {
                return NotFound();
            }
            return NoContent();
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
    }

    // Gift Exchange Endpoints

    [HttpPost("{eventPublicId}/draw-names")]
    public async Task<ActionResult<EventModel>> DrawNames(string eventPublicId)
    {
        var userId = await _userContextService.GetUserIdAsync();
        if (userId is null)
        {
            return Unauthorized();
        }
        try
        {
            var eventModel = await _eventService.DrawNamesByPublicIdAsync(eventPublicId, userId);
            return Ok(eventModel);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("{eventPublicId}/my-gift-exchange")]
    public async Task<ActionResult<GiftExchangeModel>> GetMyGiftExchange(string eventPublicId)
    {
        var userId = await _userContextService.GetUserIdAsync();
        if (userId is null)
        {
            return Unauthorized();
        }
        try
        {
            var giftExchange = await _eventService.GetMyGiftExchangeByPublicIdAsync(eventPublicId, userId);
            if (giftExchange == null)
            {
                return NotFound("No gift exchange assignment found.");
            }
            return Ok(giftExchange);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
    }

    [HttpGet("{eventPublicId}/pairing-rules")]
    public async Task<ActionResult<IEnumerable<CustomPairingRuleModel>>> GetPairingRules(string eventPublicId)
    {
        try
        {
            var rules = await _eventService.GetPairingRulesByPublicIdAsync(eventPublicId);
            return Ok(rules);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }

    [HttpPost("{eventPublicId}/pairing-rules")]
    public async Task<ActionResult<CustomPairingRuleModel>> AddPairingRule(string eventPublicId, [FromBody] CustomPairingRuleModel rule)
    {
        var userId = await _userContextService.GetUserIdAsync();
        if (userId is null)
        {
            return Unauthorized();
        }
        try
        {
            var createdRule = await _eventService.AddPairingRuleByPublicIdAsync(eventPublicId, rule, userId);
            return Ok(createdRule);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpDelete("pairing-rules/{ruleId}")]
    public async Task<IActionResult> RemovePairingRule(int ruleId)
    {
        var userId = await _userContextService.GetUserIdAsync();
        if (userId is null)
        {
            return Unauthorized();
        }
        try
        {
            var result = await _eventService.RemovePairingRuleAsync(ruleId, userId);
            if (!result)
            {
                return NotFound();
            }
            return NoContent();
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }
}