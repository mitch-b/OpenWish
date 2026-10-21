using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Npgsql;
using Testcontainers.PostgreSql;
using Xunit;

namespace OpenWish.Web.Tests.Integrations;

/// <summary>
/// Hosts the real OpenWish web app against PostgreSQL with two registered connected apps.
/// Set OPENWISH_TEST_POSTGRES to reuse an existing server; otherwise a disposable container starts.
/// </summary>
public sealed class OpenWishAppFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public static readonly FakeAppRegistration Studio = new(
        "studio",
        "Studio Notebook",
        "studio-test-secret-0123456789abcdefghijklmnop",
        "https://studio.example.test/openwish/callback");

    public static readonly FakeAppRegistration Pantry = new(
        "pantry",
        "Pantry Planner",
        "pantry-test-secret-0123456789abcdefghijklmnop",
        "https://pantry.example.test/callback");

    private PostgreSqlContainer? _container;
    private string _connectionString = string.Empty;

    public async Task InitializeAsync()
    {
        var external = Environment.GetEnvironmentVariable("OPENWISH_TEST_POSTGRES");
        if (!string.IsNullOrWhiteSpace(external))
        {
            _connectionString = new NpgsqlConnectionStringBuilder(external)
            {
                Database = $"openwish_it_{Guid.NewGuid():N}"
            }.ConnectionString;
        }
        else
        {
            _container = new PostgreSqlBuilder("postgres:18").Build();
            await _container.StartAsync();
            _connectionString = _container.GetConnectionString();
        }

        // Start the host now so migrations finish before the first test runs.
        _ = Server;
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await base.DisposeAsync();
        if (_container is not null)
        {
            await _container.DisposeAsync();
        }
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:OpenWish", _connectionString);
        builder.UseSetting("OpenWishSettings:OwnDatabaseUpgrades", "true");
        builder.UseSetting("OpenWishSettings:EnableDevelopmentLogin", "true");
        builder.UseSetting("OpenWishSettings:BaseUri", "https://wish.example.test/");

        Register(builder, 0, Studio, "Send pieces from your notebook to OpenWish.");
        Register(builder, 1, Pantry, null);
    }

    public OpenWishBrowser CreateBrowser() => new(CreateClient(new WebApplicationFactoryClientOptions
    {
        AllowAutoRedirect = false,
        HandleCookies = true,
        BaseAddress = new Uri("https://localhost/")
    }));

    public FakeConnectedApp CreateConnectedApp(FakeAppRegistration registration) =>
        new(registration, () => CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = false,
            BaseAddress = new Uri("https://localhost/")
        }));

    private static void Register(IWebHostBuilder builder, int index, FakeAppRegistration app, string? description)
    {
        var prefix = $"OpenWishSettings:Integrations:Apps:{index}:";
        builder.UseSetting(prefix + "ClientId", app.ClientId);
        builder.UseSetting(prefix + "ClientSecret", app.ClientSecret);
        builder.UseSetting(prefix + "DisplayName", app.DisplayName);
        builder.UseSetting(prefix + "RedirectUris:0", app.RedirectUri);
        builder.UseSetting(prefix + "HomepageUrl", new Uri(app.RedirectUri).GetLeftPart(UriPartial.Authority));
        if (description is not null)
        {
            builder.UseSetting(prefix + "Description", description);
        }
    }
}

public sealed record FakeAppRegistration(string ClientId, string DisplayName, string ClientSecret, string RedirectUri);

[CollectionDefinition(Name)]
public sealed class IntegrationCollection : ICollectionFixture<OpenWishAppFactory>
{
    public const string Name = "OpenWish integration host";
}