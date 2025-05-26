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
        services.AddScoped<IProductService, ProductHttpClientService>();

        // Social features
        services.AddScoped<IFriendService, FriendHttpClientService>();
        services.AddScoped<INotificationService, NotificationHttpClientService>();
        services.AddScoped<IActivityService, ActivityHttpClientService>();

        return services;
    }
}