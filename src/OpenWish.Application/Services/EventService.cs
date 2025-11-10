using System;
using System.Linq;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenWish.Application.Models.Configuration;
using OpenWish.Data;
using OpenWish.Data.Entities;
using OpenWish.Shared.Models;
using OpenWish.Shared.Services;

namespace OpenWish.Application.Services;

public class EventService(
    IServiceScopeFactory scopeFactory,
    IMapper mapper,
    INotificationService notificationService,
    IAppEmailSender emailSender,
    IOptions<OpenWishSettings> openWishSettings,
    ILogger<EventService> logger) : IEventService
{
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
    private readonly IMapper _mapper = mapper;
    private readonly INotificationService _notificationService = notificationService;
    private readonly IAppEmailSender _emailSender = emailSender;
    private readonly string? _baseUri = openWishSettings.Value.BaseUri;
    private readonly ILogger<EventService> _logger = logger;

    private static bool IsEventOwner(Event eventEntity, string userId) =>
        string.Equals(eventEntity.CreatedBy?.Id, userId, StringComparison.Ordinal);

    private static bool IsEventMember(Event eventEntity, string userId)
    {
        if (IsEventOwner(eventEntity, userId))
        {
            return true;
        }

        return eventEntity.EventUsers.Any(eu =>
            !eu.Deleted &&
            string.Equals(eu.UserId, userId, StringComparison.Ordinal));
    }

    public async Task<EventModel> CreateEventAsync(EventModel eventModel, string creatorId)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var creator = await context.Users.FindAsync(creatorId)
                ?? throw new KeyNotFoundException($"User with id {creatorId} not found");

        var eventEntity = _mapper.Map<Event>(eventModel);
        eventEntity.CreatedBy = creator;
        eventEntity.CreatedOn = DateTimeOffset.UtcNow;

        context.Events.Add(eventEntity);
        await context.SaveChangesAsync();

        var resultModel = _mapper.Map<EventModel>(eventEntity);
        return resultModel;
    }

    public async Task<EventModel> GetEventAsync(int id)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var eventEntity = await context.Events
            .AsNoTracking()
                    .Include(e => e.CreatedBy)
                    .Include(e => e.EventUsers)
                        .ThenInclude(eu => eu.User)
                    .Include(e => e.EventWishlists
                        .Where(w => !w.Deleted))
                        .ThenInclude(ew => ew.Owner)
                    .Include(e => e.GiftExchanges)
                        .ThenInclude(ge => ge.Receiver)
                    .FirstOrDefaultAsync(e => e.Id == id && !e.Deleted);

        if (eventEntity == null)
        {
            throw new KeyNotFoundException($"Event with id {id} not found");
        }

        var eventModel = _mapper.Map<EventModel>(eventEntity);
        return eventModel;
    }

    public async Task<IEnumerable<EventModel>> GetUserEventsAsync(string userId)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var eventEntities = await context.Events
            .AsNoTracking()
            .Include(e => e.CreatedBy)
            .Include(e => e.EventUsers)
                .ThenInclude(eu => eu.User)
            .Include(e => e.EventWishlists
                .Where(w => !w.Deleted))
                .ThenInclude(ew => ew.Owner)
            .Where(e => !e.Deleted &&
                (e.CreatedBy.Id == userId ||
                 e.EventUsers.Any(eu => !eu.Deleted && eu.UserId == userId)))
            .OrderBy(e => e.Date)
            .ToListAsync();

        var eventModels = _mapper.Map<IEnumerable<EventModel>>(eventEntities);
        return eventModels;
    }

    public async Task<IEnumerable<WishlistModel>> GetEventWishlistsAsync(int eventId)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var wishlistEntities = await context.Wishlists
            .AsNoTracking()
            .Where(w => w.EventId == eventId && !w.Deleted)
            .Include(w => w.Owner)
            .Include(w => w.Items)
            .OrderBy(w => w.Name)
            .ToListAsync();

        return _mapper.Map<IEnumerable<WishlistModel>>(wishlistEntities);
    }

    public async Task<WishlistModel> CreateEventWishlistAsync(int eventId, WishlistModel wishlistModel, string ownerId)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var eventEntity = await context.Events
            .Include(e => e.CreatedBy)
            .Include(e => e.EventUsers)
            .FirstOrDefaultAsync(e => e.Id == eventId && !e.Deleted)
            ?? throw new KeyNotFoundException($"Event with id {eventId} not found");

        if (!IsEventMember(eventEntity, ownerId))
        {
            throw new UnauthorizedAccessException("You must be part of this event to create a wishlist.");
        }

        var wishlistService = scope.ServiceProvider.GetRequiredService<IWishlistService>();
        wishlistModel.EventId = eventId;

        return await wishlistService.CreateWishlistAsync(wishlistModel, ownerId);
    }

    public async Task<WishlistModel> AttachWishlistAsync(int eventId, int wishlistId, string userId)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var eventEntity = await context.Events
            .Include(e => e.CreatedBy)
            .Include(e => e.EventUsers)
            .FirstOrDefaultAsync(e => e.Id == eventId && !e.Deleted)
            ?? throw new KeyNotFoundException($"Event with id {eventId} not found");

        var wishlist = await context.Wishlists
            .Include(w => w.Owner)
            .Include(w => w.Items)
            .FirstOrDefaultAsync(w => w.Id == wishlistId && !w.Deleted)
            ?? throw new KeyNotFoundException($"Wishlist {wishlistId} not found");

        if (wishlist.EventId.HasValue && wishlist.EventId != eventId)
        {
            throw new InvalidOperationException("Wishlist is already attached to another event.");
        }

        if (!IsEventMember(eventEntity, userId))
        {
            throw new UnauthorizedAccessException("You must be part of this event to attach a wishlist.");
        }

        if (string.IsNullOrEmpty(wishlist.OwnerId))
        {
            throw new InvalidOperationException("Wishlist must have an owner before being attached to an event.");
        }

        if (!IsEventMember(eventEntity, wishlist.OwnerId))
        {
            throw new InvalidOperationException("Wishlist owner must be part of the event.");
        }

        var isWishlistOwner = string.Equals(wishlist.OwnerId, userId, StringComparison.Ordinal);

        if (!isWishlistOwner && !IsEventOwner(eventEntity, userId))
        {
            throw new UnauthorizedAccessException("Only the wishlist owner or event owner can attach this wishlist.");
        }

        wishlist.EventId = eventId;
        wishlist.UpdatedOn = DateTimeOffset.UtcNow;

        await context.SaveChangesAsync();

        return _mapper.Map<WishlistModel>(wishlist);
    }

    public async Task<bool> DetachWishlistAsync(int eventId, int wishlistId, string userId)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var eventEntity = await context.Events
            .Include(e => e.CreatedBy)
            .Include(e => e.EventUsers)
            .FirstOrDefaultAsync(e => e.Id == eventId && !e.Deleted);

        if (eventEntity == null)
        {
            return false;
        }

        var wishlist = await context.Wishlists
            .FirstOrDefaultAsync(w => w.Id == wishlistId && !w.Deleted);

        if (wishlist == null || wishlist.EventId != eventId)
        {
            return false;
        }

        var isWishlistOwner = string.Equals(wishlist.OwnerId, userId, StringComparison.Ordinal);

        if (!isWishlistOwner && !IsEventOwner(eventEntity, userId))
        {
            throw new UnauthorizedAccessException("Only the wishlist owner or event owner can detach this wishlist.");
        }

        wishlist.EventId = null;
        wishlist.UpdatedOn = DateTimeOffset.UtcNow;

        await context.SaveChangesAsync();
        return true;
    }

    public async Task<EventModel> UpdateEventAsync(int id, EventModel eventModel)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var existingEvent = await context.Events.FindAsync(id)
                ?? throw new KeyNotFoundException($"Event with id {id} not found");

        _mapper.Map(eventModel, existingEvent);
        existingEvent.UpdatedOn = DateTimeOffset.UtcNow;

        await context.SaveChangesAsync();

        var updatedModel = _mapper.Map<EventModel>(existingEvent);
        return updatedModel;
    }

    public async Task DeleteEventAsync(int id)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var eventEntity = await context.Events
                .Include(e => e.EventWishlists)
                .FirstOrDefaultAsync(e => e.Id == id)
                ?? throw new KeyNotFoundException($"Event with id {id} not found");

        eventEntity.Deleted = true;
        eventEntity.UpdatedOn = DateTimeOffset.UtcNow;

        foreach (var wishlist in eventEntity.EventWishlists.Where(w => !w.Deleted))
        {
            wishlist.EventId = null;
            wishlist.UpdatedOn = DateTimeOffset.UtcNow;
        }

        await context.SaveChangesAsync();
    }

    public async Task<bool> AddUserToEventAsync(int eventId, string userId, string role = "Participant")
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var eventEntity = await context.Events.Include(e => e.EventUsers)
                .FirstOrDefaultAsync(e => e.Id == eventId);
        var user = await context.Users.FindAsync(userId);

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
            await context.SaveChangesAsync();
        }
        return true;
    }

    public async Task<bool> RemoveUserFromEventAsync(int eventId, string userId)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var eventUser = await context.EventUsers
                .FirstOrDefaultAsync(eu => eu.EventId == eventId && eu.UserId == userId);

        if (eventUser == null)
        {
            return false;
        }

        var wishlists = await context.Wishlists
                .Where(w => w.EventId == eventId && w.OwnerId == userId && !w.Deleted)
                .ToListAsync();

        context.EventUsers.Remove(eventUser);

        foreach (var wishlist in wishlists)
        {
            wishlist.EventId = null;
            wishlist.UpdatedOn = DateTimeOffset.UtcNow;
        }

        await context.SaveChangesAsync();
        return true;
    }

    public async Task<EventUserModel> InviteUserToEventAsync(int eventId, string inviterId, string userId)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var eventEntity = await context.Events
            .Include(e => e.CreatedBy)
            .FirstOrDefaultAsync(e => e.Id == eventId);

        if (eventEntity == null)
        {
            throw new KeyNotFoundException($"Event with id {eventId} not found");
        }

        // Verify inviter is the event creator or has permission
        ValidateEventCreatorPermission(eventEntity, inviterId);

        var user = await context.Users.FindAsync(userId);
        if (user == null)
        {
            throw new KeyNotFoundException($"User with id {userId} not found");
        }

        // Check if invitation already exists
        var existingInvite = await context.EventUsers
            .AsNoTracking()
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

        context.EventUsers.Add(eventUser);
        await context.SaveChangesAsync();

        // Send notification
        await _notificationService.CreateNotificationAsync(
            inviterId,
            userId,
            "Event Invitation",
            $"You've been invited to the event: {eventEntity.Name}",
            "EventInvite");

        // Send email notification
        var inviter = await context.Users.FindAsync(inviterId);
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
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var eventEntity = await context.Events
                .Include(e => e.CreatedBy)
                .FirstOrDefaultAsync(e => e.Id == eventId);

        if (eventEntity == null)
        {
            throw new KeyNotFoundException($"Event with id {eventId} not found");
        }

        // Verify inviter is the event creator
        ValidateEventCreatorPermission(eventEntity, inviterId);

        // Check if user with this email exists
        var existingUser = await context.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (existingUser != null)
        {
            // User exists, send regular invitation
            return await InviteUserToEventAsync(eventId, inviterId, existingUser.Id);
        }

        // Check if email invitation already exists
        var existingEmailInvite = await context.EventUsers
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

        context.EventUsers.Add(eventUser);
        await context.SaveChangesAsync();

        // Send email invitation
        var inviter = await context.Users.FindAsync(inviterId);
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
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var invitations = await context.EventUsers
            .AsNoTracking()
                    .Include(eu => eu.User)
                    .Include(eu => eu.Event)
                    .Where(eu => eu.EventId == eventId && !eu.Deleted)
                    .OrderByDescending(eu => eu.InvitationDate)
                    .ToListAsync();

        return _mapper.Map<IEnumerable<EventUserModel>>(invitations);
    }

    public async Task<bool> AcceptEventInvitationAsync(int eventUserId, string userId)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var eventUser = await context.EventUsers
                .Include(eu => eu.Event)
                    .ThenInclude(e => e.CreatedBy)
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

        await context.SaveChangesAsync();

        // Notify event creator (guard against missing navigation)
        var user = await context.Users.FindAsync(userId);
        var userName = user?.UserName ?? user?.Email ?? "A user";
        var creatorId = eventUser.Event.CreatedBy?.Id;

        if (!string.IsNullOrEmpty(creatorId))
        {
            await _notificationService.CreateNotificationAsync(
                userId,
                creatorId,
                "Invitation Accepted",
                $"{userName} has accepted the invitation to {eventUser.Event.Name}",
                "EventInviteAccept");
        }
        else
        {
            _logger.LogWarning("Event creator not loaded for EventUserId {EventUserId} (EventId {EventId})", eventUserId, eventUser.EventId);
        }

        return true;
    }

    public async Task<bool> RejectEventInvitationAsync(int eventUserId, string userId)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var eventUser = await context.EventUsers
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

        await context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> CancelEventInvitationAsync(int eventUserId, string inviterId)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var eventUser = await context.EventUsers
                .Include(eu => eu.Event)
                    .ThenInclude(e => e.CreatedBy)
                .FirstOrDefaultAsync(eu => eu.Id == eventUserId && !eu.Deleted);

        if (eventUser == null)
        {
            return false;
        }

        // Verify inviter is the event creator
        ValidateEventCreatorPermission(eventUser.Event, inviterId);

        eventUser.Deleted = true;
        eventUser.UpdatedOn = DateTimeOffset.UtcNow;

        await context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ResendEventInvitationAsync(int eventUserId, string inviterId)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var eventUser = await context.EventUsers
                .Include(eu => eu.Event)
                    .ThenInclude(e => e.CreatedBy)
                .Include(eu => eu.User)
                .FirstOrDefaultAsync(eu => eu.Id == eventUserId && !eu.Deleted);

        if (eventUser == null)
        {
            return false;
        }

        // Verify inviter is the event creator
        ValidateEventCreatorPermission(eventUser.Event, inviterId);

        if (eventUser.Status != "Pending")
        {
            return false;
        }

        var inviter = await context.Users.FindAsync(inviterId);
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
        await context.SaveChangesAsync();

        return true;
    }

    private static void ValidateEventCreatorPermission(Event eventEntity, string userId)
    {
        if (eventEntity.CreatedBy.Id != userId)
        {
            throw new UnauthorizedAccessException("Only the event creator can perform this action");
        }
    }
}