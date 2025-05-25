using System.Security.Claims;

namespace OpenWish.Shared.Extensions;

public static class UserExtensions
{
    public static string GetUserId(this ClaimsPrincipal principal) =>
        principal.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
        throw new InvalidOperationException("User ID not found in claims");
}