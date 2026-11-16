using System.Net;
using Xunit;

namespace OpenWish.Web.Tests.Integrations;

[Collection(IntegrationCollection.Name)]
public sealed class OfflineSupportTests(OpenWishAppFactory factory)
{
    [Fact]
    public async Task InstallableApp_ServesAnonymousServiceWorkerAndOfflinePage()
    {
        using var browser = factory.CreateBrowser();

        var worker = await browser.Client.GetAsync("service-worker.js");
        Assert.Equal(HttpStatusCode.OK, worker.StatusCode);
        var script = await worker.Content.ReadAsStringAsync();
        Assert.Contains("offline.html", script, StringComparison.Ordinal);
        Assert.Contains("request.mode !== \"navigate\"", script, StringComparison.Ordinal);

        var offline = await browser.GetPageAsync("offline.html");
        Assert.Contains("You're offline", offline, StringComparison.Ordinal);
        Assert.DoesNotContain("AlexDemo", offline, StringComparison.Ordinal);
    }
}