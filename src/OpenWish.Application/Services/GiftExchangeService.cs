using System;
using System.Collections.Generic;
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

public class GiftExchangeService(
    IServiceScopeFactory scopeFactory,
    IMapper mapper,
    INotificationService notificationService,
    IAppEmailSender emailSender,
    IOptions<OpenWishSettings> openWishSettings,
    ILogger<GiftExchangeService> logger) : IGiftExchangeService
{
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
    private readonly IMapper _mapper = mapper;
    private readonly INotificationService _notificationService = notificationService;
    private readonly IAppEmailSender _emailSender = emailSender;
    private readonly string? _baseUri = openWishSettings.Value.BaseUri;
    private readonly ILogger<GiftExchangeService> _logger = logger;

    private static bool IsEventMember(Event eventEntity, string userId)
    {
        if (IsEventOwner(eventEntity, userId))
        {
            return true;
        }

        return eventEntity.EventUsers.Any(eu =>
            !eu.Deleted &&
            eu.Status == "Accepted" &&
            string.Equals(eu.UserId, userId, StringComparison.Ordinal));
    }

    private static bool IsEventOwner(Event eventEntity, string userId) =>
        string.Equals(eventEntity.CreatedBy?.Id, userId, StringComparison.Ordinal);

    private static void ValidateEventCreatorPermission(Event eventEntity, string userId)
    {
        if (!IsEventOwner(eventEntity, userId))
        {
            throw new UnauthorizedAccessException("Only the event creator can perform this action.");
        }
    }

    public async Task<EventModel> DrawNamesAsync(int eventId, string ownerId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerId, nameof(ownerId));

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

        var participants = BuildGiftExchangeParticipants(eventEntity);

        if (participants.Count < 2)
        {
            throw new InvalidOperationException("Need at least 2 participants to draw names.");
        }

        var exclusions = eventEntity.PairingRules
            .Where(pr => pr.RuleType == "Exclusion" && !pr.Deleted)
            .Select(pr => new PairingExclusion(
                pr.SourceUserId,
                NormalizeEmail(pr.SourceInviteeEmail),
                pr.TargetUserId,
                NormalizeEmail(pr.TargetInviteeEmail)))
            .ToList();

        var assignments = DrawNamesWithExclusions(participants, exclusions);

        if (assignments == null)
        {
            throw new InvalidOperationException("Unable to create valid gift exchange assignments with the current pairing rules. Please review the exclusion rules.");
        }

        var existingExchanges = await context.GiftExchanges
            .Where(ge => ge.EventId == eventId)
            .ToListAsync();
        context.GiftExchanges.RemoveRange(existingExchanges);

        foreach (var (giver, receiver) in assignments)
        {
            var giftExchange = new GiftExchange
            {
                EventId = eventId,
                GiverId = giver.UserId,
                GiverEmail = giver.Email,
                ReceiverId = receiver.UserId,
                ReceiverEmail = receiver.Email,
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

        var baseUri = _baseUri?.TrimEnd('/') ?? "";
        var eventLink = $"{baseUri}/events/{eventEntity.PublicId}";
        foreach (var (giver, receiver) in assignments)
        {
            var receiverName = receiver.DisplayName;

            if (!string.IsNullOrWhiteSpace(giver.UserId))
            {
                await _notificationService.CreateNotificationAsync(
                    ownerId,
                    giver.UserId!,
                    "Gift Exchange Names Drawn!",
                    $"Your gift exchange recipient for {eventEntity.Name} is {receiverName}!",
                    "GiftExchangeDrawn");
            }

            if (!string.IsNullOrWhiteSpace(giver.Email))
            {
                await _emailSender.SendGiftExchangeDrawnEmailAsync(
                    giver.Email,
                    eventEntity.Name,
                    receiverName,
                    eventLink);
            }
        }

        var eventService = scope.ServiceProvider.GetRequiredService<IEventService>();
        return await eventService.GetEventAsync(eventId);
    }

    public async Task<EventModel> DrawNamesByPublicIdAsync(string eventPublicId, string ownerId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventPublicId, nameof(eventPublicId));
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerId, nameof(ownerId));

        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var eventEntity = await context.Events
            .FirstOrDefaultAsync(e => e.PublicId == eventPublicId && !e.Deleted)
            ?? throw new KeyNotFoundException($"Event with publicId {eventPublicId} not found");

        return await DrawNamesAsync(eventEntity.Id, ownerId);
    }

    public async Task<EventModel> ResetGiftExchangeAsync(int eventId, string ownerId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerId, nameof(ownerId));

        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var eventEntity = await context.Events
            .Include(e => e.CreatedBy)
            .FirstOrDefaultAsync(e => e.Id == eventId && !e.Deleted)
            ?? throw new KeyNotFoundException($"Event with id {eventId} not found");

        ValidateEventCreatorPermission(eventEntity, ownerId);

        var giftExchanges = await context.GiftExchanges
            .Where(ge => ge.EventId == eventId && !ge.Deleted)
            .ToListAsync();

        foreach (var exchange in giftExchanges)
        {
            exchange.Deleted = true;
            exchange.UpdatedOn = DateTimeOffset.UtcNow;
        }

        eventEntity.NamesDrawnOn = null;
        eventEntity.UpdatedOn = DateTimeOffset.UtcNow;

        await context.SaveChangesAsync();

        var eventService = scope.ServiceProvider.GetRequiredService<IEventService>();
        return await eventService.GetEventAsync(eventId);
    }

    public async Task<EventModel> ResetGiftExchangeByPublicIdAsync(string eventPublicId, string ownerId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventPublicId, nameof(eventPublicId));
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerId, nameof(ownerId));

        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var eventEntity = await context.Events
            .FirstOrDefaultAsync(e => e.PublicId == eventPublicId && !e.Deleted)
            ?? throw new KeyNotFoundException($"Event with publicId {eventPublicId} not found");

        return await ResetGiftExchangeAsync(eventEntity.Id, ownerId);
    }

    public async Task<GiftExchangeModel?> GetMyGiftExchangeAsync(int eventId, string userId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId, nameof(userId));

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
        ArgumentException.ThrowIfNullOrWhiteSpace(eventPublicId, nameof(eventPublicId));
        ArgumentException.ThrowIfNullOrWhiteSpace(userId, nameof(userId));

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

    public async Task<IEnumerable<CustomPairingRuleModel>> GetPairingRulesByPublicIdAsync(string eventPublicId, string requestorId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventPublicId, nameof(eventPublicId));
        ArgumentException.ThrowIfNullOrWhiteSpace(requestorId, nameof(requestorId));

        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var eventEntity = await context.Events
            .Include(e => e.CreatedBy)
            .FirstOrDefaultAsync(e => e.PublicId == eventPublicId && !e.Deleted)
            ?? throw new KeyNotFoundException($"Event with publicId {eventPublicId} not found");

        ValidateEventCreatorPermission(eventEntity, requestorId);
        return await GetPairingRulesAsync(eventEntity.Id);
    }

    public async Task<CustomPairingRuleModel> AddPairingRuleAsync(int eventId, CustomPairingRuleModel rule, string ownerId)
    {
        ArgumentNullException.ThrowIfNull(rule);
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerId, nameof(ownerId));

        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var eventEntity = await context.Events
            .Include(e => e.CreatedBy)
            .Include(e => e.EventUsers)
                .ThenInclude(eu => eu.User)
            .FirstOrDefaultAsync(e => e.Id == eventId && !e.Deleted)
            ?? throw new KeyNotFoundException($"Event with id {eventId} not found");

        ValidateEventCreatorPermission(eventEntity, ownerId);

        if (eventEntity.NamesDrawnOn.HasValue)
        {
            throw new InvalidOperationException("Cannot modify pairing rules after names have been drawn.");
        }

        var participants = BuildGiftExchangeParticipants(eventEntity);

        rule.SourceInviteeEmail = NormalizeEmail(rule.SourceInviteeEmail);
        rule.TargetInviteeEmail = NormalizeEmail(rule.TargetInviteeEmail);

        if (!HasParticipantIdentifier(rule.SourceUserId, rule.SourceInviteeEmail))
        {
            throw new InvalidOperationException("Pairing rule must specify at least the source user ID or email.");
        }

        if (!HasParticipantIdentifier(rule.TargetUserId, rule.TargetInviteeEmail))
        {
            throw new InvalidOperationException("Pairing rule must specify at least the target user ID or email.");
        }

        if (AreSameParticipant(rule.SourceUserId, rule.SourceInviteeEmail, rule.TargetUserId, rule.TargetInviteeEmail))
        {
            throw new InvalidOperationException("A participant cannot have an exclusion rule with themselves.");
        }

        if (!ParticipantExists(participants, rule.SourceUserId, rule.SourceInviteeEmail))
        {
            throw new KeyNotFoundException("The source participant is not part of this event.");
        }

        if (!ParticipantExists(participants, rule.TargetUserId, rule.TargetInviteeEmail))
        {
            throw new KeyNotFoundException("The target participant is not part of this event.");
        }

        var existingRule = await context.CustomPairingRules
            .FirstOrDefaultAsync(r =>
                r.EventId == eventId &&
                !r.Deleted &&
                r.RuleType == rule.RuleType &&
                r.SourceUserId == rule.SourceUserId &&
                r.SourceInviteeEmail == rule.SourceInviteeEmail &&
                r.TargetUserId == rule.TargetUserId &&
                r.TargetInviteeEmail == rule.TargetInviteeEmail);

        if (existingRule != null)
        {
            throw new InvalidOperationException("This pairing rule already exists for this event.");
        }

        var newRule = new CustomPairingRule
        {
            EventId = eventId,
            RuleType = rule.RuleType ?? "Exclusion",
            SourceUserId = rule.SourceUserId,
            SourceInviteeEmail = rule.SourceInviteeEmail,
            TargetUserId = rule.TargetUserId,
            TargetInviteeEmail = rule.TargetInviteeEmail,
            CreatedOn = DateTimeOffset.UtcNow,
            UpdatedOn = DateTimeOffset.UtcNow
        };

        context.CustomPairingRules.Add(newRule);
        await context.SaveChangesAsync();

        var createdRule = await context.CustomPairingRules
            .Include(pr => pr.SourceUser)
            .Include(pr => pr.TargetUser)
            .FirstOrDefaultAsync(r => r.Id == newRule.Id)
            ?? throw new InvalidOperationException("Failed to retrieve the created pairing rule.");

        return _mapper.Map<CustomPairingRuleModel>(createdRule);
    }

    public async Task<CustomPairingRuleModel> AddPairingRuleByPublicIdAsync(string eventPublicId, CustomPairingRuleModel rule, string ownerId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventPublicId, nameof(eventPublicId));
        ArgumentNullException.ThrowIfNull(rule);
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerId, nameof(ownerId));

        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var eventEntity = await context.Events
            .FirstOrDefaultAsync(e => e.PublicId == eventPublicId && !e.Deleted)
            ?? throw new KeyNotFoundException($"Event with publicId {eventPublicId} not found");

        return await AddPairingRuleAsync(eventEntity.Id, rule, ownerId);
    }

    public async Task<bool> RemovePairingRuleAsync(int ruleId, string ownerId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerId, nameof(ownerId));

        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var rule = await context.CustomPairingRules
            .Include(r => r.Event)
                .ThenInclude(e => e.CreatedBy)
            .FirstOrDefaultAsync(r => r.Id == ruleId && !r.Deleted);

        if (rule == null)
        {
            return false;
        }

        ValidateEventCreatorPermission(rule.Event, ownerId);

        if (rule.Event.NamesDrawnOn.HasValue)
        {
            throw new InvalidOperationException("Cannot modify pairing rules after names have been drawn.");
        }

        rule.Deleted = true;
        rule.UpdatedOn = DateTimeOffset.UtcNow;

        await context.SaveChangesAsync();
        return true;
    }

    private static List<GiftExchangeParticipant> BuildGiftExchangeParticipants(Event eventEntity)
    {
        ArgumentNullException.ThrowIfNull(eventEntity);

        if (eventEntity.CreatedBy == null)
        {
            throw new InvalidOperationException("Event is missing the organizer details needed for drawing names.");
        }

        var participants = new List<GiftExchangeParticipant>();
        var seenKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        void addParticipant(string key, string? userId, string? email, string? displayName)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                var name = displayName ?? userId ?? "Participant";
                throw new InvalidOperationException($"Cannot include {name} in the gift exchange because no email address is on file.");
            }

            if (seenKeys.Add(key))
            {
                var trimmedEmail = email.Trim();
                var normalizedEmail = NormalizeEmail(trimmedEmail)!;
                var resolvedName = string.IsNullOrWhiteSpace(displayName) ? trimmedEmail : displayName;
                participants.Add(new GiftExchangeParticipant(key, userId, trimmedEmail, normalizedEmail, resolvedName));
            }
        }

        addParticipant($"user:{eventEntity.CreatedBy.Id}",
            eventEntity.CreatedBy.Id,
            eventEntity.CreatedBy.Email,
            eventEntity.CreatedBy.UserName ?? eventEntity.CreatedBy.Email ?? eventEntity.CreatedBy.Id);

        foreach (var eventUser in eventEntity.EventUsers.Where(IsEligibleGiftExchangeParticipant))
        {
            if (!string.IsNullOrWhiteSpace(eventUser.UserId) && eventUser.User != null)
            {
                addParticipant($"user:{eventUser.UserId}",
                    eventUser.UserId,
                    eventUser.User.Email,
                    eventUser.User.UserName ?? eventUser.User.Email ?? eventUser.UserId);
            }
            else if (!string.IsNullOrWhiteSpace(eventUser.InviteeEmail))
            {
                var normalizedEmail = eventUser.InviteeEmail.Trim();
                addParticipant($"invite:{eventUser.Id}",
                    null,
                    normalizedEmail,
                    normalizedEmail);
            }
        }

        return participants;
    }

    private static bool IsEligibleGiftExchangeParticipant(EventUser participant) =>
        !participant.Deleted &&
        participant.IsAccepted &&
        string.Equals(participant.Status, "Accepted", StringComparison.OrdinalIgnoreCase);

    private static List<(GiftExchangeParticipant giver, GiftExchangeParticipant receiver)>? DrawNamesWithExclusions(
        IReadOnlyList<GiftExchangeParticipant> participants,
        List<PairingExclusion> exclusions)
    {
        const int maxAttempts = 1000;
        var random = new Random();

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            var receivers = participants.ToList();
            var assignments = new List<(GiftExchangeParticipant giver, GiftExchangeParticipant receiver)>();
            var isValid = true;

            for (int i = receivers.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                (receivers[j], receivers[i]) = (receivers[i], receivers[j]);
            }

            for (int i = 0; i < participants.Count; i++)
            {
                var giver = participants[i];
                var receiver = receivers[i];

                if (ReferenceEquals(giver, receiver) || string.Equals(giver.Key, receiver.Key, StringComparison.Ordinal))
                {
                    isValid = false;
                    break;
                }

                if (exclusions.Any(e =>
                        MatchesParticipant(e.SourceUserId, e.SourceEmail, giver) &&
                        MatchesParticipant(e.TargetUserId, e.TargetEmail, receiver)))
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

        return null;
    }

    private static bool MatchesParticipant(string? userId, string? email, GiftExchangeParticipant participant)
    {
        if (!string.IsNullOrEmpty(userId) && string.Equals(participant.UserId, userId, StringComparison.Ordinal))
        {
            return true;
        }

        if (!string.IsNullOrWhiteSpace(email))
        {
            return string.Equals(participant.NormalizedEmail, NormalizeEmail(email), StringComparison.Ordinal);
        }

        return false;
    }

    private static bool ParticipantExists(IEnumerable<GiftExchangeParticipant> participants, string? userId, string? normalizedEmail) =>
        participants.Any(p =>
            (!string.IsNullOrEmpty(userId) && string.Equals(p.UserId, userId, StringComparison.Ordinal)) ||
            (!string.IsNullOrEmpty(normalizedEmail) && string.Equals(p.NormalizedEmail, normalizedEmail, StringComparison.Ordinal)));

    private static bool HasParticipantIdentifier(string? userId, string? normalizedEmail) =>
        !string.IsNullOrEmpty(userId) || !string.IsNullOrEmpty(normalizedEmail);

    private static bool AreSameParticipant(string? firstUserId, string? firstEmail, string? secondUserId, string? secondEmail)
    {
        if (!string.IsNullOrEmpty(firstUserId) && string.Equals(firstUserId, secondUserId, StringComparison.Ordinal))
        {
            return true;
        }

        var normalizedFirstEmail = NormalizeEmail(firstEmail);
        var normalizedSecondEmail = NormalizeEmail(secondEmail);

        if (!string.IsNullOrEmpty(normalizedFirstEmail) &&
            !string.IsNullOrEmpty(normalizedSecondEmail) &&
            string.Equals(normalizedFirstEmail, normalizedSecondEmail, StringComparison.Ordinal))
        {
            return true;
        }

        return false;
    }

    private static string? NormalizeEmail(string? email) =>
        string.IsNullOrWhiteSpace(email) ? null : email.Trim().ToLowerInvariant();

    private record PairingExclusion(string? SourceUserId, string? SourceEmail, string? TargetUserId, string? TargetEmail);

    private record GiftExchangeParticipant(string Key, string? UserId, string Email, string NormalizedEmail, string DisplayName);
}