using System;
using System.IO;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Threading.RateLimiting;
using FluentEmail.Core;
using FluentEmail.Smtp;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using OpenWish.Application.Extensions;
using OpenWish.Application.Models.Configuration;
using OpenWish.Data;
using OpenWish.Data.Entities;
using OpenWish.Shared.Extensions;
using OpenWish.Web.Components;
using OpenWish.Web.Components.Account;
using OpenWish.Web.Extensions;
using OpenWish.Web.Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
var certificateLoaded = configureTls(builder);
var useForwardedHeaders = configureForwardedHeaders(builder);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents()
    .AddAuthenticationStateSerialization(options => options.SerializeAllClaims = true);

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityUserAccessor>();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, PersistingAuthenticationStateProvider>();
// for InteractiveServer only: builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = IdentityConstants.ApplicationScheme;
        options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
    })
    .AddIdentityCookies();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
        ? CookieSecurePolicy.SameAsRequest
        : CookieSecurePolicy.Always;
    options.SlidingExpiration = true;
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
});

var googleClientId = builder.Configuration.GetValue<string>("Authentication:Google:ClientId");
var googleClientSecret = builder.Configuration.GetValue<string>("Authentication:Google:ClientSecret");
var formActionSources = "'self'";
if (!string.IsNullOrWhiteSpace(googleClientId) &&
    !string.IsNullOrWhiteSpace(googleClientSecret))
{
    // Browsers also enforce form-action on redirects from an OAuth challenge.
    formActionSources += " https://accounts.google.com";
    builder.Services.AddAuthentication().AddGoogle(googleOptions =>
    {
        googleOptions.ClientId = googleClientId;
        googleOptions.ClientSecret = googleClientSecret;
    });
}

var connectionString = builder.Configuration.GetConnectionString("OpenWish")
    ?? throw new InvalidOperationException("Connection string 'OpenWish' not found.");

builder.Services.AddDbContextFactory<ApplicationDbContext>(options =>
{
    var dbOptions = options.UseNpgsql(connectionString)
        // HMM... https://github.com/dotnet/efcore/issues/34431
        .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning));

    if (builder.Environment.IsDevelopment() &&
        builder.Configuration.GetValue<bool>("Database:EnableSensitiveDataLogging"))
    {
        dbOptions.EnableSensitiveDataLogging();
    }
});

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddIdentityCore<ApplicationUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = true;
        options.Lockout.AllowedForNewUsers = true;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder.Services.AddOpenWishApplicationServices(builder.Configuration);
builder.Services.AddOpenWishSharedServices(builder.Configuration);
builder.Services.AddOpenWishWebServices(builder.Configuration);
builder.Services.AddHostedService<DatabaseMigrationHostedService>();

// Email configuration without building a secondary service provider
var openWishSettings = builder.Configuration.GetSection(nameof(OpenWishSettings)).Get<OpenWishSettings>()
    ?? throw new InvalidOperationException("OpenWishSettings not found.");

if (!string.IsNullOrWhiteSpace(openWishSettings.EmailConfig?.SmtpFrom) &&
    !string.IsNullOrWhiteSpace(openWishSettings.EmailConfig?.SmtpHost))
{
    builder.Services
        .AddFluentEmail(openWishSettings.EmailConfig.SmtpFrom)
        .AddSmtpSender(
            openWishSettings.EmailConfig.SmtpHost,
            openWishSettings.EmailConfig.SmtpPort ?? 587,
            openWishSettings.EmailConfig.SmtpUser,
            openWishSettings.EmailConfig.SmtpPass);
}
else
{
    Console.WriteLine("Full email configuration not found. Email will not work.");
    builder.Services.AddFluentEmail(openWishSettings.EmailConfig?.SmtpFrom ?? "no-reply@openwish.local");
}

builder.Services.AddControllers();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("authentication", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
    options.AddPolicy("invitations", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ??
                context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 20,
                Window = TimeSpan.FromMinutes(10),
                QueueLimit = 0
            }));
    options.AddPolicy("product-scrape", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ??
                context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
});

var app = builder.Build();
// Migrations now handled by DatabaseMigrationHostedService.

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

if (useForwardedHeaders)
{
    app.UseForwardedHeaders();
}

if (certificateLoaded || app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
else
{
    Console.WriteLine("HTTPS redirection disabled. No TLS certificate configured.");
}

app.Use(async (context, next) =>
{
    context.Response.Headers.XContentTypeOptions = "nosniff";
    context.Response.Headers["Referrer-Policy"] = "no-referrer";
    context.Response.Headers.XFrameOptions = "DENY";
    context.Response.Headers.ContentSecurityPolicy =
        "default-src 'self'; base-uri 'self'; object-src 'none'; frame-ancestors 'none'; " +
        $"form-action {formActionSources}; script-src 'self' 'wasm-unsafe-eval'; style-src 'self' 'unsafe-inline'; " +
        "img-src 'self' data: https: http:; font-src 'self' data:; connect-src 'self' ws: wss:";
    context.Response.Headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
    await next();
});
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();
app.UseRateLimiter();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(OpenWish.Web.Client._Imports).Assembly);

// Add additional endpoints required by the Identity /Account Razor components.
app.MapAdditionalIdentityEndpoints();
app.MapOpenWishDevelopmentEndpoints();

app.MapControllers();

app.Run();

static bool configureTls(WebApplicationBuilder builder)
{
    var configuration = builder.Configuration;
    var certificatePath = configuration["Tls:CertificatePath"];
    var certificatePassword = configuration["Tls:CertificatePassword"];

    X509Certificate2? certificate = null;

    if (!string.IsNullOrWhiteSpace(certificatePath))
    {
        if (File.Exists(certificatePath))
        {
            try
            {
                certificate = string.IsNullOrWhiteSpace(certificatePassword)
                    ? X509CertificateLoader.LoadPkcs12FromFile(certificatePath, ReadOnlySpan<char>.Empty)
                    : X509CertificateLoader.LoadPkcs12FromFile(certificatePath, certificatePassword.AsSpan());
                Console.WriteLine($"TLS certificate loaded from '{certificatePath}'.");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Failed to load the configured TLS certificate from '{certificatePath}'.",
                    ex);
            }
        }
        else
        {
            throw new FileNotFoundException("The configured TLS certificate was not found.", certificatePath);
        }
    }

    var runningInContainer = string.Equals(Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER"), "true", StringComparison.OrdinalIgnoreCase);
    var httpPortSetting = configuration.GetValue<int?>("Tls:HttpPort");
    var httpsPortSetting = configuration.GetValue<int?>("Tls:HttpsPort");

    var httpPort = httpPortSetting ?? (runningInContainer ? 8080 : 5000);
    var httpsPort = httpsPortSetting ?? (runningInContainer ? 8443 : 5001);

    var shouldConfigureKestrel = runningInContainer || certificate is not null || httpPortSetting.HasValue || httpsPortSetting.HasValue;

    if (shouldConfigureKestrel)
    {
        builder.WebHost.ConfigureKestrel(options =>
        {
            options.ListenAnyIP(httpPort);

            if (certificate is not null)
            {
                options.ListenAnyIP(httpsPort, listenOptions => listenOptions.UseHttps(certificate));
            }
        });
    }

    return certificate is not null;
}

static bool configureForwardedHeaders(WebApplicationBuilder builder)
{
    var configuration = builder.Configuration;
    var knownProxies = configuration.GetSection("ForwardedHeaders:KnownProxies").Get<string[]>() ?? [];
    var knownNetworks = configuration.GetSection("ForwardedHeaders:KnownNetworks").Get<string[]>() ?? [];
    var validProxyCount = knownProxies.Count(proxy => IPAddress.TryParse(proxy, out _));
    var validNetworkCount = knownNetworks.Count(network => System.Net.IPNetwork.TryParse(network, out _));
    if (validProxyCount + validNetworkCount == 0)
    {
        Console.WriteLine("Forwarded headers disabled. Configure trusted proxies or networks to enable them.");
        return false;
    }

    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        options.KnownIPNetworks.Clear();
        options.KnownProxies.Clear();
        options.ForwardLimit = 1;

        foreach (var proxy in knownProxies)
        {
            if (IPAddress.TryParse(proxy, out var address))
            {
                options.KnownProxies.Add(address);
            }
            else if (!string.IsNullOrWhiteSpace(proxy))
            {
                Console.WriteLine($"Unable to parse forwarded header proxy '{proxy}'.");
            }
        }

        foreach (var network in knownNetworks)
        {
            if (System.Net.IPNetwork.TryParse(network, out var parsedNetwork))
            {
                options.KnownIPNetworks.Add(parsedNetwork);
            }
            else if (!string.IsNullOrWhiteSpace(network))
            {
                Console.WriteLine($"Unable to parse forwarded header network '{network}'. Expected CIDR notation (e.g. 103.21.244.0/22).");
            }
        }
    });

    return true;
}