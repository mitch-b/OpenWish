using OpenWish.Shared.Services;
using OpenWish.Web.Client.Services;

namespace OpenWish.Web.Client.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOpenWishWasmClientServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IUserContextService, UserContextService>();
        services.AddScoped<IWishlistService, WishlistHttpClientService>();
        services.AddScoped<IEventService, EventHttpClientService>();
        return services;
    }
}
