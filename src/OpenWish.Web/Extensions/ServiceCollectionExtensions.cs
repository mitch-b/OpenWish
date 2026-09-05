using System.Net.Http.Headers;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OpenWish.Application.Models.Configuration;
using OpenWish.Data.Entities;
using OpenWish.Shared.Services;
using OpenWish.Web.Services;

namespace OpenWish.Web.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOpenWishWebServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpContextAccessor();

        services.TryAddScoped<IWebAssemblyHostEnvironment, ServerHostEnvironment>();

        // Register the application-level email sender and the ASP.NET Identity adapter
        services.AddScoped<IEmailSender<ApplicationUser>, IdentityEmailSenderAdapter>();
        services.AddScoped<IBaseUriService, BaseUriService>();

        services.AddScoped<IUserContextService, UserContextService>();
        services.AddScoped<IReleaseNotesService, ReleaseNotesService>();
        // API controllers (outside Razor context) need to use HttpContextAccessor - Do not call GetAuthenticationStateAsync outside of the DI scope for a Razor component. Typically, this means you can call it only within a Razor component or inside another DI service that is resolved for a Razor component.
        services.AddScoped<ApiUserContextService>();

        // Add OpenAIClient to the service collection
        var apiKey = configuration[$"{nameof(OpenWishSettings)}:{nameof(OpenWishSettings.OpenAI)}:{nameof(OpenAISettings.ApiKey)}"];
        services.AddHttpClient("OpenAI", c =>
        {
            c.BaseAddress = new Uri("https://api.openai.com/v1/");
            c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            c.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        });

        services.AddScoped<IOpenAIService, OpenAIService>();
        services.AddScoped<DevelopmentDataSeeder>();

        return services;
    }
}