using System.Net.Http.Headers;
using OpenWish.Application.Models.Configuration;
using OpenWish.Data.Entities;
using OpenWish.Web.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using OpenWish.Shared.Services;

namespace OpenWish.Web.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOpenWishWebServices(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        
        services.AddScoped<IEmailSender<ApplicationUser>, OpenWishEmailSender>();
        services.AddScoped<IBaseUriService, BaseUriService>();

        services.AddScoped<IUserContextService, UserContextService>();
        
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