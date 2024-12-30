using System.Security.Claims;

namespace OpenWish.Application.Extensions;

public static class UserExtensions
{
    public static string GetUserId(this ClaimsPrincipal principal) =>
        principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? 
        throw new InvalidOperationException("User ID not found in claims");
}