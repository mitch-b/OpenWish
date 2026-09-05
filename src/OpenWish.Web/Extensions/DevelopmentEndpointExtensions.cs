using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using OpenWish.Application.Models.Configuration;
using OpenWish.Data.Entities;

namespace OpenWish.Web.Extensions;

public static class DevelopmentEndpointExtensions
{
    private static readonly IReadOnlyDictionary<string, string> _developmentUsers =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["owner"] = "playwright-owner@openwish.local",
            ["guest"] = "playwright-guest@openwish.local"
        };

    public static void MapOpenWishDevelopmentEndpoints(this WebApplication app)
    {
        var settings = app.Services.GetRequiredService<IOptions<OpenWishSettings>>().Value;
        if (!app.Environment.IsDevelopment() || !settings.EnableDevelopmentLogin)
        {
            return;
        }

        app.MapPost("/auth/dev-login", async (
            string? persona,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager) =>
        {
            var selectedPersona = string.IsNullOrWhiteSpace(persona) ? "owner" : persona;
            if (!_developmentUsers.TryGetValue(selectedPersona, out var email))
            {
                return Results.BadRequest(new { error = "Unknown development persona." });
            }

            var user = await userManager.FindByEmailAsync(email);
            if (user is null)
            {
                user = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(user);
                if (!result.Succeeded)
                {
                    return Results.ValidationProblem(result.Errors.ToDictionary(
                        error => error.Code,
                        error => new[] { error.Description }));
                }
            }

            await signInManager.SignInAsync(user, isPersistent: false);
            return Results.Ok(new { persona = selectedPersona, user.Email });
        })
        .AllowAnonymous()
        .DisableAntiforgery();
    }
}