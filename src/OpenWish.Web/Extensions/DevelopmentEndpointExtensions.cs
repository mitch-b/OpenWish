using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using OpenWish.Application.Models.Configuration;
using OpenWish.Data.Entities;
using OpenWish.Web.Services;

namespace OpenWish.Web.Extensions;

public static class DevelopmentEndpointExtensions
{
    public static void MapOpenWishDevelopmentEndpoints(this WebApplication app)
    {
        var settings = app.Services.GetRequiredService<IOptions<OpenWishSettings>>().Value;
        if (!app.Environment.IsDevelopment() || !settings.EnableDevelopmentLogin)
        {
            return;
        }

        app.MapPost("/auth/dev-login", async (
            string? persona,
            DevelopmentDataSeeder seeder,
            SignInManager<ApplicationUser> signInManager) =>
        {
            var user = await seeder.EnsureUserAsync(persona);
            if (user is null)
            {
                return Results.BadRequest(new { error = "Unknown development persona." });
            }

            await signInManager.SignInAsync(user, isPersistent: false);
            return Results.Ok(new { persona = persona ?? "owner", user.Email });
        })
        .AllowAnonymous()
        .DisableAntiforgery();

        app.MapPost("/auth/dev-seed", async (
            HttpContext httpContext,
            DevelopmentDataSeeder seeder,
            UserManager<ApplicationUser> userManager,
            CancellationToken cancellationToken) =>
        {
            var owner = await seeder.EnsureUserAsync("owner");
            var currentUserId = userManager.GetUserId(httpContext.User);
            if (owner is null || !string.Equals(currentUserId, owner.Id, StringComparison.Ordinal))
            {
                return Results.StatusCode(StatusCodes.Status403Forbidden);
            }

            return Results.Ok(await seeder.SeedAsync(cancellationToken));
        })
        .RequireAuthorization()
        .DisableAntiforgery();
    }
}