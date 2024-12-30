using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;

namespace OpenWish.Web.Services;

public interface IUserContextService
{
    Task<string?> GetUserIdAsync();
    Task<string> GetUserNameAsync();
    Task<bool> IsAuthenticatedAsync();
}

public class UserContextService(AuthenticationStateProvider authStateProvider) : IUserContextService
{
    private readonly AuthenticationStateProvider _authStateProvider = authStateProvider;

    public async Task<string> GetUserNameAsync()
    {
        var state = await _authStateProvider.GetAuthenticationStateAsync();
        return state.User.FindFirstValue(ClaimTypes.Name) 
            ?? throw new InvalidOperationException("Username not found");
    }

    public async Task<bool> IsAuthenticatedAsync()
    {
        var state = await _authStateProvider.GetAuthenticationStateAsync();
        return state.User.Identity?.IsAuthenticated ?? false;
    }

    public async Task<string?> GetUserIdAsync()
    {
        var state = await _authStateProvider.GetAuthenticationStateAsync();
        return state.User.FindFirstValue(ClaimTypes.NameIdentifier);
    }
}