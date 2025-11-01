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

    [HttpGet("{id}")]
    public async Task<ActionResult<EventModel>> GetEvent(int id)
    {
        try
        {
            var evt = await _eventService.GetEventAsync(id);
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
        return CreatedAtAction(nameof(GetEvent), new { id = createdEvent.Id }, createdEvent);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateEvent(int id, EventModel eventModel)
    {
        if (id != eventModel.Id)
        {
            return BadRequest();
        }
        try
        {
            await _eventService.UpdateEventAsync(id, eventModel);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteEvent(int id)
    {
        try
        {
            await _eventService.DeleteEventAsync(id);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPost("{eventId}/users")]
    public async Task<IActionResult> AddUserToEvent(int eventId, [FromBody] AddUserToEventRequest request)
    {
        var result = await _eventService.AddUserToEventAsync(eventId, request.UserId, request.Role);
        if (!result)
        {
            return NotFound();
        }
        return NoContent();
    }

    [HttpDelete("{eventId}/users/{userId}")]
    public async Task<IActionResult> RemoveUserFromEvent(int eventId, string userId)
    {
        var result = await _eventService.RemoveUserFromEventAsync(eventId, userId);
        if (!result)
        {
            return NotFound();
        }
        return NoContent();
    }

    // Event Invitation Endpoints

    [HttpPost("{eventId}/invitations/user/{userId}")]
    public async Task<ActionResult<EventUserModel>> InviteUserToEvent(int eventId, string userId)
    {
        var inviterId = await _userContextService.GetUserIdAsync();
        if (inviterId is null)
        {
            return Unauthorized();
        }
        try
        {
            var invitation = await _eventService.InviteUserToEventAsync(eventId, inviterId, userId);
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

    [HttpPost("{eventId}/invitations/email")]
    public async Task<ActionResult<EventUserModel>> InviteByEmailToEvent(int eventId, [FromBody] InviteByEmailRequest request)
    {
        var inviterId = await _userContextService.GetUserIdAsync();
        if (inviterId is null)
        {
            return Unauthorized();
        }
        try
        {
            var invitation = await _eventService.InviteByEmailToEventAsync(eventId, inviterId, request.Email);
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

    [HttpGet("{eventId}/invitations")]
    public async Task<ActionResult<IEnumerable<EventUserModel>>> GetEventInvitations(int eventId)
    {
        var invitations = await _eventService.GetEventInvitationsAsync(eventId);
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
}