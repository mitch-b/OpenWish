using System.Net;
using Xunit;
using Xunit.Sdk;

namespace OpenWish.Web.Tests.Integrations;

public sealed class OpenWishBrowserTests
{
    [Fact]
    public async Task GetPage_ReturnsSuccessfulResponseBody()
    {
        using var browser = CreateBrowser(_ => new(HttpStatusCode.OK)
        {
            Content = new StringContent("<h1>Connected apps</h1>")
        });

        Assert.Equal("<h1>Connected apps</h1>", await browser.GetPageAsync("Account/Manage/ConnectedApps"));
    }

    [Theory]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.Redirect)]
    public async Task GetPage_ReportsUnexpectedStatusAndResponseBody(HttpStatusCode status)
    {
        using var browser = CreateBrowser(_ => new(status)
        {
            Content = new StringContent("Synthetic server diagnostic")
        });

        var exception = await Assert.ThrowsAnyAsync<XunitException>(() =>
            browser.GetPageAsync("Account/Manage/ConnectedApps"));

        Assert.Contains($"GET Account/Manage/ConnectedApps returned {(int)status}", exception.Message);
        Assert.Contains("Synthetic server diagnostic", exception.Message);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SettingsForm_ReportsFailedPostResponseBody(bool disconnect)
    {
        var formName = disconnect ? "disconnect-connection" : "lists-connection";
        using var browser = CreateBrowser(request => request.Method == HttpMethod.Get
            ? new(HttpStatusCode.OK)
            {
                Content = new StringContent($"""
                    <form>
                        <input name="_handler" value="{formName}" />
                        <input name="__RequestVerificationToken" value="synthetic-token" />
                    </form>
                    """)
            }
            : new(HttpStatusCode.InternalServerError)
            {
                Content = new StringContent("Synthetic form diagnostic")
            });

        var exception = await Assert.ThrowsAnyAsync<XunitException>(() => disconnect
            ? browser.DisconnectFromSettingsAsync("connection")
            : browser.SaveConnectedListsAsync("connection", ["wishlist"], "wishlist"));

        Assert.Contains("POST Account/Manage/ConnectedApps", exception.Message);
        Assert.Contains("returned 500: Synthetic form diagnostic", exception.Message);
    }

    private static OpenWishBrowser CreateBrowser(Func<HttpRequestMessage, HttpResponseMessage> respond) =>
        new(new HttpClient(new StubHandler(respond)) { BaseAddress = new Uri("https://localhost/") });

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(respond(request));
    }
}