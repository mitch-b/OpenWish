using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace OpenWish.Shared.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOpenWishSharedServices(this IServiceCollection services, IConfiguration configuration)
    {
        return services;
    }
}
