using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
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

        using (var provider = services.BuildServiceProvider())
        {
            var openWishSettings = provider.GetRequiredService<IOptions<OpenWishSettings>>()?.Value
                ?? throw new InvalidOperationException("OpenWishSettings not found.");

            // setup email from configuration
            if (!string.IsNullOrWhiteSpace(openWishSettings?.EmailConfig?.SmtpFrom)
                && !string.IsNullOrWhiteSpace(openWishSettings?.EmailConfig?.SmtpHost))
            {
                services
                    .AddFluentEmail(openWishSettings?.EmailConfig?.SmtpFrom)
                    .AddSmtpSender(
                        openWishSettings?.EmailConfig?.SmtpHost,
                        openWishSettings?.EmailConfig?.SmtpPort ?? 587,
                        openWishSettings?.EmailConfig?.SmtpUser,
                        openWishSettings?.EmailConfig?.SmtpPass);
            }
            else
            {
                Console.WriteLine("Full email configuration not found. Email will not work.");
                // register service so Services that depend on IFluentEmail can be registered
                services.AddFluentEmail(openWishSettings?.EmailConfig?.SmtpFrom);
            }
        }

        services.AddScoped<IWishlistService, WishlistService>();
        services.AddScoped<IEventService, EventService>();

        services.AddAutoMapper(typeof(OpenWishProfile).Assembly);

        return services;
    }
}