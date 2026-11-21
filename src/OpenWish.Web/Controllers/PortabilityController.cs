using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenWish.Application.Services;
using OpenWish.Web.Components.Account;
using OpenWish.Web.Services;

namespace OpenWish.Web.Controllers;

/// <summary>Lets a signed-in person download their own wishlists as JSON and import such a file as new private lists.</summary>
[ApiController]
[Route("api/portability")]
[Authorize]
public class PortabilityController(IWishlistPortabilityService portabilityService, ApiUserContextService userContextService) : ControllerBase
{
    private const string PersonalDataPath = "/Account/Manage/PersonalData";

    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    [HttpGet("wishlists.json")]
    public async Task<IActionResult> ExportWishlists()
    {
        var userId = await userContextService.GetUserIdAsync();
        if (userId is null)
        {
            return Unauthorized();
        }

        var export = await portabilityService.ExportAsync(userId);
        Response.Headers.CacheControl = "no-store";
        return File(
            JsonSerializer.SerializeToUtf8Bytes(export, _jsonOptions),
            "application/json",
            $"openwish-wishlists-{export.ExportedOn:yyyy-MM-dd}.json");
    }

    [HttpPost("wishlists/import")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(WishlistPortabilityService.MaxFileBytes + 4096)]
    public async Task<IActionResult> ImportWishlists(IFormFile? file)
    {
        var userId = await userContextService.GetUserIdAsync();
        if (userId is null)
        {
            return Unauthorized();
        }

        if (file is null || file.Length == 0)
        {
            return Redirect("Error: Choose an OpenWish export file to import.");
        }

        if (file.Length > WishlistPortabilityService.MaxFileBytes)
        {
            return Redirect("Error: That file is too large to import (25 MB at most).");
        }

        try
        {
            await using var stream = file.OpenReadStream();
            var parsed = await portabilityService.ReadAsync(stream);
            var result = await portabilityService.ImportAsync(userId, parsed);
            return Redirect(result.ListsCreated == 0
                ? "Nothing was imported. The file has no lists with a name."
                : $"Imported {Plural(result.ListsCreated, "list")} with {Plural(result.ItemsCreated, "idea")}. " +
                  "They are private until you share them." +
                  (result.ItemsSkipped > 0 ? $" {Plural(result.ItemsSkipped, "idea")} had no name and was skipped." : string.Empty));
        }
        catch (InvalidDataException ex)
        {
            return Redirect($"Error: {ex.Message}");
        }
    }

    private static string Plural(int count, string noun) => count == 1 ? $"1 {noun}" : $"{count} {noun}s";

    private RedirectResult Redirect(string message)
    {
        Response.Cookies.Append(IdentityRedirectManager.StatusCookieName, message, new CookieOptions
        {
            SameSite = SameSiteMode.Strict,
            HttpOnly = true,
            IsEssential = true,
            MaxAge = TimeSpan.FromSeconds(5),
            Secure = Request.IsHttps
        });
        return base.Redirect(PersonalDataPath);
    }
}