using System.Globalization;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using OpenWish.Application.Integrations;

namespace OpenWish.Web.Integrations;

public static class IntegrationAuthenticationDefaults
{
    public const string Scheme = "OpenWishIntegration";
    public const string ConnectionIdClaim = "openwish:connection_id";
    public const string ConnectionPublicIdClaim = "openwish:connection";
    public const string ClientIdClaim = "openwish:client_id";
    public const string AppNameClaim = "openwish:app_name";
}

/// <summary>
/// Authenticates connected apps by bearer token. Only the integration API opts into this scheme,
/// so an app token never works against the endpoints people use in the browser.
/// </summary>
public sealed class IntegrationAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory loggerFactory,
    UrlEncoder encoder,
    IIntegrationConnectionService connectionService)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, loggerFactory, encoder)
{
    private const string BearerPrefix = "Bearer ";

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        string? authorization = Request.Headers.Authorization;
        if (string.IsNullOrEmpty(authorization) ||
            !authorization.StartsWith(BearerPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return AuthenticateResult.NoResult();
        }

        var token = authorization[BearerPrefix.Length..].Trim();
        var caller = await connectionService.AuthenticateAsync(token);
        if (caller is null)
        {
            return AuthenticateResult.Fail("The access token is invalid or was revoked.");
        }

        var identity = new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, caller.UserId),
                new Claim(IntegrationAuthenticationDefaults.ConnectionIdClaim, caller.ConnectionId.ToString(CultureInfo.InvariantCulture)),
                new Claim(IntegrationAuthenticationDefaults.ConnectionPublicIdClaim, caller.ConnectionPublicId),
                new Claim(IntegrationAuthenticationDefaults.ClientIdClaim, caller.ClientId),
                new Claim(IntegrationAuthenticationDefaults.AppNameClaim, caller.AppName)
            ],
            IntegrationAuthenticationDefaults.Scheme);

        return AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name));
    }

    protected override async Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        var authenticateResult = await HandleAuthenticateOnceSafeAsync();
        var presentedToken = authenticateResult.Failure is not null;

        Response.StatusCode = StatusCodes.Status401Unauthorized;
        Response.Headers.WWWAuthenticate = presentedToken
            ? "Bearer realm=\"OpenWish\", error=\"invalid_token\""
            : "Bearer realm=\"OpenWish\"";
        await Response.WriteAsJsonAsync(new
        {
            error = presentedToken ? "invalid_token" : "unauthorized",
            error_description = presentedToken
                ? "The access token is invalid or was revoked. Ask the person to connect again."
                : "Send the access token in an Authorization: Bearer header."
        });
    }

    protected override async Task HandleForbiddenAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = StatusCodes.Status403Forbidden;
        await Response.WriteAsJsonAsync(new { error = "forbidden" });
    }
}

public static class IntegrationClaimsPrincipalExtensions
{
    public static IntegrationCaller? GetIntegrationCaller(this ClaimsPrincipal principal)
    {
        var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        var connectionId = principal.FindFirstValue(IntegrationAuthenticationDefaults.ConnectionIdClaim);
        var connectionPublicId = principal.FindFirstValue(IntegrationAuthenticationDefaults.ConnectionPublicIdClaim);
        var clientId = principal.FindFirstValue(IntegrationAuthenticationDefaults.ClientIdClaim);
        var appName = principal.FindFirstValue(IntegrationAuthenticationDefaults.AppNameClaim);

        return userId is null ||
            connectionPublicId is null ||
            clientId is null ||
            appName is null ||
            !int.TryParse(connectionId, NumberStyles.None, CultureInfo.InvariantCulture, out var id)
            ? null
            : new IntegrationCaller(id, connectionPublicId, userId, clientId, appName);
    }
}

public static class PublicBaseUri
{
    /// <summary>The address people use to reach OpenWish, for links handed to connected apps.</summary>
    public static Uri Resolve(HttpRequest request, string? configuredBaseUri)
    {
        var value = !string.IsNullOrWhiteSpace(configuredBaseUri)
            ? configuredBaseUri.Trim()
            : $"{request.Scheme}://{request.Host}{request.PathBase}";

        return new Uri(value.EndsWith('/') ? value : value + "/", UriKind.Absolute);
    }
}