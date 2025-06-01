using Microsoft.AspNetCore.Components.Authorization;
using OpenWish.Shared.Services;
using System.Security.Claims;

namespace OpenWish.Web.Client.Services;

public class UserContextService(AuthenticationStateProvider authenticationStateProvider) : IUserContextService
{
    private readonly AuthenticationStateProvider _authenticationStateProvider = authenticationStateProvider;

    public async Task<string?> GetUserIdAsync()
    {
        try
        {
            var authState = await _authenticationStateProvider.GetAuthenticationStateAsync();
            return authState.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
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
            return authState.User.Identity?.Name;
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
            return authState.User.Identity?.IsAuthenticated ?? false;
        }
        catch
        {
            // During SSR or if authentication state is not available
            return false;
        }
    }
}