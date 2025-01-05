using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using OpenWish.Shared.Extensions;
using OpenWish.Web.Client.Extensions;
using OpenWish.Web.Client.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// Configure logging
builder.Logging.SetMinimumLevel(LogLevel.Debug);

builder.Services.AddAuthorizationCore();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddAuthenticationStateDeserialization();
builder.Services.AddApiAuthorization();

// Register HttpClient with authorization
builder.Services.AddHttpClient("OpenWish.API", client => client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress))
    .AddHttpMessageHandler<BaseAddressAuthorizationMessageHandler>();

builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("OpenWish.API"));

builder.Services.AddOpenWishSharedServices(builder.Configuration);
builder.Services.AddOpenWishWasmClientServices(builder.Configuration);

await builder.Build().RunAsync();
