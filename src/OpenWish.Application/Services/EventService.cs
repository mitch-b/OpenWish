using System;
using System.Collections.Generic;
using System.Globalization;
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

    private static void FilterGiftExchangeVisibility(EventModel eventModel, string? requestingUserId)
    {
        if (!eventModel.IsGiftExchange || eventModel.GiftExchanges is null || eventModel.GiftExchanges.Count == 0)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(requestingUserId))
        {
            eventModel.GiftExchanges = new List<GiftExchangeModel>();
            return;
        }

        if (string.Equals(eventModel.CreatedBy?.Id, requestingUserId, StringComparison.Ordinal))
        {
            return;
        }

        var assignment = eventModel.GiftExchanges
            .FirstOrDefault(ge => string.Equals(ge.GiverId, requestingUserId, StringComparison.Ordinal));

        eventModel.GiftExchanges = assignment != null
            ? new List<GiftExchangeModel> { assignment }
            : new List<GiftExchangeModel>();
    }

    private static void FilterWishlistVisibility(ICollection<WishlistModel>? wishlists, string? requestingUserId)
    {
        if (wishlists is null || wishlists.Count == 0)
        {
            return;
        }

        foreach (var wishlist in wishlists)
        {
            if (wishlist.Items is null || wishlist.Items.Count == 0)
            {
                continue;
            }

            var isOwner = !string.IsNullOrEmpty(requestingUserId) &&
                         !string.IsNullOrEmpty(wishlist.OwnerId) &&
                         string.Equals(wishlist.OwnerId, requestingUserId, StringComparison.Ordinal);

            var filteredItems = wishlist.Items
                .Where(item => ShouldIncludeWishlistItem(item, isOwner))
                .ToList();

            if (filteredItems.Count != wishlist.Items.Count)
            {
                wishlist.Items = filteredItems;
            }
        }
    }

    private static bool ShouldIncludeWishlistItem(WishlistItemModel item, bool isOwner) =>
        (!item.IsPrivate || isOwner) &&
        (!item.IsHiddenFromOwner || !isOwner);

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
            .Include(e => e.EventWishlists
                .Where(w => !w.Deleted))
                .ThenInclude(ew => ew.Items.Where(i => !i.Deleted))
            .Where(e => !e.Deleted &&
                (e.CreatedBy.Id == userId ||
                 e.EventUsers.Any(eu => !eu.Deleted && eu.UserId == userId)))
            .OrderBy(e => e.Date)
            .ToListAsync();

        var eventModels = _mapper.Map<List<EventModel>>(eventEntities);

        foreach (var eventModel in eventModels)
        {
            FilterGiftExchangeVisibility(eventModel, userId);
            FilterWishlistVisibility(eventModel.EventWishlists, userId);
        }

        return eventModels;
    }

    public async Task<IEnumerable<WishlistModel>> GetEventWishlistsAsync(int eventId, string? requestingUserId = null)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var wishlistEntities = await context.Wishlists
            .AsNoTracking()
            .Where(w => w.EventId == eventId && !w.Deleted)
            .Include(w => w.Owner)
            .Include(w => w.Items.Where(i => !i.Deleted))
            .OrderBy(w => w.Name)
            .ToListAsync();

        var wishlistModels = _mapper.Map<List<WishlistModel>>(wishlistEntities);
        FilterWishlistVisibility(wishlistModels, requestingUserId);

        return wishlistModels;
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
            .Include(w => w.Items.Where(i => !i.Deleted))
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

    public async Task<bool> RemoveUserFromEventAsync(int eventId, string userId, string requestorId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(requestorId);

        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var eventUser = await context.EventUsers
            .Include(eu => eu.Event)
                .ThenInclude(e => e.CreatedBy)
            .FirstOrDefaultAsync(eu =>
                eu.EventId == eventId &&
                eu.UserId == userId &&
                !eu.Deleted);

        if (eventUser?.Event == null)
        {
            return false;
        }

        ValidateEventCreatorPermission(eventUser.Event, requestorId);

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
            "EventInvite",
            BuildEventInvitationAction(eventEntity.PublicId, eventUser.Id));

        // Send email notification
        var inviter = await context.Users.FindAsync(inviterId);
        var baseUri = _baseUri?.TrimEnd('/') ?? "";
        var inviteLink = $"{baseUri}/events/{eventEntity.PublicId}";
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
        var inviteLink = $"{baseUri}/events/{eventEntity.PublicId}/accept-invite?email={Uri.EscapeDataString(email)}";
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
                "EventInviteAccept",
                new NotificationActionModel
                {
                    Type = "EventInvitationResponse",
                    NavigateTo = $"/events/{eventUser.Event.PublicId}"
                });
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
            var inviteLink = $"{baseUri}/events/{eventUser.Event.PublicId}";
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
                "EventInvite",
                BuildEventInvitationAction(eventUser.Event.PublicId, eventUser.Id));
        }
        else if (eventUser.InviteeEmail != null)
        {
            // Email invitation
            var inviteLink = $"{baseUri}/events/{eventUser.Event.PublicId}/accept-invite?email={Uri.EscapeDataString(eventUser.InviteeEmail)}";
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

    public async Task<IEnumerable<EventUserModel>> GetPendingInvitationsForUserAsync(string userId)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var user = await context.Users.FindAsync(userId);
        if (user == null)
        {
            return Enumerable.Empty<EventUserModel>();
        }

        await AttachEmailInvitationsToUserAsync(context, user);

        var pendingInvitations = await context.EventUsers
            .AsNoTracking()
            .Include(eu => eu.Event)
                .ThenInclude(e => e.CreatedBy)
            .Include(eu => eu.User)
            .Where(eu =>
                !eu.Deleted &&
                eu.UserId == userId &&
                eu.Status == "Pending" &&
                eu.Event != null &&
                !eu.Event.Deleted)
            .OrderByDescending(eu => eu.InvitationDate)
            .ToListAsync();

        return _mapper.Map<IEnumerable<EventUserModel>>(pendingInvitations);
    }

    // PublicId-based methods for public routes
    public async Task<EventModel> GetEventByPublicIdAsync(string publicId, string? requestingUserId = null)
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
            .FirstOrDefaultAsync(e => e.PublicId == publicId && !e.Deleted);

        if (eventEntity == null)
        {
            throw new KeyNotFoundException($"Event with publicId {publicId} not found");
        }

        var eventModel = _mapper.Map<EventModel>(eventEntity);
        FilterGiftExchangeVisibility(eventModel, requestingUserId);
        FilterWishlistVisibility(eventModel.EventWishlists, requestingUserId);
        return eventModel;
    }

    public async Task<EventModel> UpdateEventByPublicIdAsync(string publicId, EventModel evt)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var existingEvent = await context.Events
            .FirstOrDefaultAsync(e => e.PublicId == publicId && !e.Deleted)
            ?? throw new KeyNotFoundException($"Event with publicId {publicId} not found");

        return await UpdateEventAsync(existingEvent.Id, evt);
    }

    public async Task DeleteEventByPublicIdAsync(string publicId)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var existingEvent = await context.Events
            .FirstOrDefaultAsync(e => e.PublicId == publicId && !e.Deleted)
            ?? throw new KeyNotFoundException($"Event with publicId {publicId} not found");

        await DeleteEventAsync(existingEvent.Id);
    }

    public async Task<bool> AddUserToEventByPublicIdAsync(string eventPublicId, string userId, string role = "Participant")
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var eventEntity = await context.Events
            .FirstOrDefaultAsync(e => e.PublicId == eventPublicId && !e.Deleted)
            ?? throw new KeyNotFoundException($"Event with publicId {eventPublicId} not found");

        return await AddUserToEventAsync(eventEntity.Id, userId, role);
    }

    public async Task<bool> RemoveUserFromEventByPublicIdAsync(string eventPublicId, string userId, string requestorId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(requestorId);

        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var eventEntity = await context.Events
            .FirstOrDefaultAsync(e => e.PublicId == eventPublicId && !e.Deleted)
            ?? throw new KeyNotFoundException($"Event with publicId {eventPublicId} not found");

        return await RemoveUserFromEventAsync(eventEntity.Id, userId, requestorId);
    }

    public async Task<IEnumerable<WishlistModel>> GetEventWishlistsByPublicIdAsync(string eventPublicId, string? requestingUserId = null)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var eventEntity = await context.Events
            .FirstOrDefaultAsync(e => e.PublicId == eventPublicId && !e.Deleted)
            ?? throw new KeyNotFoundException($"Event with publicId {eventPublicId} not found");

        return await GetEventWishlistsAsync(eventEntity.Id, requestingUserId);
    }

    public async Task<WishlistModel> CreateEventWishlistByPublicIdAsync(string eventPublicId, WishlistModel wishlistModel, string ownerId)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var eventEntity = await context.Events
            .FirstOrDefaultAsync(e => e.PublicId == eventPublicId && !e.Deleted)
            ?? throw new KeyNotFoundException($"Event with publicId {eventPublicId} not found");

        return await CreateEventWishlistAsync(eventEntity.Id, wishlistModel, ownerId);
    }

    public async Task<WishlistModel> AttachWishlistByPublicIdAsync(string eventPublicId, string wishlistPublicId, string userId)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var eventEntity = await context.Events
            .FirstOrDefaultAsync(e => e.PublicId == eventPublicId && !e.Deleted)
            ?? throw new KeyNotFoundException($"Event with publicId {eventPublicId} not found");

        var wishlist = await context.Wishlists
            .FirstOrDefaultAsync(w => w.PublicId == wishlistPublicId && !w.Deleted)
            ?? throw new KeyNotFoundException($"Wishlist with publicId {wishlistPublicId} not found");

        return await AttachWishlistAsync(eventEntity.Id, wishlist.Id, userId);
    }

    public async Task<bool> DetachWishlistByPublicIdAsync(string eventPublicId, string wishlistPublicId, string userId)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var eventEntity = await context.Events
            .FirstOrDefaultAsync(e => e.PublicId == eventPublicId && !e.Deleted)
            ?? throw new KeyNotFoundException($"Event with publicId {eventPublicId} not found");

        var wishlist = await context.Wishlists
            .FirstOrDefaultAsync(w => w.PublicId == wishlistPublicId && !w.Deleted)
            ?? throw new KeyNotFoundException($"Wishlist with publicId {wishlistPublicId} not found");

        return await DetachWishlistAsync(eventEntity.Id, wishlist.Id, userId);
    }

    public async Task<IEnumerable<EventReservedItemModel>> GetReservedItemsForUserByPublicIdAsync(string eventPublicId, string userId)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var eventEntity = await context.Events
            .Include(e => e.CreatedBy)
            .Include(e => e.EventUsers)
            .FirstOrDefaultAsync(e => e.PublicId == eventPublicId && !e.Deleted)
            ?? throw new KeyNotFoundException($"Event with publicId {eventPublicId} not found");

        if (!IsEventMember(eventEntity, userId))
        {
            throw new UnauthorizedAccessException("You must be part of this event to view reserved items.");
        }

        var reservations = await context.ItemReservations
            .AsNoTracking()
            .Include(r => r.WishlistItem)
                .ThenInclude(i => i.Wishlist)
                    .ThenInclude(w => w.Owner)
            .Where(r => !r.Deleted &&
                        r.UserId == userId &&
                        r.WishlistItem != null &&
                        !r.WishlistItem.Deleted &&
                        r.WishlistItem.Wishlist != null &&
                        !r.WishlistItem.Wishlist.Deleted &&
                        r.WishlistItem.Wishlist.EventId == eventEntity.Id)
            .OrderBy(r => r.ReservationDate)
            .ThenBy(r => r.WishlistItem!.Wishlist!.Name)
            .ThenBy(r => r.WishlistItem!.Name)
            .ToListAsync();

        return reservations.Select(reservation =>
        {
            var wishlist = reservation.WishlistItem!.Wishlist!;
            var ownerName = wishlist.Owner?.UserName ?? wishlist.Owner?.Email;
            if (string.IsNullOrWhiteSpace(ownerName) && string.IsNullOrWhiteSpace(wishlist.OwnerId))
            {
                ownerName = "Shared Event List";
            }

            return new EventReservedItemModel
            {
                ItemId = reservation.WishlistItemId,
                ItemPublicId = reservation.WishlistItem.PublicId,
                ItemName = reservation.WishlistItem.Name,
                Description = reservation.WishlistItem.Description,
                Url = reservation.WishlistItem.Url,
                Image = reservation.WishlistItem.Image,
                WhereToBuy = reservation.WishlistItem.WhereToBuy,
                Price = reservation.WishlistItem.Price,
                WishlistName = wishlist.Name,
                WishlistPublicId = wishlist.PublicId,
                WishlistOwnerId = wishlist.OwnerId,
                WishlistOwnerName = ownerName,
                WishlistOwnerEmail = wishlist.Owner?.Email,
                ReservedOn = reservation.ReservationDate,
                IsAnonymous = reservation.IsAnonymous
            };
        }).ToList();
    }

    public async Task<EventUserModel> InviteUserToEventByPublicIdAsync(string eventPublicId, string inviterId, string userId)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var eventEntity = await context.Events
            .FirstOrDefaultAsync(e => e.PublicId == eventPublicId && !e.Deleted)
            ?? throw new KeyNotFoundException($"Event with publicId {eventPublicId} not found");

        return await InviteUserToEventAsync(eventEntity.Id, inviterId, userId);
    }

    public async Task<EventUserModel> InviteByEmailToEventByPublicIdAsync(string eventPublicId, string inviterId, string email)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var eventEntity = await context.Events
            .FirstOrDefaultAsync(e => e.PublicId == eventPublicId && !e.Deleted)
            ?? throw new KeyNotFoundException($"Event with publicId {eventPublicId} not found");

        return await InviteByEmailToEventAsync(eventEntity.Id, inviterId, email);
    }

    public async Task<IEnumerable<EventUserModel>> GetEventInvitationsByPublicIdAsync(string eventPublicId)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var eventEntity = await context.Events
            .FirstOrDefaultAsync(e => e.PublicId == eventPublicId && !e.Deleted)
            ?? throw new KeyNotFoundException($"Event with publicId {eventPublicId} not found");

        return await GetEventInvitationsAsync(eventEntity.Id);
    }

    public async Task<EventUserModel?> ClaimEventInvitationByEmailAsync(string eventPublicId, string userId, string? email)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventPublicId);
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Invitation email is required to claim an event invite.", nameof(email));
        }

        var normalizedEmail = email.Trim();

        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var user = await context.Users.FindAsync(userId)
            ?? throw new KeyNotFoundException($"User with id {userId} not found");

        if (string.IsNullOrWhiteSpace(user.Email) ||
            !string.Equals(user.Email, normalizedEmail, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Sign in with the email address that received this invitation.");
        }

        var eventEntity = await context.Events
            .FirstOrDefaultAsync(e => e.PublicId == eventPublicId && !e.Deleted)
            ?? throw new KeyNotFoundException($"Event with publicId {eventPublicId} not found");

        var eventUser = await context.EventUsers
            .Include(eu => eu.Event)
            .Include(eu => eu.User)
            .FirstOrDefaultAsync(eu =>
                eu.EventId == eventEntity.Id &&
                !eu.Deleted &&
                (eu.UserId == userId ||
                 (eu.InviteeEmail != null && EF.Functions.ILike(eu.InviteeEmail, normalizedEmail))));

        if (eventUser == null)
        {
            return null;
        }

        if (string.IsNullOrEmpty(eventUser.UserId))
        {
            eventUser.UserId = userId;
            eventUser.UpdatedOn = DateTimeOffset.UtcNow;
            await context.SaveChangesAsync();
            eventUser.User = user;
        }
        else if (eventUser.User == null)
        {
            eventUser.User = user;
        }

        if (eventUser.Event == null)
        {
            eventUser.Event = eventEntity;
        }

        return _mapper.Map<EventUserModel>(eventUser);
    }

    private static async Task<bool> AttachEmailInvitationsToUserAsync(ApplicationDbContext context, ApplicationUser user)
    {
        if (string.IsNullOrWhiteSpace(user.Email))
        {
            return false;
        }

        var normalizedEmail = user.Email.Trim();

        var pendingEmailInvites = await context.EventUsers
            .Where(eu =>
                eu.UserId == null &&
                !eu.Deleted &&
                eu.Status == "Pending" &&
                eu.InviteeEmail != null &&
                EF.Functions.ILike(eu.InviteeEmail, normalizedEmail))
            .ToListAsync();

        if (pendingEmailInvites.Count == 0)
        {
            return false;
        }

        foreach (var eventUser in pendingEmailInvites)
        {
            eventUser.UserId = user.Id;
            eventUser.UpdatedOn = DateTimeOffset.UtcNow;
        }

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

    private static NotificationActionModel BuildEventInvitationAction(string eventPublicId, int eventUserId)
    {
        var action = new NotificationActionModel
        {
            Type = "EventInvitation",
            NavigateTo = $"/events/{eventPublicId}",
            Options = new List<NotificationActionOptionModel>
            {
                new() { Key = "accept", Label = "Accept", IsPrimary = true },
                new() { Key = "decline", Label = "Decline" }
            }
        };

        action.Parameters["eventPublicId"] = eventPublicId;
        action.Parameters["eventUserId"] = eventUserId.ToString(CultureInfo.InvariantCulture);
        return action;
    }

    // Gift Exchange methods
    public async Task<EventModel> DrawNamesAsync(int eventId, string ownerId)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var eventEntity = await context.Events
            .Include(e => e.CreatedBy)
            .Include(e => e.EventUsers)
                .ThenInclude(eu => eu.User)
            .Include(e => e.GiftExchanges)
            .Include(e => e.PairingRules)
                .ThenInclude(pr => pr.SourceUser)
            .Include(e => e.PairingRules)
                .ThenInclude(pr => pr.TargetUser)
            .FirstOrDefaultAsync(e => e.Id == eventId && !e.Deleted)
            ?? throw new KeyNotFoundException($"Event with id {eventId} not found");

        ValidateEventCreatorPermission(eventEntity, ownerId);

        if (!eventEntity.IsGiftExchange)
        {
            throw new InvalidOperationException("This event is not configured as a gift exchange.");
        }

        if (eventEntity.NamesDrawnOn.HasValue)
        {
            throw new InvalidOperationException("Names have already been drawn for this event.");
        }

        // Get all participants (owner + all invited users, regardless of acceptance status)
        var participants = new List<string> { eventEntity.CreatedBy.Id };
        participants.AddRange(eventEntity.EventUsers
            .Where(eu => !eu.Deleted && !string.IsNullOrEmpty(eu.UserId))
            .Select(eu => eu.UserId!));

        if (participants.Count < 2)
        {
            throw new InvalidOperationException("Need at least 2 participants to draw names.");
        }

        // Get exclusion rules
        var exclusions = eventEntity.PairingRules
            .Where(pr => pr.RuleType == "Exclusion" && !pr.Deleted)
            .Select(pr => (pr.SourceUserId, pr.TargetUserId))
            .ToList();

        // Perform name drawing with exclusions
        var assignments = DrawNamesWithExclusions(participants, exclusions);

        if (assignments == null)
        {
            throw new InvalidOperationException("Unable to create valid gift exchange assignments with the current pairing rules. Please review the exclusion rules.");
        }

        // Clear existing gift exchanges (shouldn't happen, but just in case)
        var existingExchanges = await context.GiftExchanges
            .Where(ge => ge.EventId == eventId)
            .ToListAsync();
        context.GiftExchanges.RemoveRange(existingExchanges);

        // Create gift exchange records
        foreach (var (giverId, receiverId) in assignments)
        {
            var giftExchange = new GiftExchange
            {
                EventId = eventId,
                GiverId = giverId,
                ReceiverId = receiverId,
                IsAnonymous = false,
                ReceiverPreferences = string.Empty,
                Budget = eventEntity.Budget,
                CreatedOn = DateTimeOffset.UtcNow,
                UpdatedOn = DateTimeOffset.UtcNow
            };
            context.GiftExchanges.Add(giftExchange);
        }

        eventEntity.NamesDrawnOn = DateTimeOffset.UtcNow;
        eventEntity.UpdatedOn = DateTimeOffset.UtcNow;

        await context.SaveChangesAsync();

        // Send notifications to all participants
        var baseUri = _baseUri?.TrimEnd('/') ?? "";
        foreach (var (giverId, receiverId) in assignments)
        {
            var giver = await context.Users.FindAsync(giverId);
            var receiver = await context.Users.FindAsync(receiverId);

            if (giver?.Email != null && receiver != null)
            {
                var receiverName = receiver.UserName ?? receiver.Email ?? "someone";
                var eventLink = $"{baseUri}/events/{eventEntity.PublicId}";

                await _notificationService.CreateNotificationAsync(
                    ownerId,
                    giverId,
                    "Gift Exchange Names Drawn!",
                    $"Your gift exchange recipient for {eventEntity.Name} is {receiverName}!",
                    "GiftExchangeDrawn");

                await _emailSender.SendGiftExchangeDrawnEmailAsync(
                    giver.Email,
                    eventEntity.Name,
                    receiverName,
                    eventLink);
            }
        }

        return await GetEventAsync(eventId);
    }

    public async Task<EventModel> DrawNamesByPublicIdAsync(string eventPublicId, string ownerId)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var eventEntity = await context.Events
            .FirstOrDefaultAsync(e => e.PublicId == eventPublicId && !e.Deleted)
            ?? throw new KeyNotFoundException($"Event with publicId {eventPublicId} not found");

        return await DrawNamesAsync(eventEntity.Id, ownerId);
    }

    public async Task<EventModel> ResetGiftExchangeAsync(int eventId, string ownerId)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var eventEntity = await context.Events
            .Include(e => e.CreatedBy)
            .Include(e => e.EventUsers)
                .ThenInclude(eu => eu.User)
            .Include(e => e.GiftExchanges)
            .FirstOrDefaultAsync(e => e.Id == eventId && !e.Deleted)
            ?? throw new KeyNotFoundException($"Event with id {eventId} not found");

        ValidateEventCreatorPermission(eventEntity, ownerId);

        if (!eventEntity.IsGiftExchange)
        {
            throw new InvalidOperationException("This event is not configured as a gift exchange.");
        }

        if (!eventEntity.NamesDrawnOn.HasValue)
        {
            throw new InvalidOperationException("No gift exchange to reset - names have not been drawn yet.");
        }

        // Delete all gift exchange records
        var giftExchanges = await context.GiftExchanges
            .Where(ge => ge.EventId == eventId)
            .ToListAsync();
        context.GiftExchanges.RemoveRange(giftExchanges);

        // Reset the NamesDrawnOn timestamp
        eventEntity.NamesDrawnOn = null;
        eventEntity.UpdatedOn = DateTimeOffset.UtcNow;

        await context.SaveChangesAsync();

        // Send notifications to all participants
        var baseUri = _baseUri?.TrimEnd('/') ?? "";
        var eventLink = $"{baseUri}/events/{eventEntity.PublicId}";

        // Get all participants (owner + all invited users, regardless of status)
        var participants = new List<(string userId, string? email)> { (eventEntity.CreatedBy.Id, eventEntity.CreatedBy.Email) };

        foreach (var eu in eventEntity.EventUsers.Where(eu => !eu.Deleted))
        {
            if (!string.IsNullOrEmpty(eu.UserId) && eu.User?.Email != null)
            {
                participants.Add((eu.UserId, eu.User.Email));
            }
            else if (!string.IsNullOrEmpty(eu.InviteeEmail))
            {
                // Email-only invites (not yet registered)
                participants.Add((string.Empty, eu.InviteeEmail));
            }
        }

        foreach (var (userId, email) in participants)
        {
            if (!string.IsNullOrEmpty(email))
            {
                await _emailSender.SendGiftExchangeResetEmailAsync(
                    email,
                    eventEntity.Name,
                    eventLink);

                if (!string.IsNullOrEmpty(userId))
                {
                    await _notificationService.CreateNotificationAsync(
                        ownerId,
                        userId,
                        "Gift Exchange Reset",
                        $"The gift exchange for {eventEntity.Name} has been reset.",
                        "GiftExchangeReset");
                }
            }
        }

        return await GetEventAsync(eventId);
    }

    public async Task<EventModel> ResetGiftExchangeByPublicIdAsync(string eventPublicId, string ownerId)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var eventEntity = await context.Events
            .FirstOrDefaultAsync(e => e.PublicId == eventPublicId && !e.Deleted)
            ?? throw new KeyNotFoundException($"Event with publicId {eventPublicId} not found");

        return await ResetGiftExchangeAsync(eventEntity.Id, ownerId);
    }

    private static List<(string giverId, string receiverId)>? DrawNamesWithExclusions(
        List<string> participants,
        List<(string sourceId, string targetId)> exclusions)
    {
        const int maxAttempts = 1000;
        var random = new Random();

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            var receivers = new List<string>(participants);
            var assignments = new List<(string giverId, string receiverId)>();
            var isValid = true;

            // Shuffle receivers
            for (int i = receivers.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                (receivers[j], receivers[i]) = (receivers[i], receivers[j]);
            }

            // Try to assign each giver to a receiver
            for (int i = 0; i < participants.Count; i++)
            {
                var giver = participants[i];
                var receiver = receivers[i];

                // Check if giver is assigned to themselves
                if (giver == receiver)
                {
                    isValid = false;
                    break;
                }

                // Check if assignment violates exclusion rules
                if (exclusions.Any(e => e.sourceId == giver && e.targetId == receiver))
                {
                    isValid = false;
                    break;
                }

                assignments.Add((giver, receiver));
            }

            if (isValid)
            {
                return assignments;
            }
        }

        return null; // Failed to find valid assignment
    }

    public async Task<GiftExchangeModel?> GetMyGiftExchangeAsync(int eventId, string userId)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var eventEntity = await context.Events
            .Include(e => e.CreatedBy)
            .Include(e => e.EventUsers)
            .FirstOrDefaultAsync(e => e.Id == eventId && !e.Deleted)
            ?? throw new KeyNotFoundException($"Event with id {eventId} not found");

        if (!IsEventMember(eventEntity, userId))
        {
            throw new UnauthorizedAccessException("You must be part of this event.");
        }

        var giftExchange = await context.GiftExchanges
            .AsNoTracking()
            .Include(ge => ge.Receiver)
            .Include(ge => ge.Giver)
            .FirstOrDefaultAsync(ge => ge.EventId == eventId && ge.GiverId == userId && !ge.Deleted);

        return giftExchange == null ? null : _mapper.Map<GiftExchangeModel>(giftExchange);
    }

    public async Task<GiftExchangeModel?> GetMyGiftExchangeByPublicIdAsync(string eventPublicId, string userId)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var eventEntity = await context.Events
            .FirstOrDefaultAsync(e => e.PublicId == eventPublicId && !e.Deleted)
            ?? throw new KeyNotFoundException($"Event with publicId {eventPublicId} not found");

        return await GetMyGiftExchangeAsync(eventEntity.Id, userId);
    }

    public async Task<IEnumerable<CustomPairingRuleModel>> GetPairingRulesAsync(int eventId)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var rules = await context.CustomPairingRules
            .AsNoTracking()
            .Include(pr => pr.SourceUser)
            .Include(pr => pr.TargetUser)
            .Where(pr => pr.EventId == eventId && !pr.Deleted)
            .ToListAsync();

        return _mapper.Map<IEnumerable<CustomPairingRuleModel>>(rules);
    }

    public async Task<IEnumerable<CustomPairingRuleModel>> GetPairingRulesByPublicIdAsync(string eventPublicId)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var eventEntity = await context.Events
            .FirstOrDefaultAsync(e => e.PublicId == eventPublicId && !e.Deleted)
            ?? throw new KeyNotFoundException($"Event with publicId {eventPublicId} not found");

        return await GetPairingRulesAsync(eventEntity.Id);
    }

    public async Task<CustomPairingRuleModel> AddPairingRuleAsync(int eventId, CustomPairingRuleModel rule, string ownerId)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var eventEntity = await context.Events
            .Include(e => e.CreatedBy)
            .FirstOrDefaultAsync(e => e.Id == eventId && !e.Deleted)
            ?? throw new KeyNotFoundException($"Event with id {eventId} not found");

        ValidateEventCreatorPermission(eventEntity, ownerId);

        if (eventEntity.NamesDrawnOn.HasValue)
        {
            throw new InvalidOperationException("Cannot add pairing rules after names have been drawn.");
        }

        var ruleEntity = _mapper.Map<CustomPairingRule>(rule);
        ruleEntity.EventId = eventId;
        ruleEntity.CreatedOn = DateTimeOffset.UtcNow;
        ruleEntity.UpdatedOn = DateTimeOffset.UtcNow;

        context.CustomPairingRules.Add(ruleEntity);
        await context.SaveChangesAsync();

        return _mapper.Map<CustomPairingRuleModel>(ruleEntity);
    }

    public async Task<CustomPairingRuleModel> AddPairingRuleByPublicIdAsync(string eventPublicId, CustomPairingRuleModel rule, string ownerId)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var eventEntity = await context.Events
            .FirstOrDefaultAsync(e => e.PublicId == eventPublicId && !e.Deleted)
            ?? throw new KeyNotFoundException($"Event with publicId {eventPublicId} not found");

        return await AddPairingRuleAsync(eventEntity.Id, rule, ownerId);
    }

    public async Task<bool> RemovePairingRuleAsync(int ruleId, string ownerId)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var rule = await context.CustomPairingRules
            .Include(pr => pr.Event)
                .ThenInclude(e => e.CreatedBy)
            .FirstOrDefaultAsync(pr => pr.Id == ruleId && !pr.Deleted);

        if (rule == null)
        {
            return false;
        }

        ValidateEventCreatorPermission(rule.Event, ownerId);

        if (rule.Event.NamesDrawnOn.HasValue)
        {
            throw new InvalidOperationException("Cannot remove pairing rules after names have been drawn.");
        }

        rule.Deleted = true;
        rule.UpdatedOn = DateTimeOffset.UtcNow;
        await context.SaveChangesAsync();

        return true;
    }
}