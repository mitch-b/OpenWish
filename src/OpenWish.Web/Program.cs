using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Options;
using OpenWish.Application.Extensions;
using OpenWish.Application.Models.Configuration;
using OpenWish.Data;
using OpenWish.Data.Entities;
using OpenWish.Shared.Extensions;
using OpenWish.Web.Components;
using OpenWish.Web.Components.Account;
using OpenWish.Web.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

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

var connectionString = builder.Configuration.GetConnectionString("OpenWish")
    ?? throw new InvalidOperationException("Connection string 'OpenWish' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString)
        // HMM... https://github.com/dotnet/efcore/issues/34431
        .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning))
        .EnableSensitiveDataLogging()
    );

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddIdentityCore<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder.Services.AddOpenWishApplicationServices(builder.Configuration);
builder.Services.AddOpenWishSharedServices(builder.Configuration);
builder.Services.AddOpenWishWebServices();

using (var provider = builder.Services.BuildServiceProvider())
{
    var configuration = provider.GetRequiredService<IConfiguration>();
    Console.WriteLine("Configuration: " + (configuration as IConfigurationRoot).GetDebugView());
    
    var openWishSettings = provider.GetRequiredService<IOptions<OpenWishSettings>>()?.Value
        ?? throw new InvalidOperationException("OpenWishSettings not found.");
        
    // setup email from configuration
    if (!string.IsNullOrWhiteSpace(openWishSettings?.EmailConfig?.SmtpFrom)
        && !string.IsNullOrWhiteSpace(openWishSettings?.EmailConfig?.SmtpHost))
    {
        builder.Services
            .AddFluentEmail(openWishSettings?.EmailConfig?.SmtpFrom)
            .AddSmtpSender(
                openWishSettings?.EmailConfig?.SmtpHost, 
                openWishSettings?.EmailConfig?.SmtpPort ?? 587,
                openWishSettings?.EmailConfig?.SmtpUser,
                openWishSettings?.EmailConfig?.SmtpPass);
    }
    else
    {
        Console.WriteLine("Full email configuration not found. Email will not work.");
        // register service so Services that depend on IFluentEmail can be registered
        builder.Services
            .AddFluentEmail(openWishSettings?.EmailConfig?.SmtpFrom);
    }
}

builder.Services.AddControllers();

var app = builder.Build();

// // fix Codespaces thinking Navigation BaseUri was localhost
var forwardingOptions = new ForwardedHeadersOptions()
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
};

forwardingOptions.KnownNetworks.Clear();
forwardingOptions.KnownProxies.Clear();

app.UseForwardedHeaders(forwardingOptions);

// Apply migrations on startup
using (var scope = app.Services.CreateScope())
{
    var openWishSettings = scope.ServiceProvider.GetRequiredService<IOptions<OpenWishSettings>>()?.Value
        ?? throw new InvalidOperationException("OpenWishSettings not found.");
    if (openWishSettings.OwnDatabaseUpgrades)
    {
        await using var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var waitSeconds = 3;
        Console.WriteLine($"Applying migrations after {waitSeconds} seconds...");
        await Task.Delay(TimeSpan.FromSeconds(waitSeconds));
        await db.Database.MigrateAsync();
    }
    else
    {
        Console.WriteLine("Skipping migrations...");
    }
}

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

app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(OpenWish.Web.Client._Imports).Assembly);

// Add additional endpoints required by the Identity /Account Razor components.
app.MapAdditionalIdentityEndpoints();

app.MapControllers();

app.Run();
