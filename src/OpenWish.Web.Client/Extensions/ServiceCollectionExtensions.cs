using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OpenWish.Shared.Services;
using OpenWish.Web.Client.Services;

namespace OpenWish.Web.Client.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOpenWishWasmClientServices(this IServiceCollection services, IConfiguration configuration)
    {
        //services.RemoveAll<AuthenticationStateProvider>();
        // https://jonhilton.net/blazor-share-auth-state/
        // services.TryAddScoped<AuthenticationStateProvider, PersistentAuthenticationStateProvider>();

        services.AddScoped<IUserContextService, UserContextService>();
        services.AddScoped<IWishlistService, WishlistHttpClientService>();
        services.AddScoped<IEventService, EventHttpClientService>();
        return services;
    }
}
