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
public class EventController(IEventService eventService, IUserContextService userContextService) : ControllerBase
{
    private readonly IEventService _eventService = eventService;
    private readonly IUserContextService _userContextService = userContextService;

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
}

