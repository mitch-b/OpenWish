using AutoMapper;
using Microsoft.EntityFrameworkCore;
using OpenWish.Data;
using OpenWish.Data.Entities;
using OpenWish.Shared.Models;
using OpenWish.Shared.Services;

namespace OpenWish.Application.Services;

public class EventService(ApplicationDbContext context, IMapper mapper) : IEventService
{
    private readonly ApplicationDbContext _context = context;
    private readonly IMapper _mapper = mapper;

    public async Task<EventModel> CreateEventAsync(EventModel eventModel, string creatorId)
    {
        var creator = await _context.Users.FindAsync(creatorId)
            ?? throw new KeyNotFoundException($"User with id {creatorId} not found");

        var eventEntity = _mapper.Map<Event>(eventModel);
        eventEntity.CreatedBy = creator;
        eventEntity.CreatedOn = DateTimeOffset.UtcNow;

        _context.Events.Add(eventEntity);
        await _context.SaveChangesAsync();

        var resultModel = _mapper.Map<EventModel>(eventEntity);
        return resultModel;
    }

    public async Task<EventModel> GetEventAsync(int id)
    {
        var eventEntity = await _context.Events
            .Include(e => e.CreatedBy)
            .Include(e => e.EventUsers)
                .ThenInclude(eu => eu.User)
            .Include(e => e.EventWishlists)
                .ThenInclude(ew => ew.Owner)
            .Include(e => e.GiftExchanges)
                .ThenInclude(ge => ge.Receiver)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (eventEntity == null)
            throw new KeyNotFoundException($"Event with id {id} not found");

        var eventModel = _mapper.Map<EventModel>(eventEntity);
        return eventModel;
    }

    public async Task<IEnumerable<EventModel>> GetUserEventsAsync(string userId)
    {
        var eventEntities = await _context.Events
            .Include(e => e.CreatedBy)
            .Where(e => e.CreatedBy.Id == userId)
            .Include(e => e.EventUsers)
                .ThenInclude(eu => eu.User)
            .Include(e => e.EventWishlists)
                .ThenInclude(ew => ew.Owner)
            .ToListAsync();

        var eventModels = _mapper.Map<IEnumerable<EventModel>>(eventEntities);
        return eventModels;
    }

    public async Task<EventModel> UpdateEventAsync(int id, EventModel eventModel)
    {
        var existingEvent = await _context.Events.FindAsync(id)
            ?? throw new KeyNotFoundException($"Event with id {id} not found");

        _mapper.Map(eventModel, existingEvent);
        existingEvent.UpdatedOn = DateTimeOffset.UtcNow;

        await _context.SaveChangesAsync();

        var updatedModel = _mapper.Map<EventModel>(existingEvent);
        return updatedModel;
    }

    public async Task DeleteEventAsync(int id)
    {
        var eventEntity = await _context.Events.FindAsync(id)
            ?? throw new KeyNotFoundException($"Event with id {id} not found");

        eventEntity.Deleted = true;
        eventEntity.UpdatedOn = DateTimeOffset.UtcNow;

        await _context.SaveChangesAsync();
    }

    public async Task<bool> AddUserToEventAsync(int eventId, string userId, string role = "Participant")
    {
        var eventEntity = await _context.Events.Include(e => e.EventUsers)
            .FirstOrDefaultAsync(e => e.Id == eventId);
        var user = await _context.Users.FindAsync(userId);

        if (eventEntity == null || user == null)
        {
            return false;
        }

        if (!eventEntity.EventUsers.Any(eu => eu.UserId == userId))
        {
            eventEntity.EventUsers.Add(new EventUser
            {
                EventId = eventId,
                UserId = userId,
                Role = role
            });
            await _context.SaveChangesAsync();
        }
        return true;
    }

    public async Task<bool> RemoveUserFromEventAsync(int eventId, string userId)
    {
        var eventUser = await _context.EventUsers
            .FirstOrDefaultAsync(eu => eu.EventId == eventId && eu.UserId == userId);

        if (eventUser == null)
            return false;

        _context.EventUsers.Remove(eventUser);
        await _context.SaveChangesAsync();
        return true;
    }
}
