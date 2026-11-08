using System.Net;
using System.Net.Http.Json;
using OpenWish.Shared.Models;
using OpenWish.Shared.RequestModels;
using Xunit;

namespace OpenWish.Web.Tests.Integrations;

[Collection(IntegrationCollection.Name)]
public sealed class MatchOpeningTests(OpenWishAppFactory factory)
{
    [Fact]
    public async Task Opening_PersistsOnceAndRejectsOtherPeoplesAssignmentsAndStaleDraws()
    {
        using var owner = factory.CreateBrowser();
        using var friend = factory.CreateBrowser();
        using var outsider = factory.CreateBrowser();
        await owner.SignInAsync("owner");
        await friend.SignInAsync("friend");
        await outsider.SignInAsync("guest");
        var created = await owner.Client.PostAsJsonAsync("api/events", new EventModel
        {
            Name = $"Unwrap {Guid.NewGuid():N}",
            Date = DateTimeOffset.UtcNow.AddDays(30),
            IsGiftExchange = true,
            UseGiftBoxReveal = true,
            EventUsers = []
        });
        created.EnsureSuccessStatusCode();
        var evt = (await created.Content.ReadFromJsonAsync<EventModel>())!;
        var invitationResponse = await owner.Client.PostAsJsonAsync($"api/events/{evt.PublicId}/invitations/email",
            new { Email = "playwright-friend@openwish.local" });
        invitationResponse.EnsureSuccessStatusCode();
        var invitation = (await invitationResponse.Content.ReadFromJsonAsync<EventUserModel>())!;
        (await friend.Client.PostAsync($"api/events/invitations/by-public-id/{invitation.PublicId}/accept", null))
            .EnsureSuccessStatusCode();
        var drawn = await owner.Client.PostAsync($"api/events/{evt.PublicId}/draw-names", null);
        drawn.EnsureSuccessStatusCode();
        var ownMatch = (await owner.Client.GetFromJsonAsync<GiftExchangeModel>($"api/events/{evt.PublicId}/my-gift-exchange"))!;
        var friendMatch = (await friend.Client.GetFromJsonAsync<GiftExchangeModel>($"api/events/{evt.PublicId}/my-gift-exchange"))!;
        Assert.Null(friendMatch.MatchOpenedOn);
        var route = $"api/events/{evt.PublicId}/my-gift-exchange/{friendMatch.PublicId}/open";

        Assert.Equal(HttpStatusCode.Forbidden, (await outsider.Client.PostAsync(route, null)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await owner.Client.PostAsync(route, null)).StatusCode);
        var concurrent = await Task.WhenAll(friend.Client.PostAsync(route, null), friend.Client.PostAsync(route, null));
        foreach (var response in concurrent)
        {
            response.EnsureSuccessStatusCode();
        }
        var first = (await concurrent[0].Content.ReadFromJsonAsync<GiftExchangeModel>())!;
        var repeated = (await concurrent[1].Content.ReadFromJsonAsync<GiftExchangeModel>())!;
        Assert.NotNull(first.MatchOpenedOn);
        Assert.Equal(first.MatchOpenedOn, repeated.MatchOpenedOn);
        Assert.Equal(friendMatch.ReceiverId, first.ReceiverId);
        var persisted = (await friend.Client.GetFromJsonAsync<GiftExchangeModel>($"api/events/{evt.PublicId}/my-gift-exchange"))!;
        Assert.Equal(first.MatchOpenedOn, persisted.MatchOpenedOn);
        Assert.Null((await owner.Client.GetFromJsonAsync<GiftExchangeModel>($"api/events/{evt.PublicId}/my-gift-exchange"))!.MatchOpenedOn);

        (await owner.Client.PostAsync($"api/events/{evt.PublicId}/reset-gift-exchange", null)).EnsureSuccessStatusCode();
        (await owner.Client.PostAsync($"api/events/{evt.PublicId}/draw-names", null)).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.Conflict, (await friend.Client.PostAsync(route, null)).StatusCode);
        var redrawn = (await friend.Client.GetFromJsonAsync<GiftExchangeModel>($"api/events/{evt.PublicId}/my-gift-exchange"))!;
        Assert.NotEqual(friendMatch.PublicId, redrawn.PublicId);
        Assert.Null(redrawn.MatchOpenedOn);
        Assert.NotEqual(ownMatch.PublicId, redrawn.PublicId);
    }

    [Fact]
    public async Task RevealChoice_DefaultsOffAndSurvivesOmittedUpdatesAndRunAgain()
    {
        using var owner = factory.CreateBrowser();
        await owner.SignInAsync("owner");
        var create = await owner.Client.PostAsJsonAsync("api/events", new EventModel
        {
            Name = $"Reveal choice {Guid.NewGuid():N}",
            Date = DateTimeOffset.UtcNow.AddDays(30),
            IsGiftExchange = true,
            EventUsers = []
        });
        create.EnsureSuccessStatusCode();
        var evt = (await create.Content.ReadFromJsonAsync<EventModel>())!;
        Assert.False(evt.UseGiftBoxReveal);
        evt.UseGiftBoxReveal = true;
        var enabled = await owner.Client.PutAsJsonAsync($"api/events/{evt.PublicId}", evt);
        Assert.True(enabled.IsSuccessStatusCode, await enabled.Content.ReadAsStringAsync());
        evt.UseGiftBoxReveal = null;
        (await owner.Client.PutAsJsonAsync($"api/events/{evt.PublicId}", evt)).EnsureSuccessStatusCode();
        var preserved = (await owner.Client.GetFromJsonAsync<EventModel>($"api/events/{evt.PublicId}"))!;
        Assert.True(preserved.UseGiftBoxReveal);
        var runAgain = await owner.Client.PostAsJsonAsync($"api/events/{evt.PublicId}/run-again", new RunEventAgainRequest
        {
            Name = "Another unwrap",
            Date = DateTimeOffset.UtcNow.AddDays(60)
        });
        runAgain.EnsureSuccessStatusCode();
        Assert.True((await runAgain.Content.ReadFromJsonAsync<EventModel>())!.UseGiftBoxReveal);
        evt.UseGiftBoxReveal = false;
        (await owner.Client.PutAsJsonAsync($"api/events/{evt.PublicId}", evt)).EnsureSuccessStatusCode();
        Assert.False((await owner.Client.GetFromJsonAsync<EventModel>($"api/events/{evt.PublicId}"))!.UseGiftBoxReveal);
    }
}