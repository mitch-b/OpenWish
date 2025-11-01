using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenWish.Application.Models.Configuration;
using OpenWish.Data;
using OpenWish.Data.Entities;
using OpenWish.Shared.Models;
using OpenWish.Shared.Services;

namespace OpenWish.Application.Services;

public class EventService(
    ApplicationDbContext context,
    IMapper mapper,
    INotificationService notificationService,
    IAppEmailSender emailSender,
    IOptions<OpenWishSettings> openWishSettings,
    ILogger<EventService> logger) : IEventService
{
    private readonly ApplicationDbContext _context = context;
    private readonly IMapper _mapper = mapper;
    private readonly INotificationService _notificationService = notificationService;
    private readonly IAppEmailSender _emailSender = emailSender;
    private readonly string? _baseUri = openWishSettings.Value.BaseUri;
    private readonly ILogger<EventService> _logger = logger;

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
        {
            throw new KeyNotFoundException($"Event with id {id} not found");
        }

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
        {
            return false;
        }

        _context.EventUsers.Remove(eventUser);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<EventUserModel> InviteUserToEventAsync(int eventId, string inviterId, string userId)
    {
        var eventEntity = await _context.Events
            .Include(e => e.CreatedBy)
            .FirstOrDefaultAsync(e => e.Id == eventId);

        if (eventEntity == null)
        {
            throw new KeyNotFoundException($"Event with id {eventId} not found");
        }

        // Verify inviter is the event creator or has permission
        if (eventEntity.CreatedBy.Id != inviterId)
        {
            throw new UnauthorizedAccessException("Only the event creator can invite users");
        }

        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            throw new KeyNotFoundException($"User with id {userId} not found");
        }

        // Check if invitation already exists
        var existingInvite = await _context.EventUsers
            .FirstOrDefaultAsync(eu => eu.EventId == eventId && eu.UserId == userId && !eu.Deleted);

        if (existingInvite != null)
        {
            throw new InvalidOperationException("User is already invited to this event");
        }

        var eventUser = new EventUser
        {
            EventId = eventId,
            UserId = userId,
            InvitationDate = DateTimeOffset.UtcNow,
            Status = "Pending",
            IsAccepted = false,
            Role = "Participant",
            CreatedOn = DateTimeOffset.UtcNow,
            UpdatedOn = DateTimeOffset.UtcNow
        };

        _context.EventUsers.Add(eventUser);
        await _context.SaveChangesAsync();

        // Send notification
        await _notificationService.CreateNotificationAsync(
            inviterId,
            userId,
            "Event Invitation",
            $"You've been invited to the event: {eventEntity.Name}",
            "EventInvite");

        // Send email notification
        var inviter = await _context.Users.FindAsync(inviterId);
        var baseUri = _baseUri?.TrimEnd('/') ?? "";
        var inviteLink = $"{baseUri}/events/{eventId}";
        await _emailSender.SendEventInviteEmailAsync(
            user.Email ?? string.Empty,
            inviter?.UserName ?? inviter?.Email ?? "Someone",
            eventEntity.Name,
            inviteLink);

        return _mapper.Map<EventUserModel>(eventUser);
    }

    public async Task<EventUserModel> InviteByEmailToEventAsync(int eventId, string inviterId, string email)
    {
        var eventEntity = await _context.Events
            .Include(e => e.CreatedBy)
            .FirstOrDefaultAsync(e => e.Id == eventId);

        if (eventEntity == null)
        {
            throw new KeyNotFoundException($"Event with id {eventId} not found");
        }

        // Verify inviter is the event creator
        if (eventEntity.CreatedBy.Id != inviterId)
        {
            throw new UnauthorizedAccessException("Only the event creator can invite users");
        }

        // Check if user with this email exists
        var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (existingUser != null)
        {
            // User exists, send regular invitation
            return await InviteUserToEventAsync(eventId, inviterId, existingUser.Id);
        }

        // Check if email invitation already exists
        var existingEmailInvite = await _context.EventUsers
            .FirstOrDefaultAsync(eu => eu.EventId == eventId && eu.InviteeEmail == email && !eu.Deleted);

        if (existingEmailInvite != null)
        {
            throw new InvalidOperationException("This email is already invited to the event");
        }

        var eventUser = new EventUser
        {
            EventId = eventId,
            InviteeEmail = email,
            InvitationDate = DateTimeOffset.UtcNow,
            Status = "Pending",
            IsAccepted = false,
            Role = "Participant",
            CreatedOn = DateTimeOffset.UtcNow,
            UpdatedOn = DateTimeOffset.UtcNow
        };

        _context.EventUsers.Add(eventUser);
        await _context.SaveChangesAsync();

        // Send email invitation
        var inviter = await _context.Users.FindAsync(inviterId);
        var baseUri = _baseUri?.TrimEnd('/') ?? "";
        var inviteLink = $"{baseUri}/events/{eventId}/accept-invite?email={Uri.EscapeDataString(email)}";
        await _emailSender.SendEventInviteEmailAsync(
            email,
            inviter?.UserName ?? inviter?.Email ?? "Someone",
            eventEntity.Name,
            inviteLink);

        return _mapper.Map<EventUserModel>(eventUser);
    }

    public async Task<IEnumerable<EventUserModel>> GetEventInvitationsAsync(int eventId)
    {
        var invitations = await _context.EventUsers
            .Include(eu => eu.User)
            .Include(eu => eu.Event)
            .Where(eu => eu.EventId == eventId && !eu.Deleted)
            .OrderByDescending(eu => eu.InvitationDate)
            .ToListAsync();

        return _mapper.Map<IEnumerable<EventUserModel>>(invitations);
    }

    public async Task<bool> AcceptEventInvitationAsync(int eventUserId, string userId)
    {
        var eventUser = await _context.EventUsers
            .Include(eu => eu.Event)
            .FirstOrDefaultAsync(eu => eu.Id == eventUserId && eu.UserId == userId && !eu.Deleted);

        if (eventUser == null)
        {
            return false;
        }

        if (eventUser.Status != "Pending")
        {
            return false;
        }

        eventUser.Status = "Accepted";
        eventUser.IsAccepted = true;
        eventUser.UpdatedOn = DateTimeOffset.UtcNow;

        await _context.SaveChangesAsync();

        // Notify event creator
        await _notificationService.CreateNotificationAsync(
            userId,
            eventUser.Event.CreatedBy.Id,
            "Invitation Accepted",
            $"A user has accepted the invitation to {eventUser.Event.Name}",
            "EventInviteAccept");

        return true;
    }

    public async Task<bool> RejectEventInvitationAsync(int eventUserId, string userId)
    {
        var eventUser = await _context.EventUsers
            .Include(eu => eu.Event)
            .FirstOrDefaultAsync(eu => eu.Id == eventUserId && eu.UserId == userId && !eu.Deleted);

        if (eventUser == null)
        {
            return false;
        }

        if (eventUser.Status != "Pending")
        {
            return false;
        }

        eventUser.Status = "Rejected";
        eventUser.IsAccepted = false;
        eventUser.UpdatedOn = DateTimeOffset.UtcNow;

        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> CancelEventInvitationAsync(int eventUserId, string inviterId)
    {
        var eventUser = await _context.EventUsers
            .Include(eu => eu.Event)
            .FirstOrDefaultAsync(eu => eu.Id == eventUserId && !eu.Deleted);

        if (eventUser == null)
        {
            return false;
        }

        // Verify inviter is the event creator
        if (eventUser.Event.CreatedBy.Id != inviterId)
        {
            throw new UnauthorizedAccessException("Only the event creator can cancel invitations");
        }

        eventUser.Deleted = true;
        eventUser.UpdatedOn = DateTimeOffset.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ResendEventInvitationAsync(int eventUserId, string inviterId)
    {
        var eventUser = await _context.EventUsers
            .Include(eu => eu.Event)
            .Include(eu => eu.User)
            .FirstOrDefaultAsync(eu => eu.Id == eventUserId && !eu.Deleted);

        if (eventUser == null)
        {
            return false;
        }

        // Verify inviter is the event creator
        if (eventUser.Event.CreatedBy.Id != inviterId)
        {
            throw new UnauthorizedAccessException("Only the event creator can resend invitations");
        }

        if (eventUser.Status != "Pending")
        {
            return false;
        }

        var inviter = await _context.Users.FindAsync(inviterId);
        var baseUri = _baseUri?.TrimEnd('/') ?? "";

        if (eventUser.UserId != null && eventUser.User != null)
        {
            // Registered user invitation
            var inviteLink = $"{baseUri}/events/{eventUser.EventId}";
            await _emailSender.SendEventInviteEmailAsync(
                eventUser.User.Email ?? string.Empty,
                inviter?.UserName ?? inviter?.Email ?? "Someone",
                eventUser.Event.Name,
                inviteLink);

            // Send notification
            await _notificationService.CreateNotificationAsync(
                inviterId,
                eventUser.UserId,
                "Event Invitation Reminder",
                $"Reminder: You've been invited to the event: {eventUser.Event.Name}",
                "EventInvite");
        }
        else if (eventUser.InviteeEmail != null)
        {
            // Email invitation
            var inviteLink = $"{baseUri}/events/{eventUser.EventId}/accept-invite?email={Uri.EscapeDataString(eventUser.InviteeEmail)}";
            await _emailSender.SendEventInviteEmailAsync(
                eventUser.InviteeEmail,
                inviter?.UserName ?? inviter?.Email ?? "Someone",
                eventUser.Event.Name,
                inviteLink);
        }

        eventUser.InvitationDate = DateTimeOffset.UtcNow;
        eventUser.UpdatedOn = DateTimeOffset.UtcNow;
        await _context.SaveChangesAsync();

        return true;
    }
}