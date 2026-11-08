using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol;
using ModelContextProtocol.Authentication;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using OpenWish.Data;
using OpenWish.Data.Entities;
using OpenWish.Shared.Integrations;
using OpenWish.Shared.Models;
using Xunit;

namespace OpenWish.Web.Tests.Integrations;

/// <summary>
/// Drives <c>/mcp</c> the way AI assistants do: discovery from the 401 challenge, dynamic client
/// registration, PKCE with an RFC 8707 resource, the person's consent, and rotating refresh tokens.
/// </summary>
[Collection(McpCollection.Name)]
public sealed class McpConnectionTests(OpenWishAppFactory factory)
{
    private const string BaseUri = "https://wish.example.test/";
    private const string Resource = BaseUri + "mcp";
    private const string LoopbackRedirect = "http://127.0.0.1:33418/callback";

    [Fact]
    public async Task LlmsTxt_IsPublicAndExplainsIntegrationDiscoveryAndConsent()
    {
        using var client = factory.CreateApiClient();
        using var response = await client.GetAsync("llms.txt");
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("/api/integrations/v1", content);
        Assert.Contains("OpenWishSettings__Integrations__Mcp__Enabled", content);
        Assert.Contains("browser sign-in and consent flow", content);
        Assert.Contains(".docs/INTEGRATIONS.md", content);
    }

    [Fact]
    public async Task UnauthenticatedRequest_ChallengesWithProtectedResourceMetadata()
    {
        using var client = factory.CreateApiClient();
        var response = await client.PostAsJsonAsync("mcp", new { jsonrpc = "2.0", id = 1, method = "ping" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var challenge = Assert.Single(response.Headers.WwwAuthenticate);
        Assert.Equal("Bearer", challenge.Scheme);
        Assert.Contains($"resource_metadata=\"{BaseUri}.well-known/oauth-protected-resource/mcp\"", challenge.Parameter);
        Assert.Contains($"scope=\"{IntegrationScopes.WishlistItems}\"", challenge.Parameter);
        Assert.DoesNotContain("invalid_token", challenge.Parameter);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "owk_not-a-real-token");
        var rejected = await client.PostAsJsonAsync("mcp", new { jsonrpc = "2.0", id = 1, method = "ping" });
        Assert.Equal(HttpStatusCode.Unauthorized, rejected.StatusCode);
        Assert.Contains("error=\"invalid_token\"", Assert.Single(rejected.Headers.WwwAuthenticate).Parameter);
    }

    [Fact]
    public async Task Metadata_AdvertisesRegistrationRefreshAndTheMcpResource()
    {
        using var client = factory.CreateApiClient();
        foreach (var path in new[] { ".well-known/oauth-protected-resource/mcp", ".well-known/oauth-protected-resource" })
        {
            using var resource = await JsonDocument.ParseAsync(await client.GetStreamAsync(path));
            Assert.Equal(Resource, resource.RootElement.GetProperty("resource").GetString());
            Assert.Equal(BaseUri, resource.RootElement.GetProperty("authorization_servers")[0].GetString());
            Assert.Contains(IntegrationScopes.WishlistItems, Strings(resource.RootElement.GetProperty("scopes_supported")));
        }

        using var server = await JsonDocument.ParseAsync(await client.GetStreamAsync(".well-known/oauth-authorization-server"));
        var root = server.RootElement;
        Assert.Equal(BaseUri, root.GetProperty("issuer").GetString());
        Assert.Equal(BaseUri + "oauth/register", root.GetProperty("registration_endpoint").GetString());
        Assert.Contains("refresh_token", Strings(root.GetProperty("grant_types_supported")));
        Assert.Contains("none", Strings(root.GetProperty("token_endpoint_auth_methods_supported")));
        Assert.Equal(["S256"], Strings(root.GetProperty("code_challenge_methods_supported")));
        Assert.True(root.GetProperty("authorization_response_iss_parameter_supported").GetBoolean());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AssistantConnection_UsesTheAccountAuthenticatedInTheBrowser(bool externalLogin)
    {
        const string password = "Test-password-123!";
        var email = externalLogin ? TestExternalLoginHandler.Email : $"{Guid.NewGuid():N}@example.test";
        string expectedUserId;
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true };
            var created = externalLogin ? await users.CreateAsync(user) : await users.CreateAsync(user, password);
            Assert.True(created.Succeeded, string.Join(", ", created.Errors.Select(e => e.Description)));
            if (externalLogin)
            {
                var linked = await users.AddLoginAsync(user, new UserLoginInfo(
                    TestExternalLoginHandler.SchemeName, TestExternalLoginHandler.ProviderKey, "Test external provider"));
                Assert.True(linked.Succeeded, string.Join(", ", linked.Errors.Select(e => e.Description)));
            }
            expectedUserId = user.Id;
        }

        using var other = await SignInAsync();
        var otherList = await other.CreateWishlistAsync(Unique("Other person list"));
        using var browser = factory.CreateBrowser();
        var client = await RegisterAsync();
        var flow = AuthorizationFlow.Start(client.ClientId, LoopbackRedirect);

        var challenge = await browser.Client.GetAsync(flow.AuthorizeUrl);
        Assert.Equal(HttpStatusCode.Redirect, challenge.StatusCode);
        var loginUrl = new Uri(browser.Client.BaseAddress!, challenge.Headers.Location!);
        Assert.Equal("/Account/Login", loginUrl.AbsolutePath);
        var returnUrl = QueryHelpers.ParseQuery(loginUrl.Query)["ReturnUrl"].ToString();
        Assert.Equal("/" + flow.AuthorizeUrl, returnUrl);

        var loginPage = await browser.GetPageAsync(loginUrl.PathAndQuery);
        HttpResponseMessage signIn;
        if (externalLogin)
        {
            Assert.Contains("Continue with Test external provider", loginPage);
            var externalReturnUrl = OpenWishBrowser.ExternalLoginReturnUrl(loginPage);
            Assert.Equal(returnUrl, externalReturnUrl);
            signIn = await browser.Client.PostAsync("Account/PerformExternalLogin", new FormUrlEncodedContent(
            [
                new("__RequestVerificationToken", OpenWishBrowser.AntiforgeryToken(loginPage)),
                new("provider", TestExternalLoginHandler.SchemeName),
                new("returnUrl", externalReturnUrl)
            ]));
            Assert.Equal(HttpStatusCode.Redirect, signIn.StatusCode);
            Assert.StartsWith("/Account/ExternalLogin", new Uri(browser.Client.BaseAddress!, signIn.Headers.Location!).PathAndQuery);
            signIn = await browser.Client.GetAsync(signIn.Headers.Location);
        }
        else
        {
            var invalidFields = OpenWishBrowser.FormFields(loginPage, "login");
            invalidFields.Add(new("Input.Email", email));
            invalidFields.Add(new("Input.Password", "Incorrect-password-123!"));
            var rejected = await browser.Client.PostAsync(loginUrl.PathAndQuery, new FormUrlEncodedContent(invalidFields));
            Assert.Equal(HttpStatusCode.OK, rejected.StatusCode);
            Assert.Contains("Check your email and password", await rejected.Content.ReadAsStringAsync());
            Assert.Equal(HttpStatusCode.Redirect, (await browser.Client.GetAsync(flow.AuthorizeUrl)).StatusCode);

            var fields = OpenWishBrowser.FormFields(await browser.GetPageAsync(loginUrl.PathAndQuery), "login");
            fields.Add(new("Input.Email", email));
            fields.Add(new("Input.Password", password));
            signIn = await browser.Client.PostAsync(loginUrl.PathAndQuery, new FormUrlEncodedContent(fields));
        }

        Assert.Equal(HttpStatusCode.Redirect, signIn.StatusCode);
        Assert.Equal(returnUrl, new Uri(browser.Client.BaseAddress!, signIn.Headers.Location!).PathAndQuery);

        var list = await browser.CreateWishlistAsync(Unique("Authenticated user list"));
        var consent = await browser.GetPageAsync(flow.AuthorizeUrl);
        Assert.Contains(list.Name, consent);
        Assert.DoesNotContain(otherList.Name, consent);
        var code = flow.ReadCode(await browser.ApproveAsync(flow.AuthorizeUrl, [list.PublicId]));
        var tokenResponse = await TokenAsync(new()
        {
            ["grant_type"] = "authorization_code",
            ["code"] = code,
            ["redirect_uri"] = LoopbackRedirect,
            ["client_id"] = client.ClientId,
            ["code_verifier"] = flow.CodeVerifier!,
            ["resource"] = Resource
        });
        Assert.Equal(HttpStatusCode.OK, tokenResponse.StatusCode);
        var token = (await tokenResponse.Content.ReadFromJsonAsync<IntegrationTokenResponse>())!;

        await using (var context = await factory.Services
            .GetRequiredService<IDbContextFactory<ApplicationDbContext>>().CreateDbContextAsync())
        {
            var ownerId = await context.Wishlists.Where(w => w.PublicId == list.PublicId).Select(w => w.OwnerId).SingleAsync();
            var connectionUserId = await context.IntegrationConnections
                .Where(c => c.PublicId == token.ConnectionId).Select(c => c.UserId).SingleAsync();
            Assert.Equal(expectedUserId, ownerId);
            Assert.Equal(expectedUserId, connectionUserId);
        }

        var http = factory.CreateApiClient();
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);
        var transport = new HttpClientTransport(new HttpClientTransportOptions { Endpoint = new Uri(Resource) }, http, ownsHttpClient: true);
        await using var mcp = await McpClient.CreateAsync(transport);
        var allowed = Assert.Single(await CallAsync<List<IntegrationWishlistModel>>(mcp, "list_wishlists"));
        Assert.Equal(list.PublicId, allowed.Id);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SdkClient_ConnectsThroughConsentAndRespectsExistingItemPermission(bool manageExistingItems)
    {
        using var owner = await SignInAsync();
        var list = await owner.CreateWishlistAsync(Unique("Assistant ideas"));
        var personal = await owner.AddOwnItemAsync(list.PublicId, new WishlistItemModel { Name = Unique("Private wish") });

        await using var mcp = await ConnectSdkClientAsync(owner, [list.PublicId], manageExistingItems);

        var tools = await mcp.ListToolsAsync();
        Assert.Equal(
            ["add_idea", "list_ideas", "list_wishlists", "remove_idea", "update_idea"],
            tools.Select(t => t.Name).Order(StringComparer.Ordinal));
        Assert.True(tools.Single(t => t.Name == "remove_idea").ProtocolTool.Annotations?.DestructiveHint);
        Assert.True(tools.Single(t => t.Name == "list_ideas").ProtocolTool.Annotations?.ReadOnlyHint);

        var wishlists = await CallAsync<List<IntegrationWishlistModel>>(mcp, "list_wishlists");
        var allowed = Assert.Single(wishlists);
        Assert.Equal(list.PublicId, allowed.Id);
        Assert.StartsWith(BaseUri, allowed.Url);

        var added = await CallAsync<IntegrationItemModel>(mcp, "add_idea", new()
        {
            ["name"] = "Pour-over kettle",
            ["price"] = 64.5m,
            ["url"] = "https://shop.example.test/kettle"
        });
        Assert.Equal(list.PublicId, added.WishlistId);
        Assert.StartsWith(BaseUri, added.OpenWishUrl);

        var updated = await CallAsync<IntegrationItemModel>(mcp, "update_idea", new()
        {
            ["ideaId"] = added.Id,
            ["description"] = "Gooseneck, matte black"
        });
        Assert.Equal("Pour-over kettle", updated.Name);
        Assert.Equal(64.5m, updated.Price);
        Assert.Equal("Gooseneck, matte black", updated.Description);

        var ideas = await CallAsync<List<IntegrationItemModel>>(mcp, "list_ideas");
        Assert.Equal(manageExistingItems ? [personal.PublicId, added.Id] : [added.Id], ideas.Select(i => i.Id));

        var touchPersonal = await mcp.CallToolAsync("remove_idea", new Dictionary<string, object?> { ["ideaId"] = personal.PublicId });
        Assert.Equal(!manageExistingItems, touchPersonal.IsError == true);
        Assert.Equal(!manageExistingItems, (await owner.GetItemsAsync(list.PublicId)).Any(i => i.PublicId == personal.PublicId));

        var invalid = await mcp.CallToolAsync("add_idea", new Dictionary<string, object?> { ["name"] = "Bad link", ["url"] = "javascript:alert(1)" });
        Assert.True(invalid.IsError);
        Assert.Contains("http or https", Text(invalid));

        var removed = await mcp.CallToolAsync("remove_idea", new Dictionary<string, object?> { ["ideaId"] = added.Id });
        Assert.NotEqual(true, removed.IsError);
        Assert.DoesNotContain(added.Id, (await owner.GetItemsAsync(list.PublicId)).Select(i => i.PublicId));

        var settings = await owner.GetPageAsync("Account/Manage/ConnectedApps");
        Assert.Contains(Resource, settings);
        Assert.Contains("AI assistant", settings);
        Assert.Contains("OpenWish Test Assistant", settings);
    }

    [Fact]
    public async Task ConsentPage_WarnsAboutSelfRegisteredClientsAndAllowsOnlyTheirRedirectOrigin()
    {
        using var owner = await SignInAsync();
        var client = await RegisterAsync();
        var flow = AuthorizationFlow.Start(client.ClientId, LoopbackRedirect);

        var response = await owner.Client.GetAsync(flow.AuthorizeUrl);
        var page = await response.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Only connect if you started this", page);
        Assert.Contains("127.0.0.1:33418", page);
        var csp = response.Headers.GetValues("Content-Security-Policy").Single();
        Assert.Contains("form-action 'self'", csp);
        Assert.Contains("http://127.0.0.1:33418", csp);
    }

    [Fact]
    public async Task ConsentRedirect_IncludesIssuerAndRequiresPkceForPublicClients()
    {
        using var owner = await SignInAsync();
        var list = await owner.CreateWishlistAsync(Unique("Issuer"));
        var client = await RegisterAsync();

        var withoutPkce = AuthorizationFlow.Start(client.ClientId, LoopbackRedirect, usePkce: false);
        var refused = QueryHelpers.ParseQuery((await owner.Client.GetAsync(withoutPkce.AuthorizeUrl)).Headers.Location!.Query);
        Assert.Equal("invalid_request", refused["error"].ToString());
        Assert.Equal(BaseUri, refused["iss"].ToString());

        var flow = AuthorizationFlow.Start(client.ClientId, LoopbackRedirect);
        var callback = QueryHelpers.ParseQuery((await owner.ApproveAsync(flow.AuthorizeUrl, [list.PublicId])).Query);
        Assert.Equal(BaseUri, callback["iss"].ToString());
        Assert.Equal(flow.State, callback["state"].ToString());
    }

    [Fact]
    public async Task UnknownResource_IsRejectedAtConsentAndTokenEndpoints()
    {
        using var owner = await SignInAsync();
        var list = await owner.CreateWishlistAsync(Unique("Resource"));
        // One registration for every case keeps the suite under the per-IP registration limit.
        var client = await RegisterAsync();
        string[] resources =
        [
            "https://elsewhere.example.test/mcp",
            "https://wish.example.test/MCP",
            "https://wish.example.test/mcp?tenant=other",
            "https://user@wish.example.test/mcp",
            "https://wish.example.test/mcp/other"
        ];

        foreach (var resource in resources)
        {
            var wrong = AuthorizationFlow.Start(client.ClientId, LoopbackRedirect, resource: resource);
            var refused = QueryHelpers.ParseQuery((await owner.Client.GetAsync(wrong.AuthorizeUrl)).Headers.Location!.Query);
            Assert.Equal("invalid_target", refused["error"].ToString());

            var flow = AuthorizationFlow.Start(client.ClientId, LoopbackRedirect);
            var code = flow.ReadCode(await owner.ApproveAsync(flow.AuthorizeUrl, [list.PublicId]));
            var response = await TokenAsync(new()
            {
                ["grant_type"] = "authorization_code",
                ["code"] = code,
                ["redirect_uri"] = LoopbackRedirect,
                ["client_id"] = client.ClientId,
                ["code_verifier"] = flow.CodeVerifier!,
                ["resource"] = resource
            });
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal("invalid_target", (await response.Content.ReadFromJsonAsync<IntegrationErrorModel>())!.Error);
        }
    }

    [Fact]
    public async Task McpAndApiTokens_OnlyWorkForTheirOwnAudience()
    {
        using var owner = await SignInAsync();
        var list = await owner.CreateWishlistAsync(Unique("Audience"));
        var tokens = await ConnectManuallyAsync(owner, [list.PublicId]);

        using (var api = factory.CreateApiClient())
        {
            api.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);
            Assert.Equal(HttpStatusCode.Unauthorized, (await api.GetAsync("api/integrations/v1/me")).StatusCode);
        }

        var studio = factory.CreateConnectedApp(OpenWishAppFactory.Studio);
        await studio.ConnectAsync(owner, [list.PublicId]);
        Assert.Equal(HttpStatusCode.Unauthorized, (await PingAsync(studio.AccessToken!)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await PingAsync(tokens.AccessToken)).StatusCode);
    }

    [Fact]
    public async Task RefreshToken_RotatesAndReuseRevokesTheConnection()
    {
        using var owner = await SignInAsync();
        var list = await owner.CreateWishlistAsync(Unique("Refresh"));
        var first = await ConnectManuallyAsync(owner, [list.PublicId]);
        Assert.Equal(3600, first.ExpiresIn);
        Assert.StartsWith("owr_", first.RefreshToken);

        var second = await RefreshAsync(first.ClientId, first.RefreshToken!);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        var rotated = (await second.Content.ReadFromJsonAsync<IntegrationTokenResponse>())!;
        Assert.NotEqual(first.RefreshToken, rotated.RefreshToken);
        Assert.Equal(first.ConnectionId, rotated.ConnectionId);
        Assert.Equal(HttpStatusCode.Unauthorized, (await PingAsync(first.AccessToken)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await PingAsync(rotated.AccessToken)).StatusCode);

        var replay = await RefreshAsync(first.ClientId, first.RefreshToken!);
        Assert.Equal(HttpStatusCode.BadRequest, replay.StatusCode);
        Assert.Equal("invalid_grant", (await replay.Content.ReadFromJsonAsync<IntegrationErrorModel>())!.Error);
        Assert.Equal(HttpStatusCode.Unauthorized, (await PingAsync(rotated.AccessToken)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await RefreshAsync(first.ClientId, rotated.RefreshToken!)).StatusCode);
    }

    [Fact]
    public async Task ExpiredAccessToken_IsRejectedUntilRefreshed()
    {
        using var owner = await SignInAsync();
        var list = await owner.CreateWishlistAsync(Unique("Expiry"));
        var tokens = await ConnectManuallyAsync(owner, [list.PublicId]);

        using (var scope = factory.Services.CreateScope())
        {
            await using var context = await scope.ServiceProvider
                .GetRequiredService<IDbContextFactory<ApplicationDbContext>>().CreateDbContextAsync();
            await context.IntegrationConnections.Where(c => c.PublicId == tokens.ConnectionId)
                .ExecuteUpdateAsync(s => s.SetProperty(c => c.AccessTokenExpiresOn, DateTimeOffset.UtcNow.AddMinutes(-1)));
        }

        Assert.Equal(HttpStatusCode.Unauthorized, (await PingAsync(tokens.AccessToken)).StatusCode);
        var refreshed = (await (await RefreshAsync(tokens.ClientId, tokens.RefreshToken!)).Content.ReadFromJsonAsync<IntegrationTokenResponse>())!;
        Assert.Equal(HttpStatusCode.OK, (await PingAsync(refreshed.AccessToken)).StatusCode);
    }

    [Fact]
    public async Task DisconnectingFromSettings_StopsAccessAndRefresh()
    {
        using var owner = await SignInAsync();
        var list = await owner.CreateWishlistAsync(Unique("Disconnect"));
        var tokens = await ConnectManuallyAsync(owner, [list.PublicId]);

        await owner.DisconnectFromSettingsAsync(tokens.ConnectionId);

        Assert.Equal(HttpStatusCode.Unauthorized, (await PingAsync(tokens.AccessToken)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await RefreshAsync(tokens.ClientId, tokens.RefreshToken!)).StatusCode);
    }

    [Theory]
    [InlineData("http://assistant.example.test/callback")]
    [InlineData("https://assistant.example.test/callback#fragment")]
    [InlineData("https://user:pass@assistant.example.test/callback")]
    [InlineData("javascript:alert(1)")]
    [InlineData("cursor://anysphere.cursor-retrieval/oauth/callback")]
    public async Task Registration_RejectsUnsafeRedirects(string redirectUri)
    {
        var response = await PostRegistrationAsync(new { client_name = "Bad", redirect_uris = new[] { redirectUri } });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("invalid_redirect_uri", (await response.Content.ReadFromJsonAsync<IntegrationErrorModel>())!.Error);
    }

    [Fact]
    public async Task ConfidentialRegistration_RequiresItsSecretAtTheTokenEndpoint()
    {
        using var owner = await SignInAsync();
        var list = await owner.CreateWishlistAsync(Unique("Confidential"));
        var response = await PostRegistrationAsync(new
        {
            client_name = "Hosted Assistant",
            redirect_uris = new[] { "https://assistant.example.test/oauth/callback" },
            token_endpoint_auth_method = "client_secret_post"
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var body = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        var clientId = body.RootElement.GetProperty("client_id").GetString()!;
        var secret = body.RootElement.GetProperty("client_secret").GetString()!;
        Assert.StartsWith("owms_", secret);

        var flow = AuthorizationFlow.Start(clientId, "https://assistant.example.test/oauth/callback");
        var code = flow.ReadCode(await owner.ApproveAsync(flow.AuthorizeUrl, [list.PublicId]));
        var fields = new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["code"] = code,
            ["redirect_uri"] = "https://assistant.example.test/oauth/callback",
            ["client_id"] = clientId,
            ["code_verifier"] = flow.CodeVerifier!
        };
        Assert.Equal(HttpStatusCode.Unauthorized, (await TokenAsync(fields)).StatusCode);

        fields["client_secret"] = secret;
        Assert.Equal(HttpStatusCode.OK, (await TokenAsync(fields)).StatusCode);
    }

    [Fact]
    public async Task LoopbackRedirect_MayUseAnyPortForTheRegisteredPath()
    {
        using var owner = await SignInAsync();
        var list = await owner.CreateWishlistAsync(Unique("Loopback"));
        var client = await RegisterAsync();

        var otherPort = AuthorizationFlow.Start(client.ClientId, "http://127.0.0.1:50123/callback");
        var callback = await owner.ApproveAsync(otherPort.AuthorizeUrl, [list.PublicId]);
        Assert.Equal(50123, callback.Port);

        var otherPath = AuthorizationFlow.Start(client.ClientId, "http://127.0.0.1:33418/elsewhere");
        var page = await owner.GetPageAsync(otherPath.AuthorizeUrl);
        Assert.Contains("this OpenWish doesn", page);
    }

    [Fact]
    public async Task DisabledMcp_HidesTheServerAndRegistration()
    {
        await using var disabled = factory.WithWebHostBuilder(builder =>
            builder.UseSetting("OpenWishSettings:Integrations:Mcp:Enabled", "false"));
        using var client = disabled.CreateClient(new() { BaseAddress = new Uri("https://localhost/") });

        // Without the MCP endpoint, /mcp falls through to the page router, which only accepts GET and form posts.
        Assert.Contains((await client.PostAsJsonAsync("mcp", new { })).StatusCode, new[] { HttpStatusCode.NotFound, HttpStatusCode.MethodNotAllowed });
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsJsonAsync("oauth/register", new { redirect_uris = new[] { LoopbackRedirect } })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync(".well-known/oauth-protected-resource/mcp")).StatusCode);

        using var server = await JsonDocument.ParseAsync(await client.GetStreamAsync(".well-known/oauth-authorization-server"));
        Assert.False(server.RootElement.TryGetProperty("registration_endpoint", out _));
        Assert.DoesNotContain("refresh_token", Strings(server.RootElement.GetProperty("grant_types_supported")));
    }

    [Fact]
    public async Task PartialUpdate_PreservesLongExistingContent_ButRejectsInvalidNewValues()
    {
        using var owner = await SignInAsync();
        var list = await owner.CreateWishlistAsync(Unique("Long existing idea"));
        var destination = await owner.CreateWishlistAsync(Unique("Move long idea"));
        var description = new string('x', IntegrationItemRequest.DescriptionMaxLength + 1);
        var own = await owner.AddOwnItemAsync(list.PublicId,
            new WishlistItemModel { Name = "Existing idea", Description = description });
        await using var mcp = await ConnectSdkClientAsync(owner, [list.PublicId, destination.PublicId]);
        var updated = await CallAsync<IntegrationItemModel>(mcp, "update_idea", new()
        {
            ["ideaId"] = own.PublicId,
            ["priority"] = 1,
            ["wishlistId"] = destination.PublicId
        });
        Assert.Equal(description, updated.Description);
        Assert.Equal(1, updated.Priority);
        Assert.Equal(destination.PublicId, updated.WishlistId);
        var invalidDescription = await mcp.CallToolAsync("update_idea", new Dictionary<string, object?>
        {
            ["ideaId"] = own.PublicId,
            ["description"] = description
        });
        Assert.True(invalidDescription.IsError);
        var invalidUrl = await mcp.CallToolAsync("update_idea", new Dictionary<string, object?>
        {
            ["ideaId"] = own.PublicId,
            ["url"] = "javascript:alert(1)"
        });
        Assert.True(invalidUrl.IsError);
        var invalidPriority = await mcp.CallToolAsync("update_idea", new Dictionary<string, object?>
        {
            ["ideaId"] = own.PublicId,
            ["priority"] = 4
        });
        Assert.True(invalidPriority.IsError);
        var saved = Assert.Single(await owner.GetItemsAsync(destination.PublicId));
        Assert.Equal(description, saved.Description);
        Assert.Equal(1, saved.Priority);
    }

    private async Task<McpClient> ConnectSdkClientAsync(OpenWishBrowser owner, IReadOnlyList<string> wishlistIds,
        bool manageExistingItems = true)
    {
        var http = factory.CreateApiClient();
        var transport = new HttpClientTransport(new HttpClientTransportOptions
        {
            Endpoint = new Uri(Resource),
            OAuth = new ClientOAuthOptions
            {
                RedirectUri = new Uri(LoopbackRedirect),
                DynamicClientRegistration = new() { ClientName = "OpenWish Test Assistant" },
                AuthorizationCallbackHandler = async (context, cancellationToken) =>
                {
                    var callback = await owner.ApproveAsync(context.AuthorizationUri.PathAndQuery.TrimStart('/'), wishlistIds,
                        manageExistingItems: manageExistingItems);
                    var query = QueryHelpers.ParseQuery(callback.Query);
                    return new AuthorizationResult
                    {
                        Code = query["code"].ToString(),
                        State = query["state"].ToString(),
                        Iss = query["iss"].ToString()
                    };
                }
            }
        }, http, ownsHttpClient: true);

        return await McpClient.CreateAsync(transport);
    }

    private async Task<ManualTokens> ConnectManuallyAsync(OpenWishBrowser owner, IReadOnlyList<string> wishlistIds)
    {
        var client = await RegisterAsync();
        var flow = AuthorizationFlow.Start(client.ClientId, LoopbackRedirect);
        var code = flow.ReadCode(await owner.ApproveAsync(flow.AuthorizeUrl, wishlistIds));
        var response = await TokenAsync(new()
        {
            ["grant_type"] = "authorization_code",
            ["code"] = code,
            ["redirect_uri"] = LoopbackRedirect,
            ["client_id"] = client.ClientId,
            ["code_verifier"] = flow.CodeVerifier!,
            ["resource"] = Resource
        });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var token = (await response.Content.ReadFromJsonAsync<IntegrationTokenResponse>())!;
        return new(client.ClientId, token.AccessToken, token.RefreshToken, token.ConnectionId, token.ExpiresIn);
    }

    private async Task<RegisteredClient> RegisterAsync()
    {
        var response = await PostRegistrationAsync(new
        {
            client_name = "Loopback Assistant",
            redirect_uris = new[] { LoopbackRedirect }
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var body = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        Assert.Equal("none", body.RootElement.GetProperty("token_endpoint_auth_method").GetString());
        Assert.False(body.RootElement.TryGetProperty("client_secret", out _));
        return new(body.RootElement.GetProperty("client_id").GetString()!);
    }

    private async Task<HttpResponseMessage> PostRegistrationAsync(object body)
    {
        using var client = factory.CreateApiClient();
        return await client.PostAsJsonAsync("oauth/register", body);
    }

    private async Task<HttpResponseMessage> TokenAsync(Dictionary<string, string> fields)
    {
        using var client = factory.CreateApiClient();
        return await client.PostAsync("oauth/token", new FormUrlEncodedContent(fields));
    }

    private Task<HttpResponseMessage> RefreshAsync(string clientId, string refreshToken) => TokenAsync(new()
    {
        ["grant_type"] = "refresh_token",
        ["refresh_token"] = refreshToken,
        ["client_id"] = clientId,
        ["resource"] = Resource
    });

    private async Task<HttpResponseMessage> PingAsync(string accessToken)
    {
        using var client = factory.CreateApiClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, "mcp")
        {
            Content = JsonContent.Create(new { jsonrpc = "2.0", id = 1, method = "ping" })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.Accept.ParseAdd("application/json");
        request.Headers.Accept.ParseAdd("text/event-stream");
        return await client.SendAsync(request);
    }

    private static async Task<T> CallAsync<T>(McpClient client, string tool, Dictionary<string, object?>? arguments = null)
    {
        var result = await client.CallToolAsync(tool, arguments ?? []);
        Assert.True(result.IsError != true, Text(result));
        return JsonSerializer.Deserialize<T>(Text(result), OpenWishBrowser.Json)!;
    }

    private static string Text(CallToolResult result) =>
        string.Concat(result.Content.OfType<TextContentBlock>().Select(block => block.Text));

    private static string[] Strings(JsonElement array) => array.EnumerateArray().Select(e => e.GetString()!).ToArray();

    private async Task<OpenWishBrowser> SignInAsync()
    {
        var browser = factory.CreateBrowser();
        await browser.SignInAsync("owner");
        return browser;
    }

    private static string Unique(string name) => $"{name} {Guid.NewGuid():N}";

    private sealed record RegisteredClient(string ClientId);

    private sealed record ManualTokens(string ClientId, string AccessToken, string? RefreshToken, string ConnectionId, int? ExpiresIn);

    private sealed record AuthorizationFlow(string AuthorizeUrl, string State, string? CodeVerifier)
    {
        public static AuthorizationFlow Start(string clientId, string redirectUri, bool usePkce = true, string resource = Resource)
        {
            var state = Base64Url(RandomNumberGenerator.GetBytes(16));
            var parameters = new Dictionary<string, string?>
            {
                ["response_type"] = "code",
                ["client_id"] = clientId,
                ["redirect_uri"] = redirectUri,
                ["state"] = state,
                ["scope"] = IntegrationScopes.WishlistItems,
                ["resource"] = resource
            };

            string? verifier = null;
            if (usePkce)
            {
                verifier = Base64Url(RandomNumberGenerator.GetBytes(48));
                parameters["code_challenge"] = Base64Url(SHA256.HashData(Encoding.ASCII.GetBytes(verifier)));
                parameters["code_challenge_method"] = "S256";
            }

            return new(QueryHelpers.AddQueryString("oauth/authorize", parameters), state, verifier);
        }

        public string ReadCode(Uri callback)
        {
            var query = QueryHelpers.ParseQuery(callback.Query);
            Assert.Equal(State, query["state"].ToString());
            Assert.True(!query.TryGetValue("error", out var error), $"Authorization failed: {error}");
            return query["code"].ToString();
        }

        private static string Base64Url(byte[] bytes) =>
            Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }
}

[CollectionDefinition(Name)]
public sealed class McpCollection : ICollectionFixture<OpenWishAppFactory>
{
    public const string Name = "OpenWish MCP host";
}