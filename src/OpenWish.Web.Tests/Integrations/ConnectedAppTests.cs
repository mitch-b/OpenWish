using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OpenWish.Data;
using OpenWish.Data.Entities;
using OpenWish.Shared.Integrations;
using OpenWish.Shared.Models;
using Xunit;

namespace OpenWish.Web.Tests.Integrations;

/// <summary>
/// End-to-end coverage of a third-party app connecting to a person's OpenWish account and
/// managing ideas on their behalf, through the real HTTP pipeline and PostgreSQL.
/// </summary>
[Collection(IntegrationCollection.Name)]
public sealed class ConnectedAppTests(OpenWishAppFactory factory)
{
    private static readonly System.Text.Json.JsonSerializerOptions _json = OpenWishBrowser.Json;

    [Fact]
    public async Task ConnectedApp_AddsEditsAndRemovesIdeas_OnBehalfOfTheConnectedPerson()
    {
        using var browser = await SignInAsync("owner");
        var list = await browser.CreateWishlistAsync(Unique("Christmas"));
        var app = factory.CreateConnectedApp(OpenWishAppFactory.Studio);

        var callback = await browser.ApproveAsync(app.BuildAuthorizeUrl(), [list.PublicId]);
        var code = app.ReadCallback(callback);
        Assert.StartsWith("owc_", code);
        var token = await app.ExchangeAsync(code);
        Assert.StartsWith("owi_", token.AccessToken);
        Assert.Equal("Bearer", token.TokenType);
        Assert.Equal(IntegrationScopes.WishlistItems, token.Scope);

        using var api = app.Api();
        var me = (await api.GetFromJsonAsync<IntegrationProfileModel>("me", _json))!;
        Assert.Equal(token.ConnectionId, me.ConnectionId);
        Assert.Equal("Studio Notebook", me.App.Name);
        Assert.Equal("AlexDemo", me.User.DisplayName);
        var allowed = Assert.Single(me.Wishlists);
        Assert.Equal(list.PublicId, allowed.Id);
        Assert.True(allowed.IsDefault);
        Assert.Equal($"https://wish.example.test/wishlists/{list.PublicId}", allowed.Url);

        // Log an idea.
        var created = await api.PutAsJsonAsync("items/external/piece-42", new IntegrationItemRequest
        {
            Name = "Cashmere crewneck",
            Description = "Oatmeal, size S",
            Url = "https://shop.example.com/crewneck",
            ImageUrl = "https://shop.example.com/crewneck.jpg",
            Price = 129.00m,
            WhereToBuy = "Example Shop",
            Priority = 1
        }, _json);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var item = (await created.Content.ReadFromJsonAsync<IntegrationItemModel>(_json))!;
        Assert.EndsWith($"/api/integrations/v1/items/{item.Id}", created.Headers.Location!.ToString());
        Assert.Equal("piece-42", item.ExternalId);
        Assert.Equal(list.PublicId, item.WishlistId);
        Assert.Equal(list.Name, item.WishlistName);
        Assert.Equal($"https://wish.example.test/wishlists/{list.PublicId}", item.OpenWishUrl);

        var ownerView = Assert.Single(await browser.GetItemsAsync(list.PublicId));
        Assert.Equal("Cashmere crewneck", ownerView.Name);
        Assert.Equal("Oatmeal, size S", ownerView.Description);
        Assert.Equal("https://shop.example.com/crewneck", ownerView.Url);
        Assert.Equal("https://shop.example.com/crewneck.jpg", ownerView.Image);
        Assert.Equal(129.00m, ownerView.Price);
        Assert.Equal("Example Shop", ownerView.WhereToBuy);
        Assert.Equal(1, ownerView.Priority);
        Assert.Equal("Studio Notebook", ownerView.SourceAppName);

        // Edit it; omitting priority keeps the owner's ranking signal.
        var updated = await api.PutAsJsonAsync("items/external/piece-42", new IntegrationItemRequest
        {
            Name = "Cashmere crewneck in navy",
            Url = "https://shop.example.com/crewneck-navy",
            Price = 119.50m
        }, _json);
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
        Assert.Equal(item.Id, (await updated.Content.ReadFromJsonAsync<IntegrationItemModel>(_json))!.Id);

        ownerView = Assert.Single(await browser.GetItemsAsync(list.PublicId));
        Assert.Equal("Cashmere crewneck in navy", ownerView.Name);
        Assert.Null(ownerView.Description);
        Assert.Equal("https://shop.example.com/crewneck-navy", ownerView.Url);
        Assert.Equal(119.50m, ownerView.Price);
        Assert.Equal(1, ownerView.Priority);

        // A plain POST lands on the default list.
        var posted = await api.PostAsJsonAsync("items", new IntegrationItemRequest { Name = "Silk scarf", Priority = 3 }, _json);
        Assert.Equal(HttpStatusCode.Created, posted.StatusCode);
        var scarf = (await posted.Content.ReadFromJsonAsync<IntegrationItemModel>(_json))!;
        Assert.Equal(list.PublicId, scarf.WishlistId);

        var appItems = (await api.GetFromJsonAsync<List<IntegrationItemModel>>($"items?wishlistId={list.PublicId}", _json))!;
        Assert.Equal(["Cashmere crewneck in navy", "Silk scarf"], appItems.Select(i => i.Name));
        var byExternalId = (await api.GetFromJsonAsync<List<IntegrationItemModel>>("items?externalId=piece-42", _json))!;
        Assert.Equal(item.Id, Assert.Single(byExternalId).Id);
        Assert.Equal("Silk scarf", (await api.GetFromJsonAsync<IntegrationItemModel>($"items/{scarf.Id}", _json))!.Name);

        // Remove both, by external id and by OpenWish id.
        Assert.Equal(HttpStatusCode.NoContent, (await api.DeleteAsync("items/external/piece-42")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await api.DeleteAsync($"items/{scarf.Id}")).StatusCode);
        Assert.Empty(await browser.GetItemsAsync(list.PublicId));
        Assert.Empty((await api.GetFromJsonAsync<List<IntegrationItemModel>>($"items?wishlistId={list.PublicId}", _json))!);
        Assert.Equal(HttpStatusCode.NotFound, (await api.DeleteAsync("items/external/piece-42")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await api.GetAsync($"items/{scarf.Id}")).StatusCode);
    }

    [Fact]
    public async Task ConnectedApp_NeverSeesReservations_OrTouchesIdeasItDidNotAdd()
    {
        using var owner = await SignInAsync("owner");
        using var friend = await SignInAsync("friend");
        var list = await owner.CreateWishlistAsync(Unique("Birthday"));
        var ownIdea = await owner.AddOwnItemAsync(list.PublicId, new WishlistItemModel { Name = "Owner's own idea" });
        var app = factory.CreateConnectedApp(OpenWishAppFactory.Studio);
        await app.ConnectAsync(owner, [list.PublicId]);
        using var api = app.Api();

        var created = await api.PostAsJsonAsync("items", new IntegrationItemRequest { Name = "Reading lamp", ExternalId = "lamp" }, _json);
        var lamp = (await created.Content.ReadFromJsonAsync<IntegrationItemModel>(_json))!;

        // A friend reserves the app's idea: a surprise the owner and their apps must never learn about.
        var friendId = await ReserveAsync(lamp.Id, "playwright-friend@openwish.local");

        var raw = await api.GetStringAsync($"items/{lamp.Id}");
        Assert.DoesNotContain("reserv", raw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(friendId, raw, StringComparison.Ordinal);
        var listRaw = await api.GetStringAsync("items");
        Assert.DoesNotContain("reserv", listRaw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Owner's own idea", listRaw, StringComparison.Ordinal);

        Assert.Equal(HttpStatusCode.NotFound, (await api.GetAsync($"items/{ownIdea.PublicId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await api.PutAsJsonAsync($"items/{ownIdea.PublicId}", new IntegrationItemRequest { Name = "Hijacked" }, _json)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await api.DeleteAsync($"items/{ownIdea.PublicId}")).StatusCode);

        var ownerItems = await owner.GetItemsAsync(list.PublicId);
        Assert.Contains(ownerItems, i => i.Name == "Owner's own idea" && i.SourceAppName is null);
        Assert.Contains(ownerItems, i => i.Name == "Reading lamp" && i.SourceAppName == "Studio Notebook");
    }

    [Fact]
    public async Task Provenance_IsServerOwned_AndSurvivesOwnerEdits()
    {
        using var owner = await SignInAsync("owner");
        var list = await owner.CreateWishlistAsync(Unique("Provenance"));

        var spoofed = await owner.AddOwnItemAsync(list.PublicId, new WishlistItemModel { Name = "Typed by hand", SourceAppName = "Studio Notebook" });
        Assert.Null(spoofed.SourceAppName);

        var app = factory.CreateConnectedApp(OpenWishAppFactory.Studio);
        await app.ConnectAsync(owner, [list.PublicId]);
        using var api = app.Api();
        await api.PostAsJsonAsync("items", new IntegrationItemRequest { Name = "Wool coat" }, _json);

        var coat = (await owner.GetItemsAsync(list.PublicId)).Single(i => i.Name == "Wool coat");
        coat.Name = "Wool coat, charcoal";
        coat.IsPrivate = true;
        coat.SourceAppName = null;
        await EditAsOwnerAsync(list.PublicId, coat);

        var items = await owner.GetItemsAsync(list.PublicId);
        Assert.Null(items.Single(i => i.Name == "Typed by hand").SourceAppName);
        var edited = items.Single(i => i.Name == "Wool coat, charcoal");
        Assert.Equal("Studio Notebook", edited.SourceAppName);
        Assert.True(edited.IsPrivate);
    }

    [Fact]
    public async Task AppTokens_AndBrowserCookies_DoNotCrossOver()
    {
        using var owner = await SignInAsync("owner");
        var list = await owner.CreateWishlistAsync(Unique("Boundaries"));
        var app = factory.CreateConnectedApp(OpenWishAppFactory.Studio);
        await app.ConnectAsync(owner, [list.PublicId]);

        // The app token is not a session: the browser API ignores it.
        using var tokenOnly = app.Api();
        var browserApi = await tokenOnly.GetAsync(new Uri("https://localhost/api/wishlists"));
        Assert.NotEqual(HttpStatusCode.OK, browserApi.StatusCode);

        // A signed-in browser cannot call the integration API with its cookie.
        var cookieCall = await owner.Client.GetAsync("api/integrations/v1/me");
        Assert.Equal(HttpStatusCode.Unauthorized, cookieCall.StatusCode);
        Assert.Contains("Bearer", cookieCall.Headers.WwwAuthenticate.ToString());

        using var bogus = app.Api("owi_not-a-real-token");
        var rejected = await bogus.GetAsync("me");
        Assert.Equal(HttpStatusCode.Unauthorized, rejected.StatusCode);
        Assert.Contains("invalid_token", rejected.Headers.WwwAuthenticate.ToString());
    }

    [Fact]
    public async Task DisconnectingInSettings_RevokesAccess_AndKeepsIdeas()
    {
        using var owner = await SignInAsync("owner");
        var list = await owner.CreateWishlistAsync(Unique("Keepers"));
        var app = factory.CreateConnectedApp(OpenWishAppFactory.Studio);
        await app.ConnectAsync(owner, [list.PublicId]);
        using var api = app.Api();
        await api.PostAsJsonAsync("items", new IntegrationItemRequest { Name = "Linen robe" }, _json);

        var settings = await owner.GetPageAsync("Account/Manage/ConnectedApps");
        Assert.Contains("Studio Notebook", settings);
        Assert.Contains(list.Name, settings);
        Assert.Contains("Send pieces from your notebook to OpenWish.", settings);
        Assert.Contains("Pantry Planner", settings);

        await owner.DisconnectFromSettingsAsync(app.ConnectionId!);

        var revoked = await api.GetAsync("me");
        Assert.Equal(HttpStatusCode.Unauthorized, revoked.StatusCode);
        Assert.Contains("invalid_token", revoked.Headers.WwwAuthenticate.ToString());
        var robe = Assert.Single(await owner.GetItemsAsync(list.PublicId));
        Assert.Equal("Studio Notebook", robe.SourceAppName);
        Assert.DoesNotContain($"connection-{app.ConnectionId}", await owner.GetPageAsync("Account/Manage/ConnectedApps"));
    }

    [Fact]
    public async Task AppRevoke_ThenReconnect_KeepsManagingIdeasItAdded()
    {
        using var owner = await SignInAsync("owner");
        var list = await owner.CreateWishlistAsync(Unique("Reconnect"));
        var app = factory.CreateConnectedApp(OpenWishAppFactory.Studio);
        await app.ConnectAsync(owner, [list.PublicId]);
        var firstToken = app.AccessToken!;
        using (var api = app.Api())
        {
            await api.PutAsJsonAsync("items/external/tote", new IntegrationItemRequest { Name = "Leather tote" }, _json);
        }

        Assert.Equal(HttpStatusCode.OK, (await app.RevokeAsync()).StatusCode);
        using (var revoked = app.Api(firstToken))
        {
            Assert.Equal(HttpStatusCode.Unauthorized, (await revoked.GetAsync("me")).StatusCode);
        }

        var consent = WebUtility.HtmlDecode(await owner.GetPageAsync(app.BuildAuthorizeUrl()));
        Assert.Contains("Connect Studio Notebook", consent);
        Assert.Contains(list.Name, consent);
        Assert.Contains("can't see who reserved or bought anything", consent);
        Assert.DoesNotContain("already connected", consent);

        await app.ConnectAsync(owner, [list.PublicId]);
        Assert.NotEqual(firstToken, app.AccessToken);
        using var reconnected = app.Api();
        var tote = Assert.Single((await reconnected.GetFromJsonAsync<List<IntegrationItemModel>>("items?externalId=tote", _json))!);
        var update = await reconnected.PutAsJsonAsync("items/external/tote", new IntegrationItemRequest { Name = "Leather tote, cognac" }, _json);
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        Assert.Equal(tote.Id, (await update.Content.ReadFromJsonAsync<IntegrationItemModel>(_json))!.Id);
        Assert.Equal("Leather tote, cognac", Assert.Single(await owner.GetItemsAsync(list.PublicId)).Name);
    }

    [Fact]
    public async Task AuthorizationCodes_AreSingleUse_AndReplayRevokesTheToken()
    {
        using var owner = await SignInAsync("owner");
        var list = await owner.CreateWishlistAsync(Unique("Replay"));
        var app = factory.CreateConnectedApp(OpenWishAppFactory.Studio);
        var code = app.ReadCallback(await owner.ApproveAsync(app.BuildAuthorizeUrl(), [list.PublicId]));

        await app.ExchangeAsync(code);
        using var api = app.Api();
        Assert.Equal(HttpStatusCode.OK, (await api.GetAsync("me")).StatusCode);

        var replay = await app.RequestTokenAsync(code);
        Assert.Equal(HttpStatusCode.BadRequest, replay.StatusCode);
        Assert.Equal("invalid_grant", (await replay.Content.ReadFromJsonAsync<IntegrationErrorModel>())!.Error);
        Assert.Equal(HttpStatusCode.Unauthorized, (await api.GetAsync("me")).StatusCode);
    }

    [Fact]
    public async Task TokenExchange_RequiresTheRightClient_RedirectAndVerifier()
    {
        using var owner = await SignInAsync("owner");
        var list = await owner.CreateWishlistAsync(Unique("Exchange"));
        var app = factory.CreateConnectedApp(OpenWishAppFactory.Studio);
        var code = app.ReadCallback(await owner.ApproveAsync(app.BuildAuthorizeUrl(), [list.PublicId]));

        var wrongSecret = await app.RequestTokenAsync(code, clientSecret: "studio-test-secret-but-not-the-right-one");
        Assert.Equal(HttpStatusCode.Unauthorized, wrongSecret.StatusCode);
        Assert.Equal("invalid_client", (await wrongSecret.Content.ReadFromJsonAsync<IntegrationErrorModel>())!.Error);

        var wrongRedirect = await app.RequestTokenAsync(code, redirectUri: "https://studio.example.test/elsewhere");
        Assert.Equal("invalid_grant", (await wrongRedirect.Content.ReadFromJsonAsync<IntegrationErrorModel>())!.Error);

        var wrongVerifier = await app.RequestTokenAsync(code, codeVerifier: new string('x', 64));
        Assert.Equal("invalid_grant", (await wrongVerifier.Content.ReadFromJsonAsync<IntegrationErrorModel>())!.Error);

        // Failed attempts don't burn the code, and HTTP Basic client authentication works too.
        await app.ExchangeAsync(code, useBasicAuth: true);
    }

    [Fact]
    public async Task AuthorizeEndpoint_RefusesUnknownApps_AndReportsProtocolErrorsToTheApp()
    {
        using var owner = await SignInAsync("owner");
        var app = factory.CreateConnectedApp(OpenWishAppFactory.Studio);

        var unknownClient = WebUtility.HtmlDecode(await owner.GetPageAsync("oauth/authorize?response_type=code&client_id=nope&redirect_uri=https%3A%2F%2Fevil.example%2Fcb&state=x"));
        Assert.Contains("isn't registered", unknownClient);

        var unregisteredRedirect = await owner.Client.GetAsync(app.BuildAuthorizeUrl(redirectUri: "https://evil.example/callback"));
        Assert.Equal(HttpStatusCode.OK, unregisteredRedirect.StatusCode);
        Assert.Contains("doesn't recognize", WebUtility.HtmlDecode(await unregisteredRedirect.Content.ReadAsStringAsync()));

        var badResponseType = await owner.Client.GetAsync(app.BuildAuthorizeUrl(responseType: "token"));
        Assert.Equal(HttpStatusCode.Redirect, badResponseType.StatusCode);
        var error = QueryHelpers.ParseQuery(badResponseType.Headers.Location!.Query);
        Assert.Equal("unsupported_response_type", error["error"].ToString());

        var denied = await owner.DenyAsync(app.BuildAuthorizeUrl());
        Assert.StartsWith(OpenWishAppFactory.Studio.RedirectUri, denied.ToString());
        Assert.Equal("access_denied", QueryHelpers.ParseQuery(denied.Query)["error"].ToString());

        using var anonymous = factory.CreateBrowser();
        var signIn = await anonymous.Client.GetAsync(app.BuildAuthorizeUrl());
        Assert.Equal(HttpStatusCode.Redirect, signIn.StatusCode);
        Assert.Contains("/Account/Login", signIn.Headers.Location!.ToString());
        Assert.Contains(Uri.EscapeDataString("oauth/authorize?"), signIn.Headers.Location!.ToString());
    }

    [Fact]
    public async Task Apps_OnlyUseTheListsThePersonAllows()
    {
        using var owner = await SignInAsync("owner");
        var gifts = await owner.CreateWishlistAsync(Unique("Allowed"));
        var secret = await owner.CreateWishlistAsync(Unique("Not shared"));
        var app = factory.CreateConnectedApp(OpenWishAppFactory.Studio);
        await app.ConnectAsync(owner, [gifts.PublicId]);
        using var api = app.Api();

        var forbidden = await api.PostAsJsonAsync("items", new IntegrationItemRequest { Name = "Sneaky", WishlistId = secret.PublicId }, _json);
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
        Assert.Equal("wishlist_not_allowed", (await forbidden.Content.ReadFromJsonAsync<IntegrationErrorModel>(_json))!.Error);
        Assert.Empty(await owner.GetItemsAsync(secret.PublicId));
        Assert.DoesNotContain(secret.Name, await api.GetStringAsync("wishlists"));

        // Allow the second list from settings and make it the default.
        await owner.SaveConnectedListsAsync(app.ConnectionId!, [gifts.PublicId, secret.PublicId], secret.PublicId);
        var lists = (await api.GetFromJsonAsync<List<IntegrationWishlistModel>>("wishlists", _json))!;
        Assert.Equal(2, lists.Count);
        Assert.Equal(secret.PublicId, lists.Single(w => w.IsDefault).Id);

        var landed = await api.PostAsJsonAsync("items", new IntegrationItemRequest { Name = "Moves around" }, _json);
        var moving = (await landed.Content.ReadFromJsonAsync<IntegrationItemModel>(_json))!;
        Assert.Equal(secret.PublicId, moving.WishlistId);

        var moved = await api.PutAsJsonAsync($"items/{moving.Id}", new IntegrationItemRequest { Name = "Moves around", WishlistId = gifts.PublicId }, _json);
        Assert.Equal(HttpStatusCode.OK, moved.StatusCode);
        Assert.Equal(gifts.PublicId, (await moved.Content.ReadFromJsonAsync<IntegrationItemModel>(_json))!.WishlistId);
        Assert.Single(await owner.GetItemsAsync(gifts.PublicId));
        Assert.Empty(await owner.GetItemsAsync(secret.PublicId));

        // Deleting the default list sends new ideas to the remaining allowed list.
        Assert.Equal(HttpStatusCode.NoContent, (await owner.Client.DeleteAsync($"api/wishlists/{secret.PublicId}")).StatusCode);
        var fallback = await api.PostAsJsonAsync("items", new IntegrationItemRequest { Name = "Lands somewhere sensible" }, _json);
        Assert.Equal(HttpStatusCode.Created, fallback.StatusCode);
        Assert.Equal(gifts.PublicId, (await fallback.Content.ReadFromJsonAsync<IntegrationItemModel>(_json))!.WishlistId);
        Assert.True(Assert.Single((await api.GetFromJsonAsync<List<IntegrationWishlistModel>>("wishlists", _json))!).IsDefault);
    }

    [Fact]
    public async Task DeletingAList_FreesTheAppsExternalIds_ForReuse()
    {
        using var owner = await SignInAsync("owner");
        var first = await owner.CreateWishlistAsync(Unique("Deleted later"));
        var second = await owner.CreateWishlistAsync(Unique("Still here"));
        var app = factory.CreateConnectedApp(OpenWishAppFactory.Studio);
        await app.ConnectAsync(owner, [first.PublicId, second.PublicId]);
        using var api = app.Api();

        var original = await api.PutAsJsonAsync("items/external/floor-lamp", new IntegrationItemRequest { Name = "Lamp", WishlistId = first.PublicId }, _json);
        Assert.Equal(HttpStatusCode.Created, original.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await owner.Client.DeleteAsync($"api/wishlists/{first.PublicId}")).StatusCode);

        var readded = await api.PutAsJsonAsync("items/external/floor-lamp", new IntegrationItemRequest { Name = "Lamp" }, _json);
        Assert.Equal(HttpStatusCode.Created, readded.StatusCode);
        var lamp = (await readded.Content.ReadFromJsonAsync<IntegrationItemModel>(_json))!;
        Assert.Equal(second.PublicId, lamp.WishlistId);
        Assert.Equal("Lamp", Assert.Single(await owner.GetItemsAsync(second.PublicId)).Name);

        var duplicate = await api.PostAsJsonAsync("items", new IntegrationItemRequest { Name = "Lamp", ExternalId = "floor-lamp" }, _json);
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        Assert.Equal(lamp.Id, (await duplicate.Content.ReadFromJsonAsync<IntegrationErrorModel>(_json))!.ItemId);
    }

    [Fact]
    public async Task ConcurrentUpserts_ForTheSameExternalId_LeaveOneIdea()
    {
        using var owner = await SignInAsync("owner");
        var list = await owner.CreateWishlistAsync(Unique("Race"));
        var app = factory.CreateConnectedApp(OpenWishAppFactory.Studio);
        await app.ConnectAsync(owner, [list.PublicId]);

        var responses = await Task.WhenAll(Enumerable.Range(1, 4).Select(async attempt =>
        {
            using var api = app.Api();
            return await api.PutAsJsonAsync("items/external/race", new IntegrationItemRequest { Name = $"Attempt {attempt}" }, _json);
        }));

        Assert.All(responses, response => Assert.True(
            response.StatusCode is HttpStatusCode.Created or HttpStatusCode.OK,
            $"Unexpected status {(int)response.StatusCode}"));
        Assert.Single(await owner.GetItemsAsync(list.PublicId));
    }

    [Fact]
    public async Task Connections_AreIsolatedByPerson_AndByApp()
    {
        using var owner = await SignInAsync("owner");
        using var friend = await SignInAsync("friend");
        var ownerList = await owner.CreateWishlistAsync(Unique("Owner list"));
        var friendList = await friend.CreateWishlistAsync(Unique("Friend list"));

        var ownerStudio = factory.CreateConnectedApp(OpenWishAppFactory.Studio);
        await ownerStudio.ConnectAsync(owner, [ownerList.PublicId]);
        using var ownerApi = ownerStudio.Api();
        var ownerItem = (await (await ownerApi.PutAsJsonAsync("items/external/shared-id", new IntegrationItemRequest { Name = "Owner's piece" }, _json))
            .Content.ReadFromJsonAsync<IntegrationItemModel>(_json))!;

        var friendStudio = factory.CreateConnectedApp(OpenWishAppFactory.Studio);
        await friendStudio.ConnectAsync(friend, [friendList.PublicId]);
        using var friendApi = friendStudio.Api();
        Assert.Equal("JordanDemo", (await friendApi.GetFromJsonAsync<IntegrationProfileModel>("me", _json))!.User.DisplayName);
        Assert.Equal(HttpStatusCode.NotFound, (await friendApi.GetAsync($"items/{ownerItem.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await friendApi.DeleteAsync($"items/{ownerItem.Id}")).StatusCode);
        var friendUpsert = await friendApi.PutAsJsonAsync("items/external/shared-id", new IntegrationItemRequest { Name = "Friend's piece" }, _json);
        Assert.Equal(HttpStatusCode.Created, friendUpsert.StatusCode);

        var ownerPantry = factory.CreateConnectedApp(OpenWishAppFactory.Pantry);
        await ownerPantry.ConnectAsync(owner, [ownerList.PublicId]);
        using var pantryApi = ownerPantry.Api();
        Assert.Empty((await pantryApi.GetFromJsonAsync<List<IntegrationItemModel>>("items", _json))!);
        Assert.Equal(HttpStatusCode.NotFound, (await pantryApi.DeleteAsync($"items/{ownerItem.Id}")).StatusCode);

        var ownerItems = await owner.GetItemsAsync(ownerList.PublicId);
        Assert.Equal("Owner's piece", Assert.Single(ownerItems).Name);
        Assert.Equal("Friend's piece", Assert.Single(await friend.GetItemsAsync(friendList.PublicId)).Name);
    }

    [Fact]
    public async Task Consent_CanStartANewPrivateList_ThatBecomesTheDefault()
    {
        using var owner = await SignInAsync("owner");
        var listName = Unique("Holiday sweaters");
        var app = factory.CreateConnectedApp(OpenWishAppFactory.Studio);
        await app.ConnectAsync(owner, [], listName);
        using var api = app.Api();

        var created = (await owner.GetWishlistsAsync()).Single(w => w.Name == listName);
        Assert.True(created.IsPrivate);
        var me = (await api.GetFromJsonAsync<IntegrationProfileModel>("me", _json))!;
        Assert.Equal(created.PublicId, Assert.Single(me.Wishlists).Id);

        await api.PostAsJsonAsync("items", new IntegrationItemRequest { Name = "Fair Isle cardigan" }, _json);
        Assert.Equal("Fair Isle cardigan", Assert.Single(await owner.GetItemsAsync(created.PublicId)).Name);
    }

    [Fact]
    public async Task Api_RejectsInvalidIdeas()
    {
        using var owner = await SignInAsync("friend2");
        var list = await owner.CreateWishlistAsync(Unique("Validation"));
        var app = factory.CreateConnectedApp(OpenWishAppFactory.Studio);
        await app.ConnectAsync(owner, [list.PublicId]);
        using var api = app.Api();

        string[] invalidBodies =
        [
            """{"name":""}""",
            """{"name":"Bad link","url":"javascript:alert(1)"}""",
            """{"name":"Bad image","imageUrl":"data:image/png;base64,AAAA"}""",
            """{"name":"Fractional cents","price":10.125}""",
            """{"name":"Too eager","priority":7}""",
            """{"name":"Blank id","externalId":"   "}"""
        ];

        foreach (var body in invalidBodies)
        {
            var response = await api.PostAsync("items", new StringContent(body, System.Text.Encoding.UTF8, "application/json"));
            Assert.True(response.StatusCode == HttpStatusCode.BadRequest, $"Expected 400 for {body} but got {(int)response.StatusCode}.");
        }

        Assert.Empty(await owner.GetItemsAsync(list.PublicId));
    }

    [Fact]
    public async Task Discovery_DescribesTheEndpoints()
    {
        using var client = factory.CreateClient();
        var metadata = (await client.GetFromJsonAsync<Dictionary<string, System.Text.Json.JsonElement>>(".well-known/oauth-authorization-server"))!;
        Assert.Equal("https://wish.example.test/oauth/authorize", metadata["authorization_endpoint"].GetString());
        Assert.Equal("https://wish.example.test/oauth/token", metadata["token_endpoint"].GetString());
        Assert.Equal("https://wish.example.test/api/integrations/v1/", metadata["openwish_api_base"].GetString());
        Assert.Contains("S256", metadata["code_challenge_methods_supported"].EnumerateArray().Select(e => e.GetString()));
    }

    private async Task<OpenWishBrowser> SignInAsync(string persona)
    {
        var browser = factory.CreateBrowser();
        await browser.SignInAsync(persona);
        return browser;
    }

    /// <summary>Saves the owner's edit dialog through the same service the interactive page uses.</summary>
    private async Task EditAsOwnerAsync(string wishlistPublicId, WishlistItemModel item)
    {
        using var scope = factory.Services.CreateScope();
        await using (var context = await scope.ServiceProvider
            .GetRequiredService<IDbContextFactory<ApplicationDbContext>>()
            .CreateDbContextAsync())
        {
            item.Id = await context.WishlistItems
                .Where(i => i.PublicId == item.PublicId)
                .Select(i => i.Id)
                .SingleAsync();
        }

        await scope.ServiceProvider
            .GetRequiredService<OpenWish.Shared.Services.IWishlistService>()
            .UpdateWishlistItemByPublicIdAsync(wishlistPublicId, item.Id, item);
    }

    private async Task<string> ReserveAsync(string itemPublicId, string reserverEmail)
    {
        using var scope = factory.Services.CreateScope();
        await using var context = await scope.ServiceProvider
            .GetRequiredService<IDbContextFactory<ApplicationDbContext>>()
            .CreateDbContextAsync();
        var reserver = await context.Users.SingleAsync(u => u.Email == reserverEmail);
        var item = await context.WishlistItems.SingleAsync(i => i.PublicId == itemPublicId);
        context.ItemReservations.Add(new ItemReservation { WishlistItemId = item.Id, UserId = reserver.Id });
        await context.SaveChangesAsync();
        return reserver.Id;
    }

    private static string Unique(string name) => $"{name} {Guid.NewGuid().ToString("N")[..6]}";
}