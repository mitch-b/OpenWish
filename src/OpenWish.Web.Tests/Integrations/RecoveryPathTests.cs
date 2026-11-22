using System.Net;
using System.Net.Http.Json;
using OpenWish.Shared.Models;
using Xunit;

namespace OpenWish.Web.Tests.Integrations;

[Collection(IntegrationCollection.Name)]
public sealed class RecoveryPathTests(OpenWishAppFactory factory)
{
    [Fact]
    public async Task UnknownRoutes_ShowFriendlyPageOnlyForBrowserNavigations()
    {
        using var browser = factory.CreateBrowser();

        var page = await SendHtmlGetAsync(browser, "no-such-page");
        Assert.Equal(HttpStatusCode.NotFound, page.StatusCode);
        Assert.Contains("Go to dashboard", await page.Content.ReadAsStringAsync(), StringComparison.Ordinal);

        var api = await SendHtmlGetAsync(browser, "api/no-such-endpoint");
        Assert.Equal(HttpStatusCode.NotFound, api.StatusCode);
        Assert.DoesNotContain("Go to dashboard", await api.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task WishlistWrites_RejectInvalidInputWithBadRequest()
    {
        using var browser = factory.CreateBrowser();
        await browser.SignInAsync("owner");

        var blankList = await browser.Client.PostAsJsonAsync("api/wishlists", new WishlistModel { Name = "   " }, OpenWishBrowser.Json);
        Assert.Equal(HttpStatusCode.BadRequest, blankList.StatusCode);

        var list = await browser.CreateWishlistAsync($"Validation {Guid.NewGuid():N}");
        var negativePrice = await browser.Client.PostAsJsonAsync(
            $"api/wishlists/{list.PublicId}/items",
            new WishlistItemModel { Name = "Scarf", Price = -1 },
            OpenWishBrowser.Json);
        Assert.Equal(HttpStatusCode.BadRequest, negativePrice.StatusCode);
    }

    [Theory]
    [InlineData("Account/Manage/GenerateRecoveryCodes")]
    [InlineData("Account/Manage/Disable2fa")]
    public async Task TwoFactorPages_RedirectToSettingsWhenTwoFactorIsOff(string path)
    {
        using var browser = factory.CreateBrowser();
        await browser.SignInAsync("owner");

        var response = await SendHtmlGetAsync(browser, path);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.EndsWith("Account/Manage/TwoFactorAuthentication", response.Headers.Location?.OriginalString, StringComparison.Ordinal);
    }

    private static Task<HttpResponseMessage> SendHtmlGetAsync(OpenWishBrowser browser, string path)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Accept.ParseAdd("text/html");
        return browser.Client.SendAsync(request);
    }
}