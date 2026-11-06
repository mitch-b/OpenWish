using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;
using OpenWish.Shared.Integrations;
using Xunit;

namespace OpenWish.Web.Tests.Integrations;

/// <summary>
/// A stand-in for a third-party app: it starts the OAuth flow, checks state on the callback,
/// exchanges the code server to server, and then calls the integration API with only a bearer token.
/// </summary>
public sealed class FakeConnectedApp(FakeAppRegistration registration, Func<HttpClient> createClient)
{
    private string? _state;
    private string? _codeVerifier;

    public FakeAppRegistration Registration { get; } = registration;

    public string? AccessToken { get; private set; }

    public string? ConnectionId { get; private set; }

    public string BuildAuthorizeUrl(bool usePkce = true, string? redirectUri = null, string responseType = "code")
    {
        _state = RandomToken();
        var parameters = new Dictionary<string, string?>
        {
            ["response_type"] = responseType,
            ["client_id"] = Registration.ClientId,
            ["redirect_uri"] = redirectUri ?? Registration.RedirectUri,
            ["state"] = _state,
            ["scope"] = IntegrationScopes.WishlistItems
        };

        _codeVerifier = null;
        if (usePkce)
        {
            _codeVerifier = RandomToken() + RandomToken();
            parameters["code_challenge"] = Base64Url(SHA256.HashData(Encoding.ASCII.GetBytes(_codeVerifier)));
            parameters["code_challenge_method"] = "S256";
        }

        return QueryHelpers.AddQueryString("oauth/authorize", parameters);
    }

    /// <summary>Handles the browser landing on the app's callback and returns the authorization code.</summary>
    public string ReadCallback(Uri callback)
    {
        Assert.StartsWith(Registration.RedirectUri, callback.ToString(), StringComparison.Ordinal);
        var query = QueryHelpers.ParseQuery(callback.Query);
        Assert.Equal(_state, query["state"].ToString());
        Assert.True(!query.TryGetValue("error", out var error), $"Authorization failed: {error}");
        return query["code"].ToString();
    }

    public async Task<HttpResponseMessage> RequestTokenAsync(
        string code,
        string? clientSecret = null,
        string? redirectUri = null,
        string? codeVerifier = null,
        bool useBasicAuth = false)
    {
        using var client = createClient();
        var fields = new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["code"] = code,
            ["redirect_uri"] = redirectUri ?? Registration.RedirectUri
        };

        var verifier = codeVerifier ?? _codeVerifier;
        if (verifier is not null)
        {
            fields["code_verifier"] = verifier;
        }

        if (useBasicAuth)
        {
            var credentials = $"{Uri.EscapeDataString(Registration.ClientId)}:{Uri.EscapeDataString(clientSecret ?? Registration.ClientSecret)}";
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                "Basic",
                Convert.ToBase64String(Encoding.UTF8.GetBytes(credentials)));
        }
        else
        {
            fields["client_id"] = Registration.ClientId;
            fields["client_secret"] = clientSecret ?? Registration.ClientSecret;
        }

        return await client.PostAsync("oauth/token", new FormUrlEncodedContent(fields));
    }

    public async Task<IntegrationTokenResponse> ExchangeAsync(string code, bool useBasicAuth = false)
    {
        var response = await RequestTokenAsync(code, useBasicAuth: useBasicAuth);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("no-store", response.Headers.CacheControl?.ToString());
        var token = (await response.Content.ReadFromJsonAsync<IntegrationTokenResponse>())!;
        AccessToken = token.AccessToken;
        ConnectionId = token.ConnectionId;
        return token;
    }

    /// <summary>Signs the app in through the person's browser: consent, callback, and code exchange.</summary>
    public async Task ConnectAsync(OpenWishBrowser browser, IEnumerable<string> wishlistIds, string? newListName = null)
    {
        var callback = await browser.ApproveAsync(BuildAuthorizeUrl(), wishlistIds, newListName);
        await ExchangeAsync(ReadCallback(callback));
    }

    public async Task<HttpResponseMessage> RevokeAsync(string? token = null)
    {
        using var client = createClient();
        return await client.PostAsync("oauth/revoke", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["token"] = token ?? AccessToken!,
            ["client_id"] = Registration.ClientId,
            ["client_secret"] = Registration.ClientSecret
        }));
    }

    public HttpClient Api(string? token = null)
    {
        var client = createClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token ?? AccessToken);
        client.BaseAddress = new Uri(client.BaseAddress!, "api/integrations/v1/");
        return client;
    }

    private static string RandomToken() => Base64Url(RandomNumberGenerator.GetBytes(24));

    private static string Base64Url(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}