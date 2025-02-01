using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenWish.Application.Models;
using OpenWish.Application.Models.Configuration;
using OpenWish.Application.Services;
using OpenWish.Shared.Services;

namespace OpenWish.Application.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOpenWishApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<OpenWishSettings>(configuration.GetSection(nameof(OpenWishSettings)));

        services.AddScoped<IWishlistService, WishlistService>();
        services.AddScoped<IEventService, EventService>();
        services.AddScoped<IProductService, ProductService>();

        services.AddAutoMapper(typeof(OpenWishProfile).Assembly);

        return services;
    }
}