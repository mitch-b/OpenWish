using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenWish.Application.Models.Configuration;

namespace OpenWish.Application.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOpenWishCommonServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<OpenWishSettings>(configuration.GetSection(nameof(OpenWishSettings)));
        return services;
    }
}