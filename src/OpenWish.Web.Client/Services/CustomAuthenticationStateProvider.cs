using Microsoft.AspNetCore.Components.Authorization;
using System.Net.Http.Json;
using System.Security.Claims;

namespace OpenWish.Web.Client.Services;

public class CustomAuthenticationStateProvider(HttpClient httpClient) : AuthenticationStateProvider
{
    private readonly HttpClient _httpClient = httpClient;

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var user = await _httpClient.GetFromJsonAsync<ApplicationUser>("api/account/user");

        if (user is null)
        {
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
        }

        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Name, user.UserName),
            new Claim(ClaimTypes.Email, user.Email)
        }, "serverAuth");

        var userPrincipal = new ClaimsPrincipal(identity);

        return new AuthenticationState(userPrincipal);
    }

    public void NotifyUserAuthentication(ApplicationUser user)
    {
        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Name, user.UserName),
            new Claim(ClaimTypes.Email, user.Email)
        }, "serverAuth");

        var userPrincipal = new ClaimsPrincipal(identity);

        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(userPrincipal)));
    }

    public void NotifyUserLogout()
    {
        var anonymousUser = new ClaimsPrincipal(new ClaimsIdentity());
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(anonymousUser)));
    }
}

public record ApplicationUser
{
    public string UserName { get; set; }
    public string Email { get; set; }
}
