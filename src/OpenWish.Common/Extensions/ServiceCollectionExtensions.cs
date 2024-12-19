using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenWish.Common.Models.Configuration;

namespace OpenWish.Common.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOpenWishCommonServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<OpenWishSettings>(configuration.GetSection(nameof(OpenWishSettings)));
        return services;
    }
}