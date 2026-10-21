using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using OpenWish.Shared.Models;
using Xunit;

namespace OpenWish.Web.Tests.Integrations;

/// <summary>A signed-in person using OpenWish in a browser: cookies, antiforgery tokens, and form posts.</summary>
public sealed partial class OpenWishBrowser(HttpClient client) : IDisposable
{
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public HttpClient Client { get; } = client;

    public async Task SignInAsync(string persona)
    {
        var response = await Client.PostAsync($"auth/dev-login?persona={persona}", content: null);
        response.EnsureSuccessStatusCode();
    }

    public async Task<WishlistModel> CreateWishlistAsync(string name)
    {
        var response = await Client.PostAsJsonAsync("api/wishlists", new WishlistModel { Name = name, IsPrivate = true }, Json);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<WishlistModel>(Json))!;
    }

    public async Task<WishlistItemModel> AddOwnItemAsync(string wishlistId, WishlistItemModel item)
    {
        var response = await Client.PostAsJsonAsync($"api/wishlists/{wishlistId}/items", item, Json);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<WishlistItemModel>(Json))!;
    }

    public async Task<List<WishlistItemModel>> GetItemsAsync(string wishlistId)
    {
        var response = await Client.GetAsync($"api/wishlists/{wishlistId}/items");
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<List<WishlistItemModel>>(Json))!;
    }

    public async Task<List<WishlistModel>> GetWishlistsAsync()
    {
        var response = await Client.GetAsync("api/wishlists");
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<List<WishlistModel>>(Json))!;
    }

    public async Task<string> GetPageAsync(string url)
    {
        var response = await Client.GetAsync(url);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadAsStringAsync();
    }

    /// <summary>Opens the consent screen and approves it with the chosen lists. Returns the redirect back to the app.</summary>
    public async Task<Uri> ApproveAsync(string authorizeUrl, IEnumerable<string> wishlistIds, string? newListName = null)
    {
        var page = await GetPageAsync(authorizeUrl);
        var fields = FormFields(page, "consent-approve");
        foreach (var wishlistId in wishlistIds)
        {
            fields.Add(new("wishlist", wishlistId));
        }

        if (newListName is not null)
        {
            fields.Add(new("createList", "true"));
            fields.Add(new("newListName", newListName));
        }

        var response = await Client.PostAsync(authorizeUrl, new FormUrlEncodedContent(fields));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        return response.Headers.Location!;
    }

    public async Task<Uri> DenyAsync(string authorizeUrl)
    {
        var page = await GetPageAsync(authorizeUrl);
        var response = await Client.PostAsync(authorizeUrl, new FormUrlEncodedContent(FormFields(page, "consent-deny")));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        return response.Headers.Location!;
    }

    public async Task DisconnectFromSettingsAsync(string connectionId)
    {
        var url = $"Account/Manage/ConnectedApps?disconnect={Uri.EscapeDataString(connectionId)}";
        var page = await GetPageAsync(url);
        var response = await Client.PostAsync(url, new FormUrlEncodedContent(FormFields(page, $"disconnect-{connectionId}")));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
    }

    public async Task SaveConnectedListsAsync(string connectionId, IEnumerable<string> wishlistIds, string defaultWishlistId)
    {
        const string url = "Account/Manage/ConnectedApps";
        var page = await GetPageAsync(url);
        var fields = FormFields(page, $"lists-{connectionId}");
        fields.AddRange(wishlistIds.Select(id => new KeyValuePair<string, string>("wishlist", id)));
        fields.Add(new("defaultWishlist", defaultWishlistId));
        var response = await Client.PostAsync(url, new FormUrlEncodedContent(fields));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
    }

    /// <summary>The hidden fields Blazor renders for a named static form: its handler name and antiforgery token.</summary>
    private static List<KeyValuePair<string, string>> FormFields(string html, string formName)
    {
        var form = FormPattern().Matches(html)
            .Select(match => match.Value)
            .FirstOrDefault(segment => segment.Contains($"value=\"{formName}\"", StringComparison.Ordinal))
            ?? throw new Xunit.Sdk.XunitException($"Form '{formName}' was not rendered.");

        var token = AntiforgeryPattern().Match(form);
        Assert.True(token.Success, $"Form '{formName}' has no antiforgery token.");

        return
        [
            new("_handler", formName),
            new("__RequestVerificationToken", WebUtility.HtmlDecode(token.Groups["value"].Value))
        ];
    }

    public void Dispose() => Client.Dispose();

    [GeneratedRegex("<form[\\s\\S]*?</form>", RegexOptions.IgnoreCase)]
    private static partial Regex FormPattern();

    [GeneratedRegex("name=\"__RequestVerificationToken\"[^>]*value=\"(?<value>[^\"]+)\"|value=\"(?<value>[^\"]+)\"[^>]*name=\"__RequestVerificationToken\"")]
    private static partial Regex AntiforgeryPattern();
}