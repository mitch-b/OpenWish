using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OpenWish.Application.Integrations;
using OpenWish.Data;
using OpenWish.Data.Entities;
using OpenWish.Shared.Integrations;
using OpenWish.Shared.Models;
using Xunit;

namespace OpenWish.Web.Tests.Integrations;

[Collection(ConnectionLifecycleCollection.Name)]
public sealed class ConnectionLifecycleTests(OpenWishAppFactory factory)
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PendingConsent_CannotRestoreDisconnectedAccess(bool appRevokes)
    {
        using var owner = await SignInAsync();
        var list = await owner.CreateWishlistAsync(Unique("Disconnect"));
        var active = factory.CreateConnectedApp(OpenWishAppFactory.Studio);
        await active.ConnectAsync(owner, [list.PublicId]);
        var pending = factory.CreateConnectedApp(OpenWishAppFactory.Studio);
        var code = pending.ReadCallback(await owner.ApproveAsync(pending.BuildAuthorizeUrl(), [list.PublicId]));

        if (appRevokes)
        {
            Assert.Equal(HttpStatusCode.OK, (await active.RevokeAsync()).StatusCode);
        }
        else
        {
            await owner.DisconnectFromSettingsAsync(active.ConnectionId!);
        }

        using var oldApi = active.Api();
        Assert.Equal(HttpStatusCode.Unauthorized, (await oldApi.GetAsync("me")).StatusCode);
        var exchange = await pending.RequestTokenAsync(code);
        Assert.Equal(HttpStatusCode.BadRequest, exchange.StatusCode);
    }

    [Fact]
    public async Task PendingConsent_CannotRestoreRemovedList()
    {
        using var owner = await SignInAsync();
        var retained = await owner.CreateWishlistAsync(Unique("Retained"));
        var removed = await owner.CreateWishlistAsync(Unique("Removed"));
        var active = factory.CreateConnectedApp(OpenWishAppFactory.Studio);
        await active.ConnectAsync(owner, [retained.PublicId, removed.PublicId]);
        var pending = factory.CreateConnectedApp(OpenWishAppFactory.Studio);
        var code = pending.ReadCallback(await owner.ApproveAsync(
            pending.BuildAuthorizeUrl(), [retained.PublicId, removed.PublicId]));

        await owner.SaveConnectedListsAsync(active.ConnectionId!, [retained.PublicId], retained.PublicId);
        using (var before = active.Api())
        {
            Assert.DoesNotContain(removed.PublicId, await before.GetStringAsync("wishlists"));
        }

        var exchange = await pending.RequestTokenAsync(code);
        Assert.Equal(HttpStatusCode.BadRequest, exchange.StatusCode);
        Assert.Equal("invalid_grant", (await exchange.Content.ReadFromJsonAsync<IntegrationErrorModel>())!.Error);
    }

    [Fact]
    public async Task MigrationSnapshot_MatchesMergedModel()
    {
        using var scope = factory.Services.CreateScope();
        await using var context = await ContextAsync(scope);
        Assert.False(context.Database.HasPendingModelChanges());
    }

    [Fact]
    public async Task SelectingNewAndExistingLists_MakesNewPrivateListTheDefault()
    {
        using var owner = await SignInAsync();
        var existing = await owner.CreateWishlistAsync(Unique("Existing"));
        var newName = Unique("New private destination");
        var app = factory.CreateConnectedApp(OpenWishAppFactory.Studio);
        await app.ConnectAsync(owner, [existing.PublicId], newName);
        var created = (await owner.GetWishlistsAsync()).Single(w => w.Name == newName);
        Assert.True(created.IsPrivate);
        using var api = app.Api();
        var lists = (await api.GetFromJsonAsync<List<IntegrationWishlistModel>>("wishlists"))!;
        Assert.Equal(created.PublicId, lists.Single(w => w.IsDefault).Id);
    }

    [Fact]
    public async Task JsonLoadedAppItem_CanBeEditedByPublicId_WithoutChangingIdentityOrProvenance()
    {
        using var owner = await SignInAsync();
        var list = await owner.CreateWishlistAsync(Unique("JSON edit"));
        var otherList = await owner.CreateWishlistAsync(Unique("Wrong edit list"));
        await owner.AddOwnItemAsync(list.PublicId, new WishlistItemModel { Name = "Owner idea remains" });
        var app = factory.CreateConnectedApp(OpenWishAppFactory.Studio);
        await app.ConnectAsync(owner, [list.PublicId]);
        using var api = app.Api();
        var created = await api.PostAsJsonAsync("items", new IntegrationItemRequest { Name = "App idea" });
        created.EnsureSuccessStatusCode();
        var item = (await owner.GetItemsAsync(list.PublicId)).Single(i => i.SourceAppName is not null);
        Assert.Equal(0, item.Id);
        item.Name = "Edited without internal ID";
        item.SourceAppName = "Spoofed source";
        var path = $"api/wishlists/{list.PublicId}/items/by-public-id/{item.PublicId}";
        using var stranger = factory.CreateBrowser();
        await stranger.SignInAsync("friend");
        Assert.Equal(HttpStatusCode.Forbidden, (await stranger.Client.PutAsJsonAsync(path, item)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await owner.Client.PutAsJsonAsync(
            $"api/wishlists/{otherList.PublicId}/items/by-public-id/{item.PublicId}", item)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await api.PutAsJsonAsync(new Uri(owner.Client.BaseAddress!, path), item)).StatusCode);

        var response = await owner.Client.PutAsJsonAsync(path, item);
        response.EnsureSuccessStatusCode();
        var saved = (await response.Content.ReadFromJsonAsync<WishlistItemModel>())!;
        Assert.Equal(item.PublicId, saved.PublicId);
        Assert.Equal(item.Name, saved.Name);
        Assert.Equal(OpenWishAppFactory.Studio.DisplayName, saved.SourceAppName);
        var all = await owner.GetItemsAsync(list.PublicId);
        Assert.Equal(2, all.Count);
        Assert.Contains(all, value => value.Name == "Owner idea remains");
    }

    [Theory]
    [InlineData("wishlist")]
    [InlineData("friends")]
    public async Task MovingToPrivateList_DoesNotExposeLaterEditsThroughOldActivity(string feed)
    {
        using var owner = await SignInAsync();
        using var friend = factory.CreateBrowser();
        await friend.SignInAsync("friend");
        var shared = await owner.CreateWishlistAsync(Unique("Shared"));
        using var scope = factory.Services.CreateScope();
        await using var context = await ContextAsync(scope);
        await context.Wishlists.Where(w => w.PublicId == shared.PublicId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(w => w.IsPrivate, false));
        var ownerId = await context.Wishlists.Where(w => w.PublicId == shared.PublicId).Select(w => w.OwnerId).SingleAsync();
        var friendId = await context.Users.Where(u => u.UserName == "JordanDemo").Select(u => u.Id).SingleAsync();
        if (!await context.Friends.AnyAsync(f => f.UserId == ownerId && f.FriendUserId == friendId && !f.Deleted))
        {
            context.Friends.Add(new Friend { UserId = ownerId, FriendUserId = friendId });
            await context.SaveChangesAsync();
        }
        if (!await context.Friends.AnyAsync(f => f.UserId == friendId && f.FriendUserId == ownerId && !f.Deleted))
        {
            context.Friends.Add(new Friend { UserId = friendId, FriendUserId = ownerId });
            await context.SaveChangesAsync();
        }

        var secret = await owner.CreateWishlistAsync(Unique("Private"));
        var app = factory.CreateConnectedApp(OpenWishAppFactory.Studio);
        await app.ConnectAsync(owner, [shared.PublicId, secret.PublicId]);
        using var api = app.Api();
        var added = await api.PostAsJsonAsync("items", new IntegrationItemRequest
        {
            Name = "Public idea",
            WishlistId = shared.PublicId
        });
        added.EnsureSuccessStatusCode();
        var item = (await added.Content.ReadFromJsonAsync<IntegrationItemModel>())!;
        Assert.Single(await friend.GetItemsAsync(shared.PublicId));
        Assert.Equal(HttpStatusCode.Forbidden,
            (await friend.Client.GetAsync($"api/wishlists/{secret.PublicId}/items")).StatusCode);

        var moved = await api.PutAsJsonAsync($"items/{item.Id}", new IntegrationItemRequest
        {
            Name = "Moved idea",
            WishlistId = secret.PublicId
        });
        moved.EnsureSuccessStatusCode();
        var privateName = Unique("Private edit must not leak");
        var edited = await api.PutAsJsonAsync($"items/{item.Id}", new IntegrationItemRequest
        {
            Name = privateName,
            Description = "Details intended only for the private list"
        });
        edited.EnsureSuccessStatusCode();
        Assert.Empty(await friend.GetItemsAsync(shared.PublicId));

        var sharedId = await context.Wishlists.Where(w => w.PublicId == shared.PublicId).Select(w => w.Id).SingleAsync();
        var path = feed == "wishlist" ? $"api/activities/wishlist/{sharedId}" : "api/activities/friends?count=100";
        var response = await friend.Client.GetAsync(path);
        response.EnsureSuccessStatusCode();
        Assert.DoesNotContain(privateName, await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task ExpiredAuthorizationCode_IsRejected()
    {
        using var owner = await SignInAsync();
        var list = await owner.CreateWishlistAsync(Unique("Expired"));
        var app = factory.CreateConnectedApp(OpenWishAppFactory.Studio);
        var code = app.ReadCallback(await owner.ApproveAsync(app.BuildAuthorizeUrl(), [list.PublicId]));
        using var scope = factory.Services.CreateScope();
        await using var context = await ContextAsync(scope);
        var grant = await context.IntegrationAuthorizationCodes.SingleAsync(c =>
            c.CodeHash == Hash(code));
        grant.ExpiresOn = DateTimeOffset.UtcNow.AddMinutes(-1);
        await context.SaveChangesAsync();
        var response = await app.RequestTokenAsync(code);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("invalid_grant", (await response.Content.ReadFromJsonAsync<IntegrationErrorModel>())!.Error);
    }

    [Fact]
    public async Task ConfidentialClient_WithoutPkce_CanUseDocumentedFlow()
    {
        using var owner = await SignInAsync();
        var list = await owner.CreateWishlistAsync(Unique("No PKCE"));
        var app = factory.CreateConnectedApp(OpenWishAppFactory.Studio);
        var code = app.ReadCallback(await owner.ApproveAsync(app.BuildAuthorizeUrl(usePkce: false), [list.PublicId]));
        await app.ExchangeAsync(code);
        using var api = app.Api();
        Assert.Equal(HttpStatusCode.OK, (await api.GetAsync("me")).StatusCode);
    }

    [Fact]
    public async Task ConsentPost_WithoutAntiforgery_IsRejected()
    {
        using var owner = await SignInAsync();
        var app = factory.CreateConnectedApp(OpenWishAppFactory.Studio);
        var url = app.BuildAuthorizeUrl();
        await owner.GetPageAsync(url);
        var response = await owner.Client.PostAsync(url, new FormUrlEncodedContent(
            new Dictionary<string, string> { ["_handler"] = "consent-approve" }));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ApiBurst_IsLimitedPerConnection_NotAcrossApps()
    {
        await using var isolated = new OpenWishAppFactory();
        await isolated.InitializeAsync();
        using var owner = isolated.CreateBrowser();
        await owner.SignInAsync("owner");
        var list = await owner.CreateWishlistAsync(Unique("Rate limit"));
        var app = isolated.CreateConnectedApp(OpenWishAppFactory.Studio);
        var other = isolated.CreateConnectedApp(OpenWishAppFactory.Pantry);
        await app.ConnectAsync(owner, [list.PublicId]);
        await other.ConnectAsync(owner, [list.PublicId]);
        using var api = app.Api();
        var statuses = new List<HttpStatusCode>();
        for (var i = 0; i < 140; i++)
        {
            using var response = await api.GetAsync("me");
            statuses.Add(response.StatusCode);
        }

        Assert.Contains(HttpStatusCode.TooManyRequests, statuses);
        using var otherApi = other.Api();
        Assert.Equal(HttpStatusCode.OK, (await otherApi.GetAsync("me")).StatusCode);
    }

    [Fact]
    public async Task ConcurrentReplay_RevokesTheWinningToken()
    {
        using var scope = factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IIntegrationConnectionService>();
        var app = scope.ServiceProvider.GetRequiredService<IntegrationAppRegistry>().Find("studio")!;
        var grant = await NewGrantAsync(scope, service, app);
        await using var locker = await ContextAsync(scope);
        var userId = await locker.IntegrationAuthorizationCodes.Where(c => c.CodeHash == Hash(grant))
            .Select(c => c.UserId).SingleAsync();
        await using var transaction = await locker.Database.BeginTransactionAsync();
        await HoldConnectionLockAsync(locker, userId, app.ClientId);
        var first = service.ExchangeCodeAsync(app, grant, app.RedirectUris[0], null);
        var second = service.ExchangeCodeAsync(app, grant, app.RedirectUris[0], null);
        await WaitForBlockedQueriesAsync(scope, 2);
        await transaction.CommitAsync();
        var results = await Task.WhenAll(first, second);
        var winner = Assert.Single(results, r => r.AccessToken is not null);
        Assert.Single(results, r => r.Error == "invalid_grant");
        Assert.Null(await service.AuthenticateAsync(winner.AccessToken));
    }

    [Fact]
    public async Task ConcurrentFirstConnections_DoNotThrowAfterConsumingCodes()
    {
        using var scope = factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IIntegrationConnectionService>();
        var app = scope.ServiceProvider.GetRequiredService<IntegrationAppRegistry>().Find("studio")!;
        var firstCode = await NewGrantAsync(scope, service, app);
        await using var context = await ContextAsync(scope);
        var firstGrant = await context.IntegrationAuthorizationCodes.SingleAsync(c => c.CodeHash == Hash(firstCode));
        var listId = int.Parse(firstGrant.GrantedWishlistIds);
        var listPublicId = await context.Wishlists.Where(w => w.Id == listId).Select(w => w.PublicId).SingleAsync();
        var secondGrant = await service.ApproveAsync(firstGrant.UserId, new AuthorizationApproval(
            app.ClientId, app.RedirectUris[0], null, [listPublicId], listPublicId, null));
        await using var transaction = await context.Database.BeginTransactionAsync();
        await HoldConnectionLockAsync(context, firstGrant.UserId, app.ClientId);
        var first = service.ExchangeCodeAsync(app, firstCode, app.RedirectUris[0], null);
        var second = service.ExchangeCodeAsync(app, secondGrant.Code, app.RedirectUris[0], null);
        await WaitForBlockedQueriesAsync(scope, 2);
        await transaction.CommitAsync();
        var results = await Task.WhenAll(first, second);
        Assert.All(results, result => Assert.NotNull(result.AccessToken));
        var callers = await Task.WhenAll(results.Select(r => service.AuthenticateAsync(r.AccessToken)));
        Assert.Single(callers, caller => caller is not null);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task InFlightExchange_CannotUndoDisconnect_RegardlessOfLockOrder(bool disconnectFirst)
    {
        using var scope = factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IIntegrationConnectionService>();
        var app = scope.ServiceProvider.GetRequiredService<IntegrationAppRegistry>().Find("studio")!;
        var firstCode = await NewGrantAsync(scope, service, app);
        var initial = await service.ExchangeCodeAsync(app, firstCode, app.RedirectUris[0], null);
        var caller = (await service.AuthenticateAsync(initial.AccessToken))!;
        await using var locker = await ContextAsync(scope);
        var list = await locker.Wishlists.SingleAsync(w => w.OwnerId == caller.UserId);
        var pending = await service.ApproveAsync(caller.UserId, new AuthorizationApproval(
            app.ClientId, app.RedirectUris[0], null, [list.PublicId], list.PublicId, null));
        await using var transaction = await locker.Database.BeginTransactionAsync();
        await HoldConnectionLockAsync(locker, caller.UserId, app.ClientId);
        Task<bool> disconnect;
        Task<TokenExchangeResult> exchange;
        if (disconnectFirst)
        {
            disconnect = service.DisconnectAsync(caller.UserId, caller.ConnectionPublicId);
            await WaitForBlockedQueriesAsync(scope, 1);
            exchange = service.ExchangeCodeAsync(app, pending.Code, app.RedirectUris[0], null);
        }
        else
        {
            exchange = service.ExchangeCodeAsync(app, pending.Code, app.RedirectUris[0], null);
            await WaitForBlockedQueriesAsync(scope, 1);
            disconnect = service.DisconnectAsync(caller.UserId, caller.ConnectionPublicId);
        }

        await WaitForBlockedQueriesAsync(scope, 2);
        await transaction.CommitAsync();
        Assert.True(await disconnect);
        var result = await exchange;
        Assert.Null(await service.AuthenticateAsync(initial.AccessToken));
        if (disconnectFirst)
        {
            Assert.Equal("invalid_grant", result.Error);
        }
        else
        {
            Assert.NotNull(result.AccessToken);
            Assert.Null(await service.AuthenticateAsync(result.AccessToken));
        }
    }

    [Fact]
    public async Task WrongVerifierOnUsedCode_DoesNotRevokeValidConnection()
    {
        using var owner = await SignInAsync();
        var list = await owner.CreateWishlistAsync(Unique("Wrong replay verifier"));
        var app = factory.CreateConnectedApp(OpenWishAppFactory.Studio);
        var code = app.ReadCallback(await owner.ApproveAsync(app.BuildAuthorizeUrl(), [list.PublicId]));
        await app.ExchangeAsync(code);
        var invalid = await app.RequestTokenAsync(code, codeVerifier: new string('x', 64));
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        using var api = app.Api();
        Assert.Equal(HttpStatusCode.OK, (await api.GetAsync("me")).StatusCode);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task InFlightExchange_CannotUndoListRemoval_RegardlessOfLockOrder(bool saveFirst)
    {
        using var scope = factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IIntegrationConnectionService>();
        var app = scope.ServiceProvider.GetRequiredService<IntegrationAppRegistry>().Find("studio")!;
        var firstCode = await NewGrantAsync(scope, service, app);
        var initial = await service.ExchangeCodeAsync(app, firstCode, app.RedirectUris[0], null);
        var caller = (await service.AuthenticateAsync(initial.AccessToken))!;
        await using var locker = await ContextAsync(scope);
        var retained = await locker.Wishlists.SingleAsync(w => w.OwnerId == caller.UserId);
        var removed = new Wishlist { Name = Unique("Removed by settings"), OwnerId = caller.UserId, IsPrivate = true };
        locker.Wishlists.Add(removed);
        await locker.SaveChangesAsync();
        var pending = await service.ApproveAsync(caller.UserId, new AuthorizationApproval(
            app.ClientId, app.RedirectUris[0], null, [retained.PublicId, removed.PublicId], retained.PublicId, null));
        await using var transaction = await locker.Database.BeginTransactionAsync();
        await HoldConnectionLockAsync(locker, caller.UserId, app.ClientId);
        Task<bool> save;
        Task<TokenExchangeResult> exchange;
        if (saveFirst)
        {
            save = service.UpdateAllowedWishlistsAsync(caller.UserId, caller.ConnectionPublicId, [retained.PublicId], retained.PublicId);
            await WaitForBlockedQueriesAsync(scope, 1);
            exchange = service.ExchangeCodeAsync(app, pending.Code, app.RedirectUris[0], null);
        }
        else
        {
            exchange = service.ExchangeCodeAsync(app, pending.Code, app.RedirectUris[0], null);
            await WaitForBlockedQueriesAsync(scope, 1);
            save = service.UpdateAllowedWishlistsAsync(caller.UserId, caller.ConnectionPublicId, [retained.PublicId], retained.PublicId);
        }

        await WaitForBlockedQueriesAsync(scope, 2);
        await transaction.CommitAsync();
        Assert.True(await save);
        var result = await exchange;
        if (saveFirst)
        {
            Assert.Equal("invalid_grant", result.Error);
        }
        else
        {
            Assert.NotNull(result.AccessToken);
        }

        var connections = await service.GetConnectedAppsAsync(caller.UserId);
        Assert.Equal(retained.PublicId, Assert.Single(Assert.Single(connections).Wishlists).PublicId);
    }

    private async Task<string> NewGrantAsync(IServiceScope scope, IIntegrationConnectionService service, IntegrationApp app)
    {
        await using var context = await ContextAsync(scope);
        var user = new ApplicationUser { UserName = Unique("review-user") };
        var list = new Wishlist { Name = Unique("Review list"), OwnerId = user.Id, Owner = user, IsPrivate = true };
        context.Wishlists.Add(list);
        await context.SaveChangesAsync();
        var grant = await service.ApproveAsync(user.Id, new AuthorizationApproval(
            app.ClientId, app.RedirectUris[0], null, [list.PublicId], list.PublicId, null));
        return Assert.IsType<string>(grant.Code);
    }

    private static Task HoldConnectionLockAsync(ApplicationDbContext context, string userId, string clientId)
    {
        var key = $"openwish-integration:{userId.Length}:{userId}:{clientId}";
        return context.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtextextended({key}, 0))");
    }

    private static async Task WaitForBlockedQueriesAsync(IServiceScope scope, int expected)
    {
        await using var observer = await ContextAsync(scope);
        for (var attempt = 0; attempt < 500; attempt++)
        {
            var blocked = await observer.Database.SqlQuery<int>($"""
                SELECT count(*)::int AS "Value" FROM pg_stat_activity
                WHERE datname = current_database() AND pid <> pg_backend_pid()
                  AND wait_event_type = 'Lock' AND query LIKE '%pg_advisory_xact_lock%'
                """).SingleAsync();
            if (blocked >= expected)
            {
                return;
            }

            await Task.Delay(20);
        }

        Assert.Fail($"Did not observe {expected} blocked connection mutations.");
    }

    private static Task<ApplicationDbContext> ContextAsync(IServiceScope scope) =>
        scope.ServiceProvider.GetRequiredService<IDbContextFactory<ApplicationDbContext>>().CreateDbContextAsync();

    private async Task<OpenWishBrowser> SignInAsync()
    {
        var browser = factory.CreateBrowser();
        await browser.SignInAsync("owner");
        return browser;
    }

    private static string Hash(string value) =>
        Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(value)));

    private static string Unique(string name) => $"{name} {Guid.NewGuid():N}";
}

[CollectionDefinition(Name)]
public sealed class ConnectionLifecycleCollection : ICollectionFixture<OpenWishAppFactory>
{
    public const string Name = "OpenWish lifecycle host";
}