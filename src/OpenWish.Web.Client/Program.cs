using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using OpenWish.Shared.Extensions;
using OpenWish.Web.Client.Extensions;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddAuthorizationCore();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddAuthenticationStateDeserialization();

builder.Services.AddHttpClient("OpenWish.API", client => client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress));

builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("OpenWish.API"));

builder.Services.AddOpenWishSharedServices(builder.Configuration);
builder.Services.AddOpenWishWasmClientServices(builder.Configuration);

await builder.Build().RunAsync();