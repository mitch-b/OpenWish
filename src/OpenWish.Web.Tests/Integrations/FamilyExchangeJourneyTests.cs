using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OpenWish.Data;
using OpenWish.Data.Entities;
using OpenWish.Shared.Models;
using OpenWish.Shared.Services;
using Xunit;

namespace OpenWish.Web.Tests.Integrations;

[Collection(IntegrationCollection.Name)]
public sealed partial class FamilyExchangeJourneyTests(OpenWishAppFactory factory)
{
    [Fact]
    public async Task GuestRegistration_ConfirmsEmailAndReturnsToTheInvitationThroughRealForms()
    {
        var sender = new ConfirmationSender();
        using var registrationHost = factory.WithWebHostBuilder(builder =>
            builder.ConfigureServices(services => services.AddSingleton<IEmailSender<ApplicationUser>>(sender)));
        using var client = registrationHost.CreateClient(new()
        {
            AllowAutoRedirect = false,
            HandleCookies = true,
            BaseAddress = new Uri("https://localhost/")
        });
        var email = $"family-{Guid.NewGuid():N}@example.test";
        var destination = $"/events/demo-holiday-gift-exchange/accept-invite?email={Uri.EscapeDataString(email)}";
        var registrationUrl = QueryHelpers.AddQueryString("/Account/Register",
            new Dictionary<string, string?> { ["ReturnUrl"] = destination, ["email"] = email });
        var registration = await client.GetStringAsync(registrationUrl);
        Assert.Contains($"value=\"{email}\"", registration);
        var fields = OpenWishBrowser.FormFields(registration, "register");
        fields.AddRange([
            new("Input.Email", email),
            new("Input.Password", "Family-Test-2026!"),
            new("Input.ConfirmPassword", "Family-Test-2026!")
        ]);
        var registered = await client.PostAsync(registrationUrl, new FormUrlEncodedContent(fields));
        Assert.Equal(HttpStatusCode.Redirect, registered.StatusCode);
        Assert.Contains("RegisterConfirmation", registered.Headers.Location!.OriginalString);
        Assert.Equal(destination, QueryHelpers.ParseQuery(Assert.IsType<Uri>(sender.Link).Query)["returnUrl"]);

        using (var scope = registrationHost.Services.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = await userManager.FindByEmailAsync(email);
            Assert.NotNull(user);
            Assert.False(user.EmailConfirmed);
        }

        var resendUrl = QueryHelpers.AddQueryString("/Account/ResendEmailConfirmation",
            new Dictionary<string, string?> { ["ReturnUrl"] = destination, ["email"] = email });
        var resend = await client.GetStringAsync(resendUrl);
        Assert.Contains($"value=\"{email}\"", resend);
        var resendFields = OpenWishBrowser.FormFields(resend, "resend-email-confirmation");
        resendFields.Add(new("Input.Email", email));
        var resent = await client.PostAsync(resendUrl, new FormUrlEncodedContent(resendFields));
        Assert.Equal(HttpStatusCode.OK, resent.StatusCode);
        var callback = Assert.IsType<Uri>(sender.Link);
        Assert.Equal(destination, QueryHelpers.ParseQuery(callback.Query)["returnUrl"]);
        var confirmation = await client.GetStringAsync(callback.PathAndQuery);
        Assert.Contains("Your email is confirmed", confirmation);
        var loginMatch = ContinueLinkPattern().Match(confirmation);
        Assert.True(loginMatch.Success, confirmation);
        var loginUrl = WebUtility.HtmlDecode(loginMatch.Groups[1].Value);
        var loginQuery = QueryHelpers.ParseQuery(new Uri(client.BaseAddress!, loginUrl).Query);
        Assert.Equal(destination, loginQuery["ReturnUrl"]);
        Assert.Equal(email, loginQuery["email"]);
        var login = await client.GetStringAsync(loginUrl);
        var loginFields = OpenWishBrowser.FormFields(login, "login");
        loginFields.AddRange([new("Input.Email", email), new("Input.Password", "Family-Test-2026!")]);
        var signedIn = await client.PostAsync(loginUrl, new FormUrlEncodedContent(loginFields));
        Assert.Equal(HttpStatusCode.Redirect, signedIn.StatusCode);
        Assert.Equal(destination, new Uri(client.BaseAddress!, signedIn.Headers.Location!).PathAndQuery);
    }

    [Fact]
    public async Task InvitedEmailPrefill_DoesNotOverwriteCorrectionsAfterValidationFails()
    {
        using var browser = factory.CreateBrowser();
        const string url = "/Account/Register?email=invited%40example.test&ReturnUrl=%2Fevents";
        var form = await browser.GetPageAsync(url);
        var fields = OpenWishBrowser.FormFields(form, "register");
        fields.AddRange([
            new("Input.Email", "corrected@example.test"),
            new("Input.Password", "short"),
            new("Input.ConfirmPassword", "short")
        ]);
        var response = await browser.Client.PostAsync(url, new FormUrlEncodedContent(fields));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var invalidForm = await response.Content.ReadAsStringAsync();
        Assert.Contains("value=\"corrected@example.test\"", invalidForm);
        Assert.DoesNotContain("value=\"invited@example.test\"", invalidForm);
    }

    [Fact]
    public async Task EmailInvitations_RecognizeExistingAccountsRegardlessOfCaseOrWhitespace()
    {
        using var organizer = factory.CreateBrowser();
        await organizer.SignInAsync("owner");
        using var guest = factory.CreateBrowser();
        await guest.SignInAsync("guest");
        var created = await organizer.Client.PostAsJsonAsync("api/events",
            new EventModel { Name = $"Family {Guid.NewGuid():N}", Date = DateTimeOffset.UtcNow.AddDays(30), IsGiftExchange = true, EventUsers = [] });
        Assert.True(created.IsSuccessStatusCode, await created.Content.ReadAsStringAsync());
        var eventModel = (await created.Content.ReadFromJsonAsync<EventModel>())!;
        var invited = await organizer.Client.PostAsJsonAsync($"api/events/{eventModel.PublicId}/invitations/email",
            new { Email = " PLAYWRIGHT-GUEST@OPENWISH.LOCAL " });
        invited.EnsureSuccessStatusCode();
        var invitation = (await invited.Content.ReadFromJsonAsync<EventUserModel>())!;
        Assert.NotNull(invitation.UserId);
        var currentUser = (await guest.Client.GetFromJsonAsync<ApplicationUserModel>("api/account/user"))!;
        Assert.Equal(currentUser.Email, invitation.User?.Email);
        var repeated = await organizer.Client.PostAsJsonAsync($"api/events/{eventModel.PublicId}/invitations/email",
            new { Email = "playwright-guest@openwish.local" });
        Assert.Equal(HttpStatusCode.BadRequest, repeated.StatusCode);
    }

    [Fact]
    public async Task MissingInvitationAndMalformedConfirmation_OfferRecoveryInsteadOfServerErrors()
    {
        using var browser = factory.CreateBrowser();
        await browser.SignInAsync("guest");
        var claim = await browser.Client.PostAsJsonAsync("api/events/missing-family-event/invitations/claim",
            new { Email = "playwright-guest@openwish.local" });
        Assert.Equal(HttpStatusCode.NotFound, claim.StatusCode);
        var invitation = await browser.GetPageAsync("/events/missing-family-event/accept-invite?email=playwright-guest%40openwish.local");
        Assert.Contains("This event is no longer available", invitation);
        Assert.Contains("Back to events", invitation);
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var user = await context.Users.SingleAsync(user => user.Email == "playwright-guest@openwish.local");
        var confirmation = await browser.GetPageAsync($"/Account/ConfirmEmail?UserId={user.Id}&Code=not%21base64");
        Assert.Contains("This confirmation link isn", confirmation);
        Assert.Contains("Send a new confirmation link", confirmation);
    }

    [Theory]
    [InlineData("not!base64")]
    [InlineData("dGVzdA")]
    public async Task InvalidConfirmation_DoesNotExposeTheAccountsEmail(string code)
    {
        using var browser = factory.CreateBrowser();
        using var scope = factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await CreateFamilyUserAsync(userManager, "confirmation", confirmed: false);
        var confirmation = await browser.GetPageAsync(
            $"/Account/ConfirmEmail?UserId={user.Id}&Code={Uri.EscapeDataString(code)}");

        Assert.Contains("Send a new confirmation link", confirmation);
        Assert.DoesNotContain(user.Email!, confirmation, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(Uri.EscapeDataString(user.Email!), confirmation, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task InvitationClaim_DistinguishesAnUnavailableEventFromNoMatchingInvitation()
    {
        using var organizer = factory.CreateBrowser();
        await organizer.SignInAsync("owner");
        using var guest = factory.CreateBrowser();
        await guest.SignInAsync("guest");
        var created = await organizer.Client.PostAsJsonAsync("api/events",
            new EventModel { Name = $"No invitation {Guid.NewGuid():N}", Date = DateTimeOffset.UtcNow.AddDays(30), EventUsers = [] });
        created.EnsureSuccessStatusCode();
        var eventModel = (await created.Content.ReadFromJsonAsync<EventModel>())!;

        var noInvitation = await guest.Client.PostAsJsonAsync($"api/events/{eventModel.PublicId}/invitations/claim",
            new { Email = "playwright-guest@openwish.local" });
        Assert.Equal(HttpStatusCode.NoContent, noInvitation.StatusCode);
        var noEvent = await guest.Client.PostAsJsonAsync("api/events/missing-family-event/invitations/claim",
            new { Email = "playwright-guest@openwish.local" });
        Assert.Equal(HttpStatusCode.NotFound, noEvent.StatusCode);
    }

    [Fact]
    public async Task EventOnlyWishlist_RestrictsOutsideFriendsAndPreservesSurpriseIdeas()
    {
        using var scope = factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var host = await CreateFamilyUserAsync(userManager, "host");
        var participant = await CreateFamilyUserAsync(userManager, "participant");
        var outsideFriend = await CreateFamilyUserAsync(userManager, "outside");
        var service = scope.ServiceProvider.GetRequiredService<IEventService>();
        var eventModel = await service.CreateEventAsync(new()
        {
            Name = $"Family privacy {Guid.NewGuid():N}",
            Date = DateTimeOffset.UtcNow.AddDays(30),
            IsGiftExchange = true
        }, host.Id);
        var invitation = await service.InviteByEmailToEventByPublicIdAsync(eventModel.PublicId, host.Id, participant.Email!);
        Assert.True(await service.AcceptEventInvitationByPublicIdAsync(invitation.PublicId, participant.Id));
        using var participantBrowser = factory.CreateBrowser();
        await SignInWithPasswordAsync(participantBrowser, participant.Email!);
        var created = await participantBrowser.Client.PostAsJsonAsync($"api/events/{eventModel.PublicId}/wishlists",
            new WishlistModel { Name = "My gift ideas", IsFriendsOnly = true });
        created.EnsureSuccessStatusCode();
        var wishlist = (await created.Content.ReadFromJsonAsync<WishlistModel>())!;
        Assert.Empty(wishlist.Permissions);

        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var entity = await context.Wishlists.SingleAsync(list => list.PublicId == wishlist.PublicId);
        context.Friends.Add(new Friend { UserId = participant.Id, FriendUserId = outsideFriend.Id });
        context.WishlistItems.Add(new WishlistItem
        {
            WishlistId = entity.Id,
            Name = "A secret gift idea",
            IsHiddenFromOwner = true
        });
        var drawnEvent = await context.Events.SingleAsync(item => item.PublicId == eventModel.PublicId);
        drawnEvent.NamesDrawnOn = DateTimeOffset.UtcNow;
        await context.SaveChangesAsync();

        using var outsiderBrowser = factory.CreateBrowser();
        await SignInWithPasswordAsync(outsiderBrowser, outsideFriend.Email!);
        var denied = await outsiderBrowser.Client.GetAsync($"api/wishlists/{wishlist.PublicId}");
        Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
        var ownerView = (await participantBrowser.Client.GetFromJsonAsync<WishlistModel>($"api/wishlists/{wishlist.PublicId}"))!;
        Assert.Empty(ownerView.Items);
        using var hostBrowser = factory.CreateBrowser();
        await SignInWithPasswordAsync(hostBrowser, host.Email!);
        var giverView = (await hostBrowser.Client.GetFromJsonAsync<WishlistModel>($"api/wishlists/{wishlist.PublicId}"))!;
        Assert.Contains(giverView.Items, item => item.Name == "A secret gift idea");
        var eventPage = await participantBrowser.GetPageAsync($"/events/{eventModel.PublicId}");
        Assert.Contains("Help your gift giver choose something", eventPage);
        Assert.DoesNotContain("gift giver can&#x27;t see any ideas", eventPage);
        Assert.DoesNotContain("A secret gift idea", eventPage);
    }

    [Fact]
    public async Task InvitationClaim_TreatsUnderscoresAsLiteralEmailCharacters()
    {
        using var organizer = factory.CreateBrowser();
        await organizer.SignInAsync("owner");
        using var scope = factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var suffix = Guid.NewGuid().ToString("N");
        var email = $"family_one-{suffix}@example.test";
        var user = new ApplicationUser { UserName = email, Email = email };
        Assert.True((await userManager.CreateAsync(user, "Family-Test-2026!")).Succeeded);
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var owner = await context.Users.SingleAsync(user => user.Email == "playwright-owner@openwish.local");
        var service = scope.ServiceProvider.GetRequiredService<IEventService>();
        var eventModel = await service.CreateEventAsync(new()
        {
            Name = $"Literal emails {suffix}",
            Date = DateTimeOffset.UtcNow.AddDays(30),
            IsGiftExchange = true
        }, owner.Id);
        await service.InviteByEmailToEventByPublicIdAsync(eventModel.PublicId, owner.Id, $"familyXone-{suffix}@example.test");
        Assert.Null(await service.ClaimEventInvitationByEmailAsync(eventModel.PublicId, user.Id, email));
    }

    [GeneratedRegex("href=\"([^\"]+)\"[^>]*>Sign in and continue</a>")]
    private static partial Regex ContinueLinkPattern();

    private static async Task<ApplicationUser> CreateFamilyUserAsync(UserManager<ApplicationUser> manager, string role, bool confirmed = true)
    {
        var email = $"family-{role}-{Guid.NewGuid():N}@example.test";
        var user = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = confirmed };
        Assert.True((await manager.CreateAsync(user, "Family-Test-2026!")).Succeeded);
        return user;
    }

    private static async Task SignInWithPasswordAsync(OpenWishBrowser browser, string email)
    {
        var login = await browser.GetPageAsync("/Account/Login");
        var fields = OpenWishBrowser.FormFields(login, "login");
        fields.AddRange([new("Input.Email", email), new("Input.Password", "Family-Test-2026!")]);
        var response = await browser.Client.PostAsync("/Account/Login", new FormUrlEncodedContent(fields));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
    }

    private sealed class ConfirmationSender : IEmailSender<ApplicationUser>
    {
        public Uri? Link { get; private set; }

        public Task SendConfirmationLinkAsync(ApplicationUser user, string email, string confirmationLink)
        {
            Link = new Uri(confirmationLink);
            return Task.CompletedTask;
        }

        public Task SendPasswordResetLinkAsync(ApplicationUser user, string email, string resetLink) =>
            throw new NotSupportedException();

        public Task SendPasswordResetCodeAsync(ApplicationUser user, string email, string resetCode) =>
            throw new NotSupportedException();
    }
}