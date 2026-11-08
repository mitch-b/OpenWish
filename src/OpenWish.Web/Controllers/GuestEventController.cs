using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OpenWish.Application.Services;
using OpenWish.Web.Services;

namespace OpenWish.Web.Controllers;

[AllowAnonymous]
[Route("guest")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[EnableRateLimiting("guest-access")]
public sealed class GuestEventController(GuestEventService service, ILogger<GuestEventController> logger) : Controller
{
    [HttpPost("access")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Access([FromForm] string? token)
    {
        var session = token is null ? null : await service.RedeemAsync(token);
        if (session is null)
        {
            return Redirect("/guest?error=unavailable");
        }
        await HttpContext.SignInAsync(GuestEventAuthentication.Scheme,
            GuestEventAuthentication.CreatePrincipal(session),
            new AuthenticationProperties { IsPersistent = false, ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8) });
        return Redirect("/guest");
    }

    [HttpPost("respond")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Respond([FromForm] string? displayName, [FromForm] string response)
    {
        if (response is not ("accept" or "decline"))
        {
            return Redirect("/guest?error=response");
        }
        var session = await GuestEventAuthentication.GetSessionAsync(HttpContext);
        if (session is null)
        {
            return Redirect("/guest?error=unavailable");
        }
        try
        {
            await service.RespondAsync(session, displayName, response == "accept");
            if (response == "decline")
            {
                await HttpContext.SignOutAsync(GuestEventAuthentication.Scheme);
                return Redirect("/guest?declined=true");
            }
            return Redirect("/guest?accepted=true");
        }
        catch (ArgumentException)
        {
            return Redirect("/guest?error=name");
        }
        catch (UnauthorizedAccessException)
        {
            return Redirect("/guest?error=unavailable");
        }
        catch (InvalidOperationException)
        {
            return Redirect("/guest?error=changed");
        }
    }

    [HttpPost("open")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Open([FromForm] string assignmentPublicId)
    {
        var session = await GuestEventAuthentication.GetSessionAsync(HttpContext);
        if (session is null)
        {
            return Redirect("/guest?error=unavailable");
        }
        try
        {
            await service.OpenMatchAsync(session, assignmentPublicId);
            return Redirect("/guest?opened=true#guest-match-name");
        }
        catch (UnauthorizedAccessException)
        {
            return Redirect("/guest?error=unavailable");
        }
        catch (InvalidOperationException)
        {
            return Redirect("/guest?error=changed");
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "Guest match opening could not be completed.");
            return Redirect("/guest?error=retry");
        }
    }

    [HttpPost("leave")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Leave()
    {
        await HttpContext.SignOutAsync(GuestEventAuthentication.Scheme);
        return Redirect("/guest");
    }

    [HttpPost("account")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Account()
    {
        var session = await GuestEventAuthentication.GetSessionAsync(HttpContext);
        if (session is null)
        {
            return Redirect("/guest?error=unavailable");
        }
        try
        {
            return Redirect(await service.GetAccountRegistrationUrlAsync(session));
        }
        catch (UnauthorizedAccessException)
        {
            return Redirect("/guest?error=unavailable");
        }
    }
}