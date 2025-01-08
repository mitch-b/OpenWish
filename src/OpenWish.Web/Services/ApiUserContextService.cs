using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using OpenWish.Shared.Services;

namespace OpenWish.Web.Services;

public class ApiUserContextService : IUserContextService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ApiUserContextService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Task<string?> GetUserIdAsync()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        return Task.FromResult(user?.FindFirstValue(ClaimTypes.NameIdentifier));
    }

    public Task<string?> GetUserNameAsync()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        return Task.FromResult(user?.Identity?.Name);
    }

    public Task<bool> IsAuthenticatedAsync()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        return Task.FromResult(user?.Identity?.IsAuthenticated ?? false);
    }
}