using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using OpenWish.Application.Integrations;
using OpenWish.Application.Models.Configuration;
using Xunit;

namespace OpenWish.Application.Tests.Integrations;

public class IntegrationAppRegistryTests
{
    private const string Secret = "a-secret-that-is-long-enough-0123456789";

    [Fact]
    public void Registry_IsDisabled_WhenNothingIsConfigured()
    {
        var registry = Create();

        Assert.False(registry.IsEnabled);
        Assert.Empty(registry.Apps);
        Assert.Empty(registry.RedirectOrigins);
    }

    [Fact]
    public void Registry_SkipsInvalidApps_InsteadOfFailingStartup()
    {
        var registry = Create(
            App("ok", redirect: "https://ok.example/callback"),
            App("short-secret", secret: "too-short"),
            App("bad id!"),
            App("no-redirects", redirect: "ftp://files.example/cb"),
            App("fragment", redirect: "https://frag.example/cb#x"),
            App("ok", redirect: "https://duplicate.example/callback", displayName: "Duplicate"));

        var app = Assert.Single(registry.Apps);
        Assert.Equal("ok", app.ClientId);
        Assert.Equal(["https://ok.example"], registry.RedirectOrigins);
    }

    [Fact]
    public void Authenticate_RequiresTheExactSecret()
    {
        var registry = Create(App("studio"));

        Assert.NotNull(registry.Authenticate("studio", Secret));
        Assert.Null(registry.Authenticate("studio", Secret + "x"));
        Assert.Null(registry.Authenticate("studio", null));
        Assert.Null(registry.Authenticate("other", Secret));
    }

    [Fact]
    public void RedirectUris_MatchExactly()
    {
        var registry = Create(App("studio", redirect: "https://studio.example/callback"));
        var app = registry.Find("studio")!;

        Assert.True(IntegrationAppRegistry.IsRegisteredRedirect(app, "https://studio.example/callback"));
        Assert.False(IntegrationAppRegistry.IsRegisteredRedirect(app, "https://studio.example/callback/"));
        Assert.False(IntegrationAppRegistry.IsRegisteredRedirect(app, "https://studio.example/callback?next=evil"));
        Assert.False(IntegrationAppRegistry.IsRegisteredRedirect(app, "https://STUDIO.example/callback"));
    }

    [Fact]
    public void DisplayName_DefaultsToClientId_AndHomepageMustBeHttp()
    {
        var registry = Create(App("pantry", displayName: " ", homepage: "javascript:alert(1)"));
        var app = registry.Find("pantry")!;

        Assert.Equal("pantry", app.DisplayName);
        Assert.Null(app.HomepageUrl);
    }

    [Theory]
    [InlineData("dBjftJeZ4CVP-mB92K27uhbUJU1p1r_wW1gFWFOEjXk", "E9Melhoa2OwvFrEMTJguCHaoeK1t8URWbuGJSstw-cM", true)]
    [InlineData("dBjftJeZ4CVP-mB92K27uhbUJU1p1r_wW1gFWFOEjXx", "E9Melhoa2OwvFrEMTJguCHaoeK1t8URWbuGJSstw-cM", false)]
    [InlineData("too-short", "E9Melhoa2OwvFrEMTJguCHaoeK1t8URWbuGJSstw-cM", false)]
    [InlineData(null, "E9Melhoa2OwvFrEMTJguCHaoeK1t8URWbuGJSstw-cM", false)]
    public void Pkce_UsesTheRfc7636S256Transform(string? verifier, string challenge, bool expected)
    {
        // The first case is the worked example from RFC 7636 appendix B.
        Assert.Equal(expected, IntegrationTokens.VerifyPkce(verifier, challenge));
    }

    [Fact]
    public void Tokens_AreRandom_Prefixed_AndStoredOnlyAsHashes()
    {
        var first = IntegrationTokens.NewAccessToken();
        var second = IntegrationTokens.NewAccessToken();

        Assert.StartsWith("owi_", first);
        Assert.NotEqual(first, second);
        Assert.Equal(64, IntegrationTokens.Hash(first).Length);
        Assert.DoesNotContain(first, IntegrationTokens.Hash(first));
    }

    private static IntegrationAppRegistry Create(params IntegrationAppSettings[] apps) =>
        new(
            Options.Create(new OpenWishSettings { Integrations = new IntegrationSettings { Apps = [.. apps] } }),
            NullLogger<IntegrationAppRegistry>.Instance);

    private static IntegrationAppSettings App(
        string clientId,
        string secret = Secret,
        string redirect = "https://app.example/callback",
        string? displayName = "Example app",
        string? homepage = null) => new()
        {
            ClientId = clientId,
            ClientSecret = secret,
            DisplayName = displayName,
            HomepageUrl = homepage,
            RedirectUris = [redirect]
        };
}