using OpenWish.Data;
using OpenWish.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace OpenWish.Application.Services;

public interface IEventService
{
    Task<Event> CreateEventAsync(Event evt, string creatorId);
    Task<Event> GetEventAsync(int id);
    Task<IEnumerable<Event>> GetUserEventsAsync(string userId);
    Task<Event> UpdateEventAsync(int id, Event evt);
    Task DeleteEventAsync(int id);
    Task<bool> AddUserToEventAsync(int eventId, string userId, string role = "Participant");
}

public class EventService : IEventService
{
    private readonly ApplicationDbContext _context;

    public EventService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Event> CreateEventAsync(Event evt, string creatorId)
    {
        evt.CreatedBy = await _context.Users.FindAsync(creatorId);
        var entry = _context.Events.Add(evt);
        await _context.SaveChangesAsync();
        return entry.Entity;
    }

    public async Task<Event> GetEventAsync(int id)
    {
        return await _context.Events.FindAsync(id)
            ?? throw new KeyNotFoundException($"Event with id {id} not found");
    }

    public async Task<IEnumerable<Event>> GetUserEventsAsync(string userId)
    {
        return await _context.Events
            .Where(e => e.CreatedBy.Id == userId)
            .Include(e => e.EventUsers)
            .Include(e => e.EventWishlists)
            .ToListAsync();
    }

    public async Task<Event> UpdateEventAsync(int id, Event evt)
    {
        var existingEvent = await _context.Events.FindAsync(id)
            ?? throw new KeyNotFoundException($"Event with id {id} not found");

        _context.Entry(existingEvent).CurrentValues.SetValues(evt);
        await _context.SaveChangesAsync();
        return existingEvent;
    }

    public async Task DeleteEventAsync(int id)
    {
        var evt = await _context.Events.FindAsync(id)
            ?? throw new KeyNotFoundException($"Event with id {id} not found");
        evt.Deleted = true;
        await UpdateEventAsync(id, evt);
    }

    public async Task<bool> AddUserToEventAsync(int eventId, string userId, string role = "Participant")
    {
        var evt = await _context.Events.FindAsync(eventId);
        var user = await _context.Users.FindAsync(userId);
        if (evt == null || user == null)
        {
            return false;
        }

        evt.EventUsers.Add(new EventUser { Event = evt, User = user, Role = role });
        await _context.SaveChangesAsync();
        return true;
    }
}