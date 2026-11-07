using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OpenWish.Application.Integrations;
using OpenWish.Data;
using OpenWish.Shared.Integrations;
using OpenWish.Shared.Models;
using Xunit;

namespace OpenWish.Web.Tests.Integrations;

[Collection(IntegrationPermissionCollection.Name)]
public sealed class IntegrationPermissionTests(OpenWishAppFactory factory)
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExistingItems_RespectConsent_AndSettingsApplyWithoutReplacingTheToken(bool manageExistingItems)
    {
        using var owner = await SignInAsync("owner");
        var list = await owner.CreateWishlistAsync(Unique("Permission"));
        var own = await owner.AddOwnItemAsync(list.PublicId, new WishlistItemModel { Name = "Owner idea", IsPrivate = true });
        var hidden = await owner.AddOwnItemAsync(list.PublicId, new WishlistItemModel { Name = "Hidden surprise" });
        await using var context = await factory.Services.GetRequiredService<IDbContextFactory<ApplicationDbContext>>().CreateDbContextAsync();
        await context.WishlistItems.Where(i => i.PublicId == hidden.PublicId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(i => i.IsHiddenFromOwner, true));

        var other = factory.CreateConnectedApp(OpenWishAppFactory.Pantry);
        await other.ConnectAsync(owner, [list.PublicId]);
        using var otherApi = other.Api();
        var externalId = Unique("other-private-key");
        var otherItem = (await (await otherApi.PostAsJsonAsync("items", new IntegrationItemRequest
        {
            Name = "Other app idea",
            ExternalId = externalId
        })).Content.ReadFromJsonAsync<IntegrationItemModel>())!;
        var app = factory.CreateConnectedApp(OpenWishAppFactory.Studio);
        await app.ConnectAsync(owner, [list.PublicId], manageExistingItems: manageExistingItems);
        using var api = app.Api();
        var ownResponse = await api.GetAsync($"items/{own.PublicId}");
        Assert.Equal(manageExistingItems ? HttpStatusCode.OK : HttpStatusCode.NotFound, ownResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await api.GetAsync($"items/{hidden.PublicId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await api.PutAsJsonAsync($"items/{hidden.PublicId}",
            new IntegrationItemRequest { Name = "Spoiled" })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await api.DeleteAsync($"items/{hidden.PublicId}")).StatusCode);
        Assert.Empty((await api.GetFromJsonAsync<List<IntegrationItemModel>>($"items?externalId={Uri.EscapeDataString(externalId)}"))!);
        Assert.Equal(HttpStatusCode.NotFound, (await api.DeleteAsync($"items/external/{Uri.EscapeDataString(externalId)}")).StatusCode);

        var items = (await api.GetFromJsonAsync<List<IntegrationItemModel>>("items"))!;
        Assert.Equal(manageExistingItems ? 2 : 0, items.Count);
        Assert.All(items, item => Assert.Null(item.ExternalId));
        if (!manageExistingItems)
        {
            Assert.Equal(HttpStatusCode.NotFound, (await api.PutAsJsonAsync($"items/{own.PublicId}",
                new IntegrationItemRequest { Name = "Must not change" })).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await api.DeleteAsync($"items/{otherItem.Id}")).StatusCode);
        }

        await owner.SaveConnectedListsAsync(app.ConnectionId!, [list.PublicId], list.PublicId, manageExistingItems: true);
        var movedTo = await owner.CreateWishlistAsync(Unique("Move destination"));
        await owner.SaveConnectedListsAsync(app.ConnectionId!, [list.PublicId, movedTo.PublicId], list.PublicId);
        var update = await api.PutAsJsonAsync($"items/{own.PublicId}",
            new IntegrationItemRequest { Name = "Managed owner idea", WishlistId = movedTo.PublicId });
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        var savedOwner = await context.WishlistItems.AsNoTracking().SingleAsync(i => i.PublicId == own.PublicId);
        Assert.Null(savedOwner.SourceConnectionId);
        Assert.Null(savedOwner.SourceAppName);
        Assert.True(savedOwner.IsPrivate);
        Assert.Equal(HttpStatusCode.OK, (await api.PutAsJsonAsync($"items/{otherItem.Id}",
            new IntegrationItemRequest { Name = "Managed other app idea" })).StatusCode);
        var savedOther = await context.WishlistItems.AsNoTracking().SingleAsync(i => i.PublicId == otherItem.Id);
        Assert.Equal(OpenWishAppFactory.Pantry.DisplayName, savedOther.SourceAppName);
        Assert.Equal(externalId, savedOther.ExternalId);

        await owner.SaveConnectedListsAsync(app.ConnectionId!, [list.PublicId, movedTo.PublicId], list.PublicId,
            manageExistingItems: false);
        Assert.Equal(HttpStatusCode.NotFound, (await api.DeleteAsync($"items/{own.PublicId}")).StatusCode);
        Assert.Empty((await api.GetFromJsonAsync<List<IntegrationItemModel>>("items"))!);
        await owner.SaveConnectedListsAsync(app.ConnectionId!, [list.PublicId, movedTo.PublicId], list.PublicId);
        Assert.Equal(HttpStatusCode.NoContent, (await api.DeleteAsync($"items/{own.PublicId}")).StatusCode);
    }

    [Fact]
    public async Task AllWishlists_IncludesFutureOwnedLists_ButNeverOtherPeoples_AndCanBeRestrictedAgain()
    {
        using var owner = await SignInAsync("owner");
        using var friend = await SignInAsync("friend");
        var first = await owner.CreateWishlistAsync(Unique("All lists"));
        var friendsList = await friend.CreateWishlistAsync(Unique("Never shared"));
        var app = factory.CreateConnectedApp(OpenWishAppFactory.Studio);
        await app.ConnectAsync(owner, [], allowAllWishlists: true);
        using var api = app.Api();
        var future = await owner.CreateWishlistAsync(Unique("Future private list"));
        var lists = (await api.GetFromJsonAsync<List<IntegrationWishlistModel>>("wishlists"))!;
        Assert.Contains(lists, w => w.Id == first.PublicId);
        Assert.Contains(lists, w => w.Id == future.PublicId);
        Assert.DoesNotContain(lists, w => w.Id == friendsList.PublicId);
        using var scope = factory.Services.CreateScope();
        var connectionService = scope.ServiceProvider.GetRequiredService<IIntegrationConnectionService>();
        var caller = (await connectionService.AuthenticateAsync(app.AccessToken))!;
        var summary = Assert.Single(await connectionService.GetConnectedAppsAsync(caller.UserId),
            c => c.ConnectionId == app.ConnectionId);
        Assert.Contains(summary.Wishlists, w => w.PublicId == future.PublicId);
        Assert.DoesNotContain(summary.Wishlists, w => w.PublicId == friendsList.PublicId);
        Assert.Equal(HttpStatusCode.Created, (await api.PostAsJsonAsync("items",
            new IntegrationItemRequest { Name = "Future idea", WishlistId = future.PublicId })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await api.PostAsJsonAsync("items",
            new IntegrationItemRequest { Name = "Forbidden idea", WishlistId = friendsList.PublicId })).StatusCode);

        await owner.SaveConnectedListsAsync(app.ConnectionId!, [first.PublicId], first.PublicId);
        Assert.Equal(first.PublicId, Assert.Single((await api.GetFromJsonAsync<List<IntegrationWishlistModel>>("wishlists"))!).Id);
        Assert.Equal(HttpStatusCode.Forbidden, (await api.PostAsJsonAsync("items",
            new IntegrationItemRequest { Name = "No longer allowed", WishlistId = future.PublicId })).StatusCode);
        await owner.SaveConnectedListsAsync(app.ConnectionId!, [], future.PublicId, allowAllWishlists: true);
        lists = (await api.GetFromJsonAsync<List<IntegrationWishlistModel>>("wishlists"))!;
        Assert.Equal(future.PublicId, lists.Single(w => w.IsDefault).Id);
        var anotherFuture = await owner.CreateWishlistAsync(Unique("Another future list"));
        await using var context = await factory.Services.GetRequiredService<IDbContextFactory<ApplicationDbContext>>().CreateDbContextAsync();
        await context.Wishlists.Where(w => w.PublicId == future.PublicId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(w => w.Deleted, true));
        lists = (await api.GetFromJsonAsync<List<IntegrationWishlistModel>>("wishlists"))!;
        var fallbackId = lists.Single(w => w.IsDefault).Id;
        summary = Assert.Single(await connectionService.GetConnectedAppsAsync(caller.UserId),
            c => c.ConnectionId == app.ConnectionId);
        Assert.Contains(summary.Wishlists, w => w.PublicId == anotherFuture.PublicId);
        Assert.Equal(fallbackId, summary.Wishlists.Single(w => w.IsDefault).PublicId);
        var consent = await connectionService.GetConsentStateAsync(caller.UserId, caller.ClientId);
        Assert.Equal(fallbackId, consent.Wishlists.Single(w => w.IsDefault).PublicId);
    }

    [Fact]
    public async Task Reconnect_RetainsExplicitPrivacyChoices()
    {
        using var owner = await SignInAsync("owner");
        var list = await owner.CreateWishlistAsync(Unique("Reconnect"));
        var app = factory.CreateConnectedApp(OpenWishAppFactory.Studio);
        await app.ConnectAsync(owner, [list.PublicId], allowAllWishlists: true, manageExistingItems: false);
        var page = await owner.GetPageAsync(app.BuildAuthorizeUrl());
        Assert.Contains("id=\"consent-all-lists\" name=\"allowAllWishlists\" value=\"true\" checked", page);
        Assert.DoesNotContain("id=\"consent-existing-items\" name=\"manageExistingItems\" value=\"true\" checked", page);
        var settings = await owner.GetPageAsync($"Account/Manage/ConnectedApps?edit={app.ConnectionId}");
        Assert.Contains($"id=\"all-lists-{app.ConnectionId}\" checked", settings);
        Assert.DoesNotContain($"id=\"existing-items-{app.ConnectionId}\" checked", settings);
    }

    [Fact]
    public async Task PendingConsent_CannotRestoreBroaderItemOrListPermissions()
    {
        using var owner = await SignInAsync("owner");
        var list = await owner.CreateWishlistAsync(Unique("Pending permissions"));
        var active = factory.CreateConnectedApp(OpenWishAppFactory.Studio);
        await active.ConnectAsync(owner, [list.PublicId], allowAllWishlists: true);
        var pending = factory.CreateConnectedApp(OpenWishAppFactory.Studio);
        var code = pending.ReadCallback(await owner.ApproveAsync(pending.BuildAuthorizeUrl(), [list.PublicId],
            allowAllWishlists: true));
        await owner.SaveConnectedListsAsync(active.ConnectionId!, [list.PublicId], list.PublicId, manageExistingItems: false);
        Assert.Equal(HttpStatusCode.BadRequest, (await pending.RequestTokenAsync(code)).StatusCode);
        using var scope = factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IIntegrationConnectionService>();
        var caller = (await service.AuthenticateAsync(active.AccessToken))!;
        var state = await service.GetConsentStateAsync(caller.UserId, caller.ClientId);
        Assert.False(state.AllowAllWishlists);
        Assert.False(state.ManageExistingItems);
    }

    [Theory]
    [InlineData(false, true, true)]
    [InlineData(true, false, true)]
    [InlineData(false, false, true)]
    [InlineData(false, true, false)]
    public async Task RestrictedReconnect_InvalidatesOlderBroaderConsent(bool allowAllWishlists, bool manageExistingItems,
        bool olderAllowsAllWishlists)
    {
        using var owner = await SignInAsync("owner");
        var list = await owner.CreateWishlistAsync(Unique("Superseded consent"));
        var removedList = await owner.CreateWishlistAsync(Unique("Removed consent list"));
        var idea = await owner.AddOwnItemAsync(list.PublicId, new WishlistItemModel { Name = "Existing idea" });
        var active = factory.CreateConnectedApp(OpenWishAppFactory.Studio);
        await active.ConnectAsync(owner, [list.PublicId, removedList.PublicId], allowAllWishlists: olderAllowsAllWishlists);
        var pending = factory.CreateConnectedApp(OpenWishAppFactory.Studio);
        var olderCode = pending.ReadCallback(await owner.ApproveAsync(pending.BuildAuthorizeUrl(),
            [list.PublicId, removedList.PublicId], allowAllWishlists: olderAllowsAllWishlists));
        var restricted = factory.CreateConnectedApp(OpenWishAppFactory.Studio);
        await restricted.ConnectAsync(owner, [list.PublicId], allowAllWishlists: allowAllWishlists,
            manageExistingItems: manageExistingItems);

        Assert.Equal(HttpStatusCode.BadRequest, (await pending.RequestTokenAsync(olderCode)).StatusCode);
        using var api = restricted.Api();
        Assert.Equal(manageExistingItems ? HttpStatusCode.OK : HttpStatusCode.NotFound,
            (await api.GetAsync($"items/{idea.PublicId}")).StatusCode);
        if (!allowAllWishlists)
        {
            Assert.Equal(list.PublicId, Assert.Single((await api.GetFromJsonAsync<List<IntegrationWishlistModel>>("wishlists"))!).Id);
        }
    }

    [Fact]
    public async Task SettingsValidation_PreservesSubmittedPrivacyAndDefaultChoices()
    {
        using var owner = await SignInAsync("owner");
        var selected = await owner.CreateWishlistAsync(Unique("Selected"));
        var uncheckedList = await owner.CreateWishlistAsync(Unique("Unchecked default"));
        var own = await owner.AddOwnItemAsync(selected.PublicId, new WishlistItemModel { Name = "Owner idea" });
        var app = factory.CreateConnectedApp(OpenWishAppFactory.Studio);
        await app.ConnectAsync(owner, [selected.PublicId], allowAllWishlists: true);
        var url = $"Account/Manage/ConnectedApps?edit={app.ConnectionId}";
        var fields = OpenWishBrowser.FormFields(await owner.GetPageAsync(url), $"lists-{app.ConnectionId}");
        fields.Add(new("wishlist", selected.PublicId));
        fields.Add(new("defaultWishlist", uncheckedList.PublicId));
        var invalid = await owner.Client.PostAsync(url, new FormUrlEncodedContent(fields));
        Assert.Equal(HttpStatusCode.OK, invalid.StatusCode);
        var errorPage = await invalid.Content.ReadAsStringAsync();
        Assert.Contains("Where new ideas go must be one of the checked lists.", errorPage);
        Assert.DoesNotContain($"id=\"existing-items-{app.ConnectionId}\" checked", errorPage);
        Assert.DoesNotContain($"id=\"all-lists-{app.ConnectionId}\" checked", errorPage);
        Assert.Contains($"value=\"{uncheckedList.PublicId}\" selected", errorPage);
        Assert.Contains($"id=\"allow-{app.ConnectionId}-{selected.PublicId}\" name=\"wishlist\" value=\"{selected.PublicId}\" checked", errorPage);
        Assert.DoesNotContain($"id=\"allow-{app.ConnectionId}-{uncheckedList.PublicId}\" name=\"wishlist\" value=\"{uncheckedList.PublicId}\" checked", errorPage);

        var corrected = OpenWishBrowser.FormFields(errorPage, $"lists-{app.ConnectionId}");
        corrected.Add(new("wishlist", selected.PublicId));
        corrected.Add(new("defaultWishlist", selected.PublicId));
        Assert.Equal(HttpStatusCode.Redirect,
            (await owner.Client.PostAsync(url, new FormUrlEncodedContent(corrected))).StatusCode);
        using var api = app.Api();
        Assert.Equal(HttpStatusCode.NotFound, (await api.GetAsync($"items/{own.PublicId}")).StatusCode);
    }

    private async Task<OpenWishBrowser> SignInAsync(string role)
    {
        var browser = factory.CreateBrowser();
        await browser.SignInAsync(role);
        return browser;
    }

    private static string Unique(string name) => $"{name} {Guid.NewGuid():N}";
}

[CollectionDefinition(Name)]
public sealed class IntegrationPermissionCollection : ICollectionFixture<OpenWishAppFactory>
{
    public const string Name = "OpenWish permission host";
}