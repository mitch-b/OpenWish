using System.Net;
using OpenWish.Web.Client.Services;
using Xunit;

namespace OpenWish.Web.Tests.Integrations;

public sealed class EventInvitationHttpClientTests
{
    [Fact]
    public async Task Claim_ReturnsNullForNoMatchingInvitation()
    {
        using var client = CreateClient(HttpStatusCode.NoContent);
        var service = new EventHttpClientService(client);

        Assert.Null(await service.ClaimEventInvitationByEmailAsync("event", "user", "family@example.test"));
    }

    [Fact]
    public async Task Claim_ThrowsForUnavailableEvent()
    {
        using var client = CreateClient(HttpStatusCode.NotFound);
        var service = new EventHttpClientService(client);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            service.ClaimEventInvitationByEmailAsync("event", "user", "family@example.test"));
    }

    private static HttpClient CreateClient(HttpStatusCode status) =>
        new(new StubHandler(status)) { BaseAddress = new Uri("https://localhost/") };

    private sealed class StubHandler(HttpStatusCode status) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(status));
    }
}