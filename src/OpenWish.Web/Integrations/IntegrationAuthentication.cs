using System.Globalization;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using OpenWish.Application.Integrations;
using OpenWish.Application.Models.Configuration;
using OpenWish.Data.Entities;
using OpenWish.Shared.Integrations;

namespace OpenWish.Web.Integrations;

public static class IntegrationAuthenticationDefaults
{
    public const string Scheme = "OpenWishIntegration";
    public const string McpScheme = "OpenWishMcp";
    public const string McpPolicy = "OpenWishMcp";
    public const string McpPath = "/mcp";
    public const string ProtectedResourceMetadataPath = "/.well-known/oauth-protected-resource";
    public const string FormActionOriginItem = "openwish:form-action-origin";
    public const string ConnectionIdClaim = "openwish:connection_id";
    public const string ConnectionPublicIdClaim = "openwish:connection";
    public const string ClientIdClaim = "openwish:client_id";
    public const string AppNameClaim = "openwish:app_name";
}

public sealed class IntegrationAuthenticationOptions : AuthenticationSchemeOptions
{
    /// <summary>The only token audience this scheme accepts.</summary>
    public IntegrationAudience Audience { get; set; }
}

/// <summary>
/// Authenticates connected apps by bearer token. Only the integration API and the MCP server opt into
/// these schemes, so an app token never works against the endpoints people use in the browser, and a
/// token issued for one surface never works on the other.
/// </summary>
public sealed class IntegrationAuthenticationHandler(
    IOptionsMonitor<IntegrationAuthenticationOptions> options,
    ILoggerFactory loggerFactory,
    UrlEncoder encoder,
    IIntegrationConnectionService connectionService,
    IOptions<OpenWishSettings> settings)
    : AuthenticationHandler<IntegrationAuthenticationOptions>(options, loggerFactory, encoder)
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
        var caller = await connectionService.AuthenticateAsync(token, Options.Audience);
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
            Scheme.Name);

        return AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name));
    }

    protected override async Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        var authenticateResult = await HandleAuthenticateOnceSafeAsync();
        var presentedToken = authenticateResult.Failure is not null;

        Response.StatusCode = StatusCodes.Status401Unauthorized;
        var challenge = "Bearer realm=\"OpenWish\"";
        if (Options.Audience == IntegrationAudience.Mcp)
        {
            // MCP clients discover the authorization server from this header (RFC 9728 section 5.1).
            challenge += $", resource_metadata=\"{PublicBaseUri.McpResourceMetadata(PublicBaseUri.Resolve(Request, settings.Value.BaseUri))}\", scope=\"{IntegrationScopes.WishlistItems}\"";
        }

        if (presentedToken)
        {
            challenge += ", error=\"invalid_token\"";
        }

        Response.Headers.WWWAuthenticate = challenge;
        await Response.WriteAsJsonAsync(new
        {
            error = presentedToken ? "invalid_token" : "unauthorized",
            error_description = presentedToken
                ? Options.Audience == IntegrationAudience.Mcp
                    ? "The access token is invalid, expired, or was revoked."
                    : "The access token is invalid or was revoked. Ask the person to connect again."
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

    /// <summary>The MCP server's resource identifier (RFC 8707), which MCP clients use as the token audience.</summary>
    public static Uri McpResource(Uri baseUri) => new(baseUri, IntegrationAuthenticationDefaults.McpPath.TrimStart('/'));

    /// <summary>Where MCP clients read the protected resource metadata for <c>/mcp</c> (RFC 9728).</summary>
    public static Uri McpResourceMetadata(Uri baseUri) =>
        new(baseUri, IntegrationAuthenticationDefaults.ProtectedResourceMetadataPath.TrimStart('/') + IntegrationAuthenticationDefaults.McpPath);

    /// <summary>Matches an RFC 8707 resource parameter to a token audience.</summary>
    public static bool TryResolveAudience(string? resource, Uri baseUri, bool mcpEnabled, out IntegrationAudience? audience)
    {
        audience = null;
        if (string.IsNullOrEmpty(resource))
        {
            return true;
        }

        if (!mcpEnabled ||
            !Uri.TryCreate(resource, UriKind.Absolute, out var requested) ||
            !string.IsNullOrEmpty(requested.Query) ||
            !string.IsNullOrEmpty(requested.Fragment) ||
            !string.IsNullOrEmpty(requested.UserInfo))
        {
            return false;
        }

        // Scheme and host are case-insensitive; the path must match exactly (one trailing slash tolerated).
        var expected = McpResource(baseUri);
        if (Uri.Compare(requested, expected, UriComponents.SchemeAndServer, UriFormat.UriEscaped, StringComparison.OrdinalIgnoreCase) == 0 &&
            string.Equals(requested.AbsolutePath.TrimEnd('/'), expected.AbsolutePath.TrimEnd('/'), StringComparison.Ordinal))
        {
            audience = IntegrationAudience.Mcp;
            return true;
        }

        return false;
    }
}