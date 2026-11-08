using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OpenWish.Application.Services;
using OpenWish.Data;
using OpenWish.Data.Entities;
using OpenWish.Shared.Models;
using OpenWish.Shared.RequestModels;
using Xunit;

namespace OpenWish.Web.Tests.Integrations;

[Collection(GuestEventIntegrationCollection.Name)]
public sealed partial class GuestEventAccessTests(OpenWishAppFactory factory)
{
    [Fact]
    public async Task FailedGuestMail_IsVisibleAndRecoverableByResending()
    {
        using var owner = factory.CreateBrowser();
        await owner.SignInAsync("owner");
        var evt = await CreateEventAsync(owner);
        var email = $"mail-failure-{Guid.NewGuid():N}@example.test";
        factory.Emails.FailGuestDelivery = true;
        try
        {
            var failed = await owner.Client.PostAsJsonAsync($"api/events/{evt.PublicId}/invitations/email", new { Email = email });
            Assert.Equal(HttpStatusCode.BadGateway, failed.StatusCode);
            Assert.Contains("could not be delivered", await failed.Content.ReadAsStringAsync(), StringComparison.Ordinal);
        }
        finally
        {
            factory.Emails.FailGuestDelivery = false;
        }
        var invitations = await owner.Client.GetFromJsonAsync<List<EventUserModel>>($"api/events/{evt.PublicId}/invitations");
        var invitation = Assert.Single(invitations!);
        Assert.True(invitation.HasGuestAccess);
        Assert.Null(invitation.GuestEmailSentOn);
        (await owner.Client.PostAsync($"api/events/{evt.PublicId}/invitations/{invitation.PublicId}/guest-access/send", null))
            .EnsureSuccessStatusCode();
        using var guest = factory.CreateBrowser();
        await ConnectAsync(guest, TokenFor(email));
        Assert.Contains("Your name for the group", await guest.GetPageAsync("guest"));
    }

    [Fact]
    public async Task Guests_AcceptAndOpenOnlyTheirOwnMatchWithoutAnAccount()
    {
        using var owner = factory.CreateBrowser();
        using var guest = factory.CreateBrowser();
        using var otherGuest = factory.CreateBrowser();
        await owner.SignInAsync("owner");
        var evt = await CreateEventAsync(owner);
        var (invite, firstToken) = await InviteAsync(owner, evt, $"guest-{Guid.NewGuid():N}@example.test");
        var (otherInvite, otherToken) = await InviteAsync(owner, evt, $"other-{Guid.NewGuid():N}@example.test");

        // Following a mail-scanner GET does not redeem, accept, or open anything.
        var landing = await guest.GetPageAsync($"guest#access={firstToken}");
        Assert.DoesNotContain(firstToken, landing);
        await using (var context = OpenContext())
        {
            var stored = await context.EventUsers.SingleAsync(p => p.PublicId == invite.PublicId);
            Assert.Equal("Pending", stored.Status);
            Assert.Equal(GuestEventService.HashToken(firstToken), stored.GuestTokenHash);
            Assert.NotEqual(firstToken, stored.GuestTokenHash);
        }
        Assert.Equal(HttpStatusCode.BadRequest,
            (await guest.Client.PostAsync("guest/access", new FormUrlEncodedContent([new("token", firstToken)]))).StatusCode);
        var connected = await ConnectAsync(guest, firstToken);
        var cookie = connected.Headers.GetValues("Set-Cookie").Single(c => c.StartsWith("OpenWish.EventGuest=", StringComparison.Ordinal));
        Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("secure", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=strict", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("path=/guest", cookie, StringComparison.OrdinalIgnoreCase);
        using (var forcedCookieApi = factory.CreateApiClient())
        {
            forcedCookieApi.DefaultRequestHeaders.Add("Cookie", cookie.Split(';')[0]);
            Assert.Equal(HttpStatusCode.Unauthorized, (await forcedCookieApi.GetAsync("api/events")).StatusCode);
        }
        var accepted = await PostFormAsync(guest, "guest/respond",
            new("displayName", "Morgan <Guest>"), new("response", "accept"));
        Assert.Equal("/guest?accepted=true", accepted.Headers.Location!.OriginalString);
        await ConnectAsync(otherGuest, otherToken);
        await PostFormAsync(otherGuest, "guest/respond", new("displayName", "Robin Guest"), new("response", "accept"));

        (await owner.Client.PostAsync($"api/events/{evt.PublicId}/draw-names", null)).EnsureSuccessStatusCode();
        var freshToken = TokenFor(invite.InviteeEmail!);
        Assert.NotEqual(firstToken, freshToken);
        Assert.Contains("unavailable", await guest.GetPageAsync("guest"));
        await ConnectAsync(guest, freshToken);
        await ConnectAsync(otherGuest, TokenFor(otherInvite.InviteeEmail!));

        string assignmentPublicId;
        string otherAssignmentPublicId;
        string recipientName;
        await using (var context = OpenContext())
        {
            var assignment = await context.GiftExchanges.Include(g => g.GiverParticipant).Include(g => g.Receiver)
                .Include(g => g.ReceiverParticipant)
                .SingleAsync(g => g.GiverParticipant!.PublicId == invite.PublicId);
            assignmentPublicId = assignment.PublicId;
            recipientName = assignment.Receiver?.UserName ?? assignment.ReceiverParticipant!.GuestDisplayName!;
            otherAssignmentPublicId = await context.GiftExchanges.Where(g => g.GiverParticipant!.PublicId == otherInvite.PublicId)
                .Select(g => g.PublicId).SingleAsync();
            Assert.Null(assignment.MatchOpenedOn);
        }
        var unopened = await guest.GetPageAsync("guest");
        Assert.Contains("Open my gift", unopened);
        Assert.DoesNotContain("guest-match-name", unopened);
        Assert.DoesNotContain(invite.InviteeEmail!, unopened);
        Assert.DoesNotContain(otherInvite.InviteeEmail!, unopened);
        var wrong = await PostFormAsync(guest, "guest/open", new KeyValuePair<string, string>("assignmentPublicId", otherAssignmentPublicId));
        Assert.Equal("/guest?error=changed", wrong.Headers.Location!.OriginalString);
        Assert.Equal(HttpStatusCode.BadRequest,
            (await guest.Client.PostAsync("guest/open", new FormUrlEncodedContent([new("assignmentPublicId", assignmentPublicId)]))).StatusCode);
        await PostFormAsync(guest, "guest/open", new KeyValuePair<string, string>("assignmentPublicId", assignmentPublicId));
        var opened = await guest.GetPageAsync("guest");
        Assert.Contains(WebUtility.HtmlEncode(recipientName), opened);
        Assert.DoesNotContain("Open my gift", opened);
        Assert.DoesNotContain("Open my gift", await guest.GetPageAsync("guest"));
        using var secondDevice = factory.CreateBrowser();
        await ConnectAsync(secondDevice, freshToken);
        Assert.DoesNotContain("Open my gift", await secondDevice.GetPageAsync("guest"));

        foreach (var endpoint in new[]
        {
            $"api/events/{evt.PublicId}", $"api/events/{evt.PublicId}/my-gift-exchange",
            $"api/events/{evt.PublicId}/invitations", "api/wishlists", "api/notifications"
        })
        {
            Assert.Equal(HttpStatusCode.Unauthorized, (await guest.Client.GetAsync(endpoint)).StatusCode);
        }
        var guestPage = await guest.Client.GetAsync("guest");
        Assert.True(guestPage.Headers.CacheControl!.NoStore);
        Assert.Equal("no-referrer", guestPage.Headers.GetValues("Referrer-Policy").Single());
        Assert.Equal("noindex, nofollow", guestPage.Headers.GetValues("X-Robots-Tag").Single());
        evt.UseGiftBoxReveal = false;
        (await owner.Client.PutAsJsonAsync($"api/events/{evt.PublicId}", evt)).EnsureSuccessStatusCode();
        Assert.DoesNotContain("Open my gift", await otherGuest.GetPageAsync("guest"));
        evt.UseGiftBoxReveal = true;
        (await owner.Client.PutAsJsonAsync($"api/events/{evt.PublicId}", evt)).EnsureSuccessStatusCode();
        Assert.Contains("Open my gift", await otherGuest.GetPageAsync("guest"));
    }

    [Fact]
    public async Task GuestAccess_IsRevocableExpiresAndCannotSurviveDisablingOrResetting()
    {
        using var owner = factory.CreateBrowser();
        using var guest = factory.CreateBrowser();
        using var outsider = factory.CreateBrowser();
        await owner.SignInAsync("owner");
        await outsider.SignInAsync("friend");
        var evt = await CreateEventAsync(owner);
        var (invite, token) = await InviteAsync(owner, evt, $"revoke-{Guid.NewGuid():N}@example.test");
        await ConnectAsync(guest, token);
        await PostFormAsync(guest, "guest/respond", new("displayName", "Guest name"), new("response", "accept"));
        var route = $"api/events/{evt.PublicId}/invitations/{invite.PublicId}/guest-access";
        Assert.Equal(HttpStatusCode.Forbidden, (await outsider.Client.PostAsync($"{route}/revoke", null)).StatusCode);
        (await owner.Client.PostAsync($"{route}/revoke", null)).EnsureSuccessStatusCode();
        Assert.Contains("unavailable", await guest.GetPageAsync("guest"));
        await ConnectAsync(guest, token, succeeds: false);
        (await owner.Client.PostAsync($"{route}/send", null)).EnsureSuccessStatusCode();
        var newToken = TokenFor(invite.InviteeEmail!);
        await ConnectAsync(guest, newToken);

        evt.AllowGuestParticipants = null;
        (await owner.Client.PutAsJsonAsync($"api/events/{evt.PublicId}", evt)).EnsureSuccessStatusCode();
        Assert.True((await owner.Client.GetFromJsonAsync<EventModel>($"api/events/{evt.PublicId}"))!.AllowGuestParticipants);
        var copy = await owner.Client.PostAsJsonAsync($"api/events/{evt.PublicId}/run-again",
            new RunEventAgainRequest { Name = "Guest event again", Date = DateTimeOffset.UtcNow.AddDays(45) });
        copy.EnsureSuccessStatusCode();
        Assert.True((await copy.Content.ReadFromJsonAsync<EventModel>())!.AllowGuestParticipants);
        evt.AllowGuestParticipants = false;
        (await owner.Client.PutAsJsonAsync($"api/events/{evt.PublicId}", evt)).EnsureSuccessStatusCode();
        Assert.Contains("unavailable", await guest.GetPageAsync("guest"));
        evt.AllowGuestParticipants = true;
        (await owner.Client.PutAsJsonAsync($"api/events/{evt.PublicId}", evt)).EnsureSuccessStatusCode();
        await ConnectAsync(guest, newToken, succeeds: false);
        (await owner.Client.PostAsync($"{route}/send", null)).EnsureSuccessStatusCode();
        await ConnectAsync(guest, TokenFor(invite.InviteeEmail!));

        await using (var context = OpenContext())
        {
            var participant = await context.EventUsers.SingleAsync(p => p.PublicId == invite.PublicId);
            participant.GuestAccessExpiresOn = DateTimeOffset.UtcNow.AddMinutes(-1);
            await context.SaveChangesAsync();
        }
        Assert.Contains("unavailable", await guest.GetPageAsync("guest"));
        await ConnectAsync(guest, TokenFor(invite.InviteeEmail!), succeeds: false);
        (await owner.Client.PostAsync($"api/events/{evt.PublicId}/draw-names", null)).EnsureSuccessStatusCode();
        await ConnectAsync(guest, TokenFor(invite.InviteeEmail!));
        Assert.Equal(HttpStatusCode.BadRequest, (await owner.Client.PostAsync($"{route}/remove", null)).StatusCode);
        (await owner.Client.PostAsync($"api/events/{evt.PublicId}/reset-gift-exchange", null)).EnsureSuccessStatusCode();
        Assert.Contains("unavailable", await guest.GetPageAsync("guest"));
    }

    [Fact]
    public async Task GuestResponse_ValidatesNameDeclinesSafelyAndVerifiedAccountClaimPreservesTheMatch()
    {
        using var owner = factory.CreateBrowser();
        using var guest = factory.CreateBrowser();
        await owner.SignInAsync("owner");
        var evt = await CreateEventAsync(owner);
        var (invite, token) = await InviteAsync(owner, evt, $"claim-{Guid.NewGuid():N}@example.test");
        await ConnectAsync(guest, token);
        var invalid = await PostFormAsync(guest, "guest/respond", new("displayName", " "), new("response", "accept"));
        Assert.Equal("/guest?error=name", invalid.Headers.Location!.OriginalString);
        var emailName = await PostFormAsync(guest, "guest/respond", new("displayName", invite.InviteeEmail!), new("response", "accept"));
        Assert.Equal("/guest?error=name", emailName.Headers.Location!.OriginalString);
        await PostFormAsync(guest, "guest/respond", new("displayName", "Guest person"), new("response", "accept"));
        (await owner.Client.PostAsync($"api/events/{evt.PublicId}/draw-names", null)).EnsureSuccessStatusCode();
        var freshToken = TokenFor(invite.InviteeEmail!);
        await ConnectAsync(guest, freshToken);
        string assignmentPublicId;
        await using (var context = OpenContext())
        {
            assignmentPublicId = await context.GiftExchanges.Where(g => g.GiverParticipant!.PublicId == invite.PublicId)
                .Select(g => g.PublicId).SingleAsync();
            context.Users.Add(new ApplicationUser
            {
                Id = "claim-" + invite.PublicId,
                UserName = "Claimed guest",
                Email = invite.InviteeEmail,
                NormalizedEmail = invite.InviteeEmail!.ToUpperInvariant(),
                EmailConfirmed = false
            });
            await context.SaveChangesAsync();
        }
        await PostFormAsync(guest, "guest/open", new KeyValuePair<string, string>("assignmentPublicId", assignmentPublicId));
        var registration = await PostFormAsync(guest, "guest/account");
        Assert.StartsWith("/Account/Register?email=", registration.Headers.Location!.OriginalString, StringComparison.Ordinal);
        Assert.Contains("ReturnUrl=", registration.Headers.Location.OriginalString);
        using var scope = factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<OpenWish.Shared.Services.IEventService>();
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.ClaimEventInvitationByEmailAsync(evt.PublicId, "claim-" + invite.PublicId, invite.InviteeEmail));
        evt.AllowGuestParticipants = false;
        (await owner.Client.PutAsJsonAsync($"api/events/{evt.PublicId}", evt)).EnsureSuccessStatusCode();
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.ClaimEventInvitationByEmailAsync(evt.PublicId, "claim-" + invite.PublicId, invite.InviteeEmail));
        await using (var context = OpenContext())
        {
            var user = await context.Users.SingleAsync(u => u.Id == "claim-" + invite.PublicId);
            user.EmailConfirmed = true;
            await context.SaveChangesAsync();
        }
        var claimed = await service.ClaimEventInvitationByEmailAsync(evt.PublicId, "claim-" + invite.PublicId, invite.InviteeEmail);
        Assert.True(claimed!.IsAccepted);
        var accountMatch = await service.GetMyGiftExchangeByPublicIdAsync(evt.PublicId, "claim-" + invite.PublicId);
        Assert.Equal(assignmentPublicId, accountMatch!.PublicId);
        Assert.NotNull(accountMatch.MatchOpenedOn);
        await ConnectAsync(guest, freshToken, succeeds: false);
        Assert.Contains("unavailable", await guest.GetPageAsync("guest"));

        var declineEvt = await CreateEventAsync(owner);
        var (_, declineToken) = await InviteAsync(owner, declineEvt, $"decline-{Guid.NewGuid():N}@example.test");
        using var decliningGuest = factory.CreateBrowser();
        await ConnectAsync(decliningGuest, declineToken);
        var declined = await PostFormAsync(decliningGuest, "guest/respond", new KeyValuePair<string, string>("response", "decline"));
        Assert.Equal("/guest?declined=true", declined.Headers.Location!.OriginalString);
        await ConnectAsync(decliningGuest, declineToken, succeeds: false);
        var (removed, removedToken) = await InviteAsync(owner, declineEvt, $"remove-{Guid.NewGuid():N}@example.test");
        (await owner.Client.PostAsync($"api/events/{declineEvt.PublicId}/invitations/{removed.PublicId}/guest-access/remove", null))
            .EnsureSuccessStatusCode();
        await ConnectAsync(decliningGuest, removedToken, succeeds: false);
        var noGuests = await owner.Client.PostAsJsonAsync("api/events", new EventModel
        {
            Name = "Account-required event",
            Date = DateTimeOffset.UtcNow.AddDays(30),
            IsGiftExchange = true,
            EventUsers = []
        });
        noGuests.EnsureSuccessStatusCode();
        Assert.False((await noGuests.Content.ReadFromJsonAsync<EventModel>())!.AllowGuestParticipants);
    }

    private ApplicationDbContext OpenContext() =>
        factory.Services.GetRequiredService<IDbContextFactory<ApplicationDbContext>>().CreateDbContext();

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AccountAttachment_WaitsForTheEventLockAndRacingDrawKeepsTheMatchAccessible(bool automatic)
    {
        using var owner = factory.CreateBrowser();
        using var guest = factory.CreateBrowser();
        await owner.SignInAsync("owner");
        var evt = await CreateEventAsync(owner);
        using var scope = factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<OpenWish.Shared.Services.IEventService>();
        var email = $"racing-claim-{Guid.NewGuid():N}@example.test";
        var invitation = await service.InviteByEmailToEventByPublicIdAsync(evt.PublicId, evt.CreatedBy!.Id, email);
        await ConnectAsync(guest, TokenFor(email));
        await PostFormAsync(guest, "guest/respond", new("displayName", "Cabin friend"), new("response", "accept"));
        var userId = "racing-" + invitation.PublicId;
        await using var blocker = OpenContext();
        blocker.Users.Add(new ApplicationUser
        {
            Id = userId,
            UserName = email,
            Email = email,
            NormalizedEmail = email.ToUpperInvariant(),
            EmailConfirmed = true
        });
        await blocker.SaveChangesAsync();
        var eventId = await blocker.Events.Where(e => e.PublicId == evt.PublicId).Select(e => e.Id).SingleAsync();
        await using var transaction = await blocker.Database.BeginTransactionAsync();
        await blocker.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM \"Events\" WHERE \"Id\" = {eventId} FOR UPDATE");
        Task attachment = automatic
            ? service.GetPendingInvitationsForUserAsync(userId)
            : service.ClaimEventInvitationByEmailAsync(evt.PublicId, userId, email);
        var draw = service.DrawNamesByPublicIdAsync(evt.PublicId, evt.CreatedBy.Id);
        await Task.Delay(200);
        var waitedForLock = !attachment.IsCompleted;
        await transaction.CommitAsync();
        await Task.WhenAll(attachment, draw);
        Assert.True(waitedForLock, "Account attachment must wait for the same event lock as drawing.");
        var match = await service.GetMyGiftExchangeByPublicIdAsync(evt.PublicId, userId);
        Assert.NotNull(match);
        await using var context = OpenContext();
        var participant = await context.EventUsers.SingleAsync(p => p.PublicId == invitation.PublicId);
        Assert.Equal(userId, participant.UserId);
        Assert.Equal("Cabin friend", participant.GuestDisplayName);
        Assert.Null(participant.GuestTokenHash);
    }

    [Fact]
    public async Task FailedDrawMail_DoesNotSkipLaterGuestsAndCancellationReturnsTheResetRequirement()
    {
        using var owner = factory.CreateBrowser();
        using var firstGuest = factory.CreateBrowser();
        using var secondGuest = factory.CreateBrowser();
        await owner.SignInAsync("owner");
        var evt = await CreateEventAsync(owner);
        using var scope = factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<OpenWish.Shared.Services.IEventService>();
        var firstEmail = $"first-draw-{Guid.NewGuid():N}@example.test";
        var secondEmail = $"second-draw-{Guid.NewGuid():N}@example.test";
        var first = await service.InviteByEmailToEventByPublicIdAsync(evt.PublicId, evt.CreatedBy!.Id, firstEmail);
        var second = await service.InviteByEmailToEventByPublicIdAsync(evt.PublicId, evt.CreatedBy.Id, secondEmail);
        await ConnectAsync(firstGuest, TokenFor(firstEmail));
        await ConnectAsync(secondGuest, TokenFor(secondEmail));
        await PostFormAsync(firstGuest, "guest/respond", new("displayName", "First guest"), new("response", "accept"));
        await PostFormAsync(secondGuest, "guest/respond", new("displayName", "Second guest"), new("response", "accept"));
        factory.Emails.FailGuestAddress = firstEmail;
        try
        {
            var result = await owner.Client.PostAsync($"api/events/{evt.PublicId}/draw-names", null);
            Assert.Equal(HttpStatusCode.BadGateway, result.StatusCode);
            Assert.Contains("Names were drawn", await result.Content.ReadAsStringAsync(), StringComparison.Ordinal);
        }
        finally
        {
            factory.Emails.FailGuestAddress = null;
        }
        Assert.Contains(factory.Emails.Messages, m => m.To == secondEmail && m.Subject.StartsWith("Your private match is ready", StringComparison.Ordinal));
        await using var context = OpenContext();
        var firstParticipant = await context.EventUsers.SingleAsync(p => p.PublicId == first.PublicId);
        var secondParticipant = await context.EventUsers.SingleAsync(p => p.PublicId == second.PublicId);
        Assert.Null(firstParticipant.GuestEmailSentOn);
        Assert.NotNull(secondParticipant.GuestEmailSentOn);
        Assert.NotNull((await context.Events.SingleAsync(e => e.PublicId == evt.PublicId)).NamesDrawnOn);
        var cancellation = await owner.Client.DeleteAsync($"api/events/invitations/{firstParticipant.Id}");
        Assert.Equal(HttpStatusCode.BadRequest, cancellation.StatusCode);
        Assert.Contains("Reset the gift exchange", await cancellation.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task GuestMatch_ShowsTheRegisteredRecipientsHandleWithoutTheirEmail()
    {
        using var owner = factory.CreateBrowser();
        using var guest = factory.CreateBrowser();
        await owner.SignInAsync("owner");
        var evt = await CreateEventAsync(owner);
        using var scope = factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<OpenWish.Shared.Services.IEventService>();
        var email = $"recipient-name-{Guid.NewGuid():N}@example.test";
        await service.InviteByEmailToEventByPublicIdAsync(evt.PublicId, evt.CreatedBy!.Id, email);
        await ConnectAsync(guest, TokenFor(email));
        await PostFormAsync(guest, "guest/respond", new("displayName", "Morgan"), new("response", "accept"));
        await service.DrawNamesByPublicIdAsync(evt.PublicId, evt.CreatedBy.Id);
        await ConnectAsync(guest, TokenFor(email));
        await using var context = OpenContext();
        var recipient = await context.Users.SingleAsync(u => u.Id == evt.CreatedBy.Id);
        var originalName = recipient.UserName;
        try
        {
            recipient.UserName = "avery.person@example.test";
            await context.SaveChangesAsync();
            var assignmentId = await context.GiftExchanges.Where(g => g.Event!.PublicId == evt.PublicId && g.GiverId == null)
                .Select(g => g.PublicId).SingleAsync();
            await PostFormAsync(guest, "guest/open", new KeyValuePair<string, string>("assignmentPublicId", assignmentId));
            var page = await guest.GetPageAsync("guest");
            Assert.Contains("avery person", page);
            Assert.DoesNotContain("avery.person@example.test", page);
        }
        finally
        {
            recipient.UserName = originalName;
            await context.SaveChangesAsync();
        }
    }

    private static async Task<EventModel> CreateEventAsync(OpenWishBrowser owner)
    {
        var response = await owner.Client.PostAsJsonAsync("api/events", new EventModel
        {
            Name = $"Private guest event {Guid.NewGuid():N}",
            Date = DateTimeOffset.UtcNow.AddDays(30),
            Budget = 25,
            Description = "A thoughtful gift",
            IsGiftExchange = true,
            AllowGuestParticipants = true,
            UseGiftBoxReveal = true,
            EventUsers = []
        });
        Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<EventModel>())!;
    }

    private async Task<(EventUserModel Invitation, string Token)> InviteAsync(OpenWishBrowser owner, EventModel evt, string email)
    {
        var response = await owner.Client.PostAsJsonAsync($"api/events/{evt.PublicId}/invitations/email", new { Email = email });
        Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
        var json = await response.Content.ReadAsStringAsync();
        var invitation = (await response.Content.ReadFromJsonAsync<EventUserModel>())!;
        var token = TokenFor(email);
        Assert.DoesNotContain(token, json);
        Assert.DoesNotContain("guestTokenHash", json);
        return (invitation, token);
    }

    private string TokenFor(string email)
    {
        var message = factory.Emails.Messages.Last(m => m.To == email && m.Body.Contains("#access=", StringComparison.Ordinal));
        Assert.DoesNotContain("Your Gift Recipient", message.Body);
        return AccessTokenPattern().Match(message.Body).Groups[1].Value;
    }

    private static async Task<HttpResponseMessage> ConnectAsync(OpenWishBrowser guest, string token, bool succeeds = true)
    {
        var response = await PostFormAsync(guest, "guest/access", new KeyValuePair<string, string>("token", token));
        Assert.Equal(succeeds ? "/guest" : "/guest?error=unavailable", response.Headers.Location!.OriginalString);
        return response;
    }

    private static async Task<HttpResponseMessage> PostFormAsync(OpenWishBrowser guest, string route,
        params KeyValuePair<string, string>[] fields)
    {
        var page = await guest.GetPageAsync("guest");
        var data = fields.Append(new KeyValuePair<string, string>("__RequestVerificationToken", OpenWishBrowser.AntiforgeryToken(page)));
        var response = await guest.Client.PostAsync(route, new FormUrlEncodedContent(data));
        Assert.True(response.StatusCode == HttpStatusCode.Redirect,
            $"Guest form {route} returned {(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
        return response;
    }

    [GeneratedRegex("#access=([A-F0-9]{64})")]
    private static partial Regex AccessTokenPattern();
}

[CollectionDefinition(Name)]
public sealed class GuestEventIntegrationCollection : ICollectionFixture<OpenWishAppFactory>
{
    public const string Name = "OpenWish guest integration host";
}