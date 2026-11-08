using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using OpenWish.Application.Models.Configuration;
using OpenWish.Data;
using OpenWish.Data.Entities;
using OpenWish.Shared.Models;

namespace OpenWish.Application.Services;

// Guest credentials never become application-user identities or ordinary event permissions.
public sealed class GuestEventService(
    IDbContextFactory<ApplicationDbContext> contextFactory,
    IAppEmailSender emailSender,
    IOptions<OpenWishSettings> settings,
    IHostEnvironment environment)
{
    public static string HashToken(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    public async Task SendAccessAsync(string participantPublicId, string ownerId)
    {
        var baseUri = settings.Value.BaseUri?.TrimEnd('/');
        if (!Uri.TryCreate(baseUri, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttps && !(environment.IsDevelopment() && uri.Scheme == Uri.UriSchemeHttp)))
        {
            throw new InvalidOperationException("Configure an HTTPS public URL before sending guest access.");
        }
        await using var context = await contextFactory.CreateDbContextAsync();
        var participant = await context.EventUsers.Include(p => p.Event).ThenInclude(e => e!.CreatedBy)
            .SingleOrDefaultAsync(p => p.PublicId == participantPublicId && !p.Deleted)
            ?? throw new KeyNotFoundException("This invitation is no longer available.");
        if (participant.Event!.CreatedBy.Id != ownerId)
        {
            throw new UnauthorizedAccessException("Only the organizer can send guest access.");
        }
        await using var transaction = await EventService.LockEventRowAsync(context, participant.EventId);
        await context.Entry(participant.Event).ReloadAsync();
        await context.Entry(participant).ReloadAsync();
        if (!IsAvailable(participant) || string.IsNullOrWhiteSpace(participant.InviteeEmail))
        {
            throw new InvalidOperationException("Guest access is unavailable. Check the event setting and invitation.");
        }

        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        participant.GuestTokenHash = HashToken(token);
        participant.GuestAccessExpiresOn = DateTimeOffset.UtcNow.AddDays(30);
        participant.GuestEmailSentOn = null;
        participant.UpdatedOn = DateTimeOffset.UtcNow;
        await context.SaveChangesAsync();
        if (transaction is not null)
        {
            await transaction.CommitAsync();
        }

        await emailSender.SendGuestEventAccessEmailAsync(participant.InviteeEmail,
            participant.Event.Name, $"{baseUri}/guest#access={token}",
            matchReady: participant.IsAccepted && participant.Event.NamesDrawnOn.HasValue);

        // A concurrent revoke/resend must not be overwritten by a completed mail delivery.
        await using var deliveryContext = await contextFactory.CreateDbContextAsync();
        await using var deliveryTransaction = await EventService.LockEventRowAsync(deliveryContext, participant.EventId);
        var deliveredParticipant = await deliveryContext.EventUsers.SingleAsync(p => p.Id == participant.Id);
        if (deliveredParticipant.GuestTokenHash == HashToken(token))
        {
            deliveredParticipant.GuestEmailSentOn = DateTimeOffset.UtcNow;
            await deliveryContext.SaveChangesAsync();
        }
        if (deliveryTransaction is not null)
        {
            await deliveryTransaction.CommitAsync();
        }
    }

    public async Task RevokeAccessAsync(string eventPublicId, string participantPublicId, string ownerId, bool removeParticipant = false)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var participant = await context.EventUsers.Include(p => p.Event).ThenInclude(e => e!.CreatedBy)
            .SingleOrDefaultAsync(p => p.PublicId == participantPublicId && p.Event!.PublicId == eventPublicId && !p.Deleted)
            ?? throw new KeyNotFoundException("This invitation is no longer available.");
        if (participant.Event!.CreatedBy.Id != ownerId)
        {
            throw new UnauthorizedAccessException("Only the organizer can revoke guest access.");
        }
        await using var transaction = await EventService.LockEventRowAsync(context, participant.EventId);
        await context.Entry(participant).ReloadAsync();
        await context.Entry(participant.Event!).ReloadAsync();
        if (participant.UserId is not null)
        {
            throw new InvalidOperationException("This participant now uses an account.");
        }
        if (removeParticipant)
        {
            if (participant.Event.NamesDrawnOn.HasValue && participant.IsAccepted)
            {
                throw new InvalidOperationException("Reset the gift exchange before removing an accepted participant.");
            }
            participant.Deleted = true;
        }
        participant.GuestTokenHash = null;
        participant.GuestAccessExpiresOn = null;
        participant.GuestEmailSentOn = null;
        participant.UpdatedOn = DateTimeOffset.UtcNow;
        await context.SaveChangesAsync();
        if (transaction is not null)
        {
            await transaction.CommitAsync();
        }
    }

    public async Task<GuestEventSession?> RedeemAsync(string token)
    {
        if (token.Length != 64 || token.Any(c => !Uri.IsHexDigit(c)))
        {
            return null;
        }
        var hash = HashToken(token);
        await using var context = await contextFactory.CreateDbContextAsync();
        var participant = await context.EventUsers.Include(p => p.Event)
            .SingleOrDefaultAsync(p => p.GuestTokenHash == hash);
        return participant is not null && IsAvailable(participant) &&
            participant.GuestAccessExpiresOn > DateTimeOffset.UtcNow
            ? new GuestEventSession(participant.PublicId, hash)
            : null;
    }

    public async Task<GuestEventView?> GetViewAsync(GuestEventSession session)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var participant = await FindParticipantAsync(context, session);
        if (participant is null)
        {
            return null;
        }
        await using var transaction = await EventService.LockEventRowAsync(context, participant.EventId);
        await context.Entry(participant).ReloadAsync();
        await context.Entry(participant.Event!).ReloadAsync();
        if (!MatchesSession(participant, session))
        {
            return null;
        }
        var evt = participant.Event!;
        var assignment = participant.IsAccepted
            ? await context.GiftExchanges.AsNoTracking().Include(g => g.Receiver).Include(g => g.ReceiverParticipant)
                .SingleOrDefaultAsync(g => g.EventId == evt.Id && g.GiverParticipantId == participant.Id && !g.Deleted)
            : null;
        var needsOpening = assignment is not null && evt.UseGiftBoxReveal && assignment.MatchOpenedOn is null;
        var view = new GuestEventView(
            evt.Name, evt.Date, evt.Budget, evt.Description, participant.GuestDisplayName,
            participant.IsAccepted, evt.NamesDrawnOn is null, assignment is not null, needsOpening,
            assignment?.PublicId,
            assignment is null || needsOpening ? null :
                PublicDisplayName.From(assignment.Receiver?.UserName ?? assignment.ReceiverParticipant?.GuestDisplayName, "Invited guest"));
        if (transaction is not null)
        {
            await transaction.CommitAsync();
        }
        return view;
    }

    public async Task<string> GetAccountRegistrationUrlAsync(GuestEventSession session)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var participant = await FindParticipantAsync(context, session)
            ?? throw new UnauthorizedAccessException("Guest access has expired or been revoked.");
        var email = Uri.EscapeDataString(participant.InviteeEmail!);
        var returnUrl = Uri.EscapeDataString($"/events/{participant.Event!.PublicId}/accept-invite?email={email}");
        return $"/Account/Register?email={email}&ReturnUrl={returnUrl}";
    }

    public async Task RespondAsync(GuestEventSession session, string? displayName, bool accept)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var participant = await FindParticipantAsync(context, session)
            ?? throw new UnauthorizedAccessException("Guest access has expired or been revoked.");
        await using var transaction = await EventService.LockEventRowAsync(context, participant.EventId);
        await context.Entry(participant).ReloadAsync();
        await context.Entry(participant.Event!).ReloadAsync();
        if (!MatchesSession(participant, session))
        {
            throw new UnauthorizedAccessException("Guest access has expired or been revoked.");
        }
        if (participant.Event!.NamesDrawnOn is not null || participant.Status != "Pending")
        {
            throw new InvalidOperationException("This invitation has already been answered or the draw has started.");
        }
        var name = displayName?.Trim();
        if (accept && (string.IsNullOrWhiteSpace(name) || name.Length > 80 || name.Any(char.IsControl)))
        {
            throw new ArgumentException("Enter a display name of 1 to 80 characters.");
        }
        participant.GuestDisplayName = accept ? name : null;
        participant.Status = accept ? "Accepted" : "Rejected";
        participant.IsAccepted = accept;
        participant.UpdatedOn = DateTimeOffset.UtcNow;
        if (!accept)
        {
            participant.GuestTokenHash = null;
            participant.GuestAccessExpiresOn = null;
        }
        await context.SaveChangesAsync();
        if (transaction is not null)
        {
            await transaction.CommitAsync();
        }
    }

    public async Task OpenMatchAsync(GuestEventSession session, string assignmentPublicId)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var participant = await FindParticipantAsync(context, session)
            ?? throw new UnauthorizedAccessException("Guest access has expired or been revoked.");
        await using var transaction = await EventService.LockEventRowAsync(context, participant.EventId);
        await context.Entry(participant).ReloadAsync();
        await context.Entry(participant.Event!).ReloadAsync();
        if (!MatchesSession(participant, session) || !participant.IsAccepted)
        {
            throw new UnauthorizedAccessException("Guest access has expired or been revoked.");
        }
        var assignment = await context.GiftExchanges.SingleOrDefaultAsync(g =>
            g.EventId == participant.EventId && g.GiverParticipantId == participant.Id &&
            g.PublicId == assignmentPublicId && !g.Deleted)
            ?? throw new InvalidOperationException("Your assignment changed. Open your latest invitation email.");
        if (assignment.MatchOpenedOn is null)
        {
            assignment.MatchOpenedOn = DateTimeOffset.UtcNow;
            assignment.UpdatedOn = DateTimeOffset.UtcNow;
            await context.SaveChangesAsync();
        }
        if (transaction is not null)
        {
            await transaction.CommitAsync();
        }
    }

    private static bool IsAvailable(EventUser participant) =>
        !participant.Deleted && participant.UserId is null &&
        participant.Status is "Pending" or "Accepted" &&
        participant.Event is { Deleted: false, IsGiftExchange: true, AllowGuestParticipants: true };

    private static bool MatchesSession(EventUser participant, GuestEventSession session) =>
        IsAvailable(participant) && participant.GuestTokenHash == session.TokenHash &&
        participant.GuestAccessExpiresOn > DateTimeOffset.UtcNow;

    private static async Task<EventUser?> FindParticipantAsync(ApplicationDbContext context, GuestEventSession session)
    {
        var participant = await context.EventUsers.Include(p => p.Event)
            .SingleOrDefaultAsync(p => p.PublicId == session.ParticipantPublicId);
        return participant is not null && MatchesSession(participant, session) ? participant : null;
    }
}

public sealed record GuestEventSession(string ParticipantPublicId, string TokenHash);
public sealed record GuestEventView(
    string EventName, DateTimeOffset Date, decimal? Budget, string? Guidance, string? DisplayName,
    bool Accepted, bool CanRespond, bool HasMatch, bool NeedsOpening, string? AssignmentPublicId, string? RecipientName);