using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OpenWish.Shared.Services;

namespace OpenWish.Shared.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOpenWishSharedServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.TryAddSingleton<IAppVersionService>(new AppVersionService(configuration["APP_VERSION"] ?? $"{DateTimeOffset.UtcNow.ToLocalTime():yyyy.MM.dd.HHmm}"));
        return services;
    }
}
