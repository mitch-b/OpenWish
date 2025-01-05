using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;
using OpenWish.Shared.Services;

namespace OpenWish.Web.Services;

public class UserContextService : IUserContextService
{
    private readonly AuthenticationStateProvider _authenticationStateProvider;

    public UserContextService(AuthenticationStateProvider authenticationStateProvider)
    {
        _authenticationStateProvider = authenticationStateProvider;
    }

    public async Task<string?> GetUserIdAsync()
    {
        var authState = await _authenticationStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;

        return user.FindFirstValue(ClaimTypes.NameIdentifier);
    }

    public async Task<string?> GetUserNameAsync()
    {
        var authState = await _authenticationStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;

        return user.Identity?.Name;
    }

    public async Task<bool> IsAuthenticatedAsync()
    {
        var authState = await _authenticationStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;

        return user.Identity?.IsAuthenticated ?? false;
    }
}
