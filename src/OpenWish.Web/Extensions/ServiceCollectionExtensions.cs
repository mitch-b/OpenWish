using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using OpenWish.Application.Models.Configuration;
using OpenWish.Data.Entities;
using OpenWish.Shared.Services;
using OpenWish.Web.Services;
using System.Net.Http.Headers;

namespace OpenWish.Web.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOpenWishWebServices(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();

        services.TryAddScoped<IWebAssemblyHostEnvironment, ServerHostEnvironment>();

        // Register the application-level email sender and the ASP.NET Identity adapter
        services.AddScoped<IEmailSender<ApplicationUser>, IdentityEmailSenderAdapter>();
        services.AddScoped<IBaseUriService, BaseUriService>();

        services.AddScoped<IUserContextService, UserContextService>();
        // API controllers (outside Razor context) need to use HttpContextAccessor - Do not call GetAuthenticationStateAsync outside of the DI scope for a Razor component. Typically, this means you can call it only within a Razor component or inside another DI service that is resolved for a Razor component.
        services.AddScoped<ApiUserContextService>();

        // Add OpenAIClient to the service collection
        var apiKey = services.BuildServiceProvider().GetRequiredService<IOptions<OpenWishSettings>>().Value?.OpenAI.ApiKey;
        services.AddHttpClient("OpenAI", c =>
        {
            c.BaseAddress = new Uri("https://api.openai.com/v1/");
            c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            c.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        });

        services.AddScoped<IOpenAIService, OpenAIService>();

        return services;
    }
}