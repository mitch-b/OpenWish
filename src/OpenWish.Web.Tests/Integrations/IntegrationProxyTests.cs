using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Xunit;

namespace OpenWish.Web.Tests.Integrations;

[Collection(ConnectionLifecycleCollection.Name)]
public sealed class IntegrationProxyTests(OpenWishAppFactory factory)
{
    [Theory]
    [InlineData("127.0.0.1", "https://wish.proxy.test/")]
    [InlineData("192.0.2.10", "http://wish.proxy.test/")]
    public async Task Discovery_UsesForwardedHttpsOnlyFromTrustedProxy(string proxyIp, string expectedIssuer)
    {
        await using var host = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("OpenWishSettings:BaseUri", "");
            builder.UseSetting("ForwardedHeaders:KnownProxies:0", "127.0.0.1");
        });
        var response = await host.Server.SendAsync(context =>
        {
            context.Connection.RemoteIpAddress = IPAddress.Parse(proxyIp);
            context.Request.Scheme = "http";
            context.Request.Host = new("wish.proxy.test");
            context.Request.Path = "/.well-known/oauth-authorization-server";
            context.Request.Headers["X-Forwarded-Proto"] = "https";
            context.Request.Headers["X-Forwarded-Host"] = "untrusted.example";
        });
        Assert.Equal(200, response.Response.StatusCode);
        using var metadata = await JsonDocument.ParseAsync(response.Response.Body);
        Assert.Equal(expectedIssuer, metadata.RootElement.GetProperty("issuer").GetString());
        Assert.Equal($"{expectedIssuer}oauth/authorize",
            metadata.RootElement.GetProperty("authorization_endpoint").GetString());
        Assert.Equal($"{expectedIssuer}oauth/token",
            metadata.RootElement.GetProperty("token_endpoint").GetString());
    }
}