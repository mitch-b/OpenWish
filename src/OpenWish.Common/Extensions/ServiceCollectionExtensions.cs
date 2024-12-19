using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenWish.Common.Models.Configuration;
using OpenWish.Common.Services;

namespace OpenWish.Common.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMaintenanceLogCommonServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<OpenWishSettings>(configuration.GetSection(nameof(OpenWishSettings)));
        services.AddScoped<IDatabaseConfigurationService, DatabaseConfigurationService>();
        return services;
    }
}