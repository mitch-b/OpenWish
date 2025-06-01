using Microsoft.AspNetCore.Components.Authorization;
using OpenWish.Shared.Services;
using System.Security.Claims;

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
        try
        {
            var authState = await _authenticationStateProvider.GetAuthenticationStateAsync();
            var user = authState.User;

            return user.FindFirstValue(ClaimTypes.NameIdentifier);
        }
        catch
        {
            // During SSR or if authentication state is not available
            return null;
        }
    }

    public async Task<string?> GetUserNameAsync()
    {
        try
        {
            var authState = await _authenticationStateProvider.GetAuthenticationStateAsync();
            var user = authState.User;

            return user.Identity?.Name;
        }
        catch
        {
            // During SSR or if authentication state is not available
            return null;
        }
    }

    public async Task<bool> IsAuthenticatedAsync()
    {
        try
        {
            var authState = await _authenticationStateProvider.GetAuthenticationStateAsync();
            var user = authState.User;

            return user.Identity?.IsAuthenticated ?? false;
        }
        catch
        {
            // During SSR or if authentication state is not available
            return false;
        }
    }
}