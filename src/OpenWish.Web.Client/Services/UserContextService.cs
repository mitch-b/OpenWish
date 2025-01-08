using Microsoft.AspNetCore.Components.Authorization;
using OpenWish.Shared.Services;
using System.Security.Claims;

namespace OpenWish.Web.Client.Services;

public class UserContextService(AuthenticationStateProvider authenticationStateProvider) : IUserContextService
{
    private readonly AuthenticationStateProvider _authenticationStateProvider = authenticationStateProvider;

    public async Task<string?> GetUserIdAsync()
    {
        var authState = await _authenticationStateProvider.GetAuthenticationStateAsync();
        return authState.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }

    public async Task<string?> GetUserNameAsync()
    {
        var authState = await _authenticationStateProvider.GetAuthenticationStateAsync();
        return authState.User.Identity?.Name;
    }

    public async Task<bool> IsAuthenticatedAsync()
    {
        var authState = await _authenticationStateProvider.GetAuthenticationStateAsync();
        return authState.User.Identity?.IsAuthenticated ?? false;
    }
}
