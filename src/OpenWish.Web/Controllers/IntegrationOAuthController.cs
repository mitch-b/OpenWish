using System.Net.Http.Headers;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using OpenWish.Application.Integrations;
using OpenWish.Application.Models.Configuration;
using OpenWish.Shared.Integrations;
using OpenWish.Web.Integrations;

namespace OpenWish.Web.Controllers;

/// <summary>OAuth 2.0 endpoints that connected apps call server to server.</summary>
[ApiController]
[AllowAnonymous]
[IgnoreAntiforgeryToken]
public class IntegrationOAuthController(
    IntegrationAppRegistry registry,
    IIntegrationConnectionService connectionService,
    IOptions<OpenWishSettings> settings) : ControllerBase
{
    public const string TokenRateLimitPolicy = "integration-token";

    [HttpGet(".well-known/oauth-authorization-server")]
    public IActionResult GetMetadata()
    {
        if (!registry.IsEnabled)
        {
            return NotFound();
        }

        var baseUri = PublicBaseUri.Resolve(Request, settings.Value.BaseUri);
        return Ok(new Dictionary<string, object>
        {
            ["issuer"] = baseUri.ToString(),
            ["authorization_endpoint"] = new Uri(baseUri, "oauth/authorize").ToString(),
            ["token_endpoint"] = new Uri(baseUri, "oauth/token").ToString(),
            ["revocation_endpoint"] = new Uri(baseUri, "oauth/revoke").ToString(),
            ["response_types_supported"] = new[] { "code" },
            ["grant_types_supported"] = new[] { "authorization_code" },
            ["code_challenge_methods_supported"] = new[] { "S256" },
            ["token_endpoint_auth_methods_supported"] = new[] { "client_secret_post", "client_secret_basic" },
            ["scopes_supported"] = new[] { IntegrationScopes.WishlistItems },
            ["openwish_api_base"] = new Uri(baseUri, "api/integrations/v1/").ToString()
        });
    }

    [HttpPost("oauth/token")]
    [EnableRateLimiting(TokenRateLimitPolicy)]
    public async Task<IActionResult> ExchangeToken(
        [FromForm(Name = "grant_type")] string? grantType,
        [FromForm(Name = "code")] string? code,
        [FromForm(Name = "redirect_uri")] string? redirectUri,
        [FromForm(Name = "code_verifier")] string? codeVerifier,
        [FromForm(Name = "client_id")] string? clientId,
        [FromForm(Name = "client_secret")] string? clientSecret)
    {
        NoStore();
        var app = AuthenticateClient(clientId, clientSecret, out var usedBasic);
        if (app is null)
        {
            return InvalidClient(usedBasic);
        }

        if (!string.Equals(grantType, "authorization_code", StringComparison.Ordinal))
        {
            return OAuthError("unsupported_grant_type", "Only the authorization_code grant is supported.");
        }

        var result = await connectionService.ExchangeCodeAsync(app, code, redirectUri, codeVerifier);
        if (result.AccessToken is null || result.ConnectionPublicId is null)
        {
            return OAuthError(result.Error ?? "invalid_grant", result.ErrorDescription);
        }

        return Ok(new IntegrationTokenResponse
        {
            AccessToken = result.AccessToken,
            ConnectionId = result.ConnectionPublicId
        });
    }

    [HttpPost("oauth/revoke")]
    [EnableRateLimiting(TokenRateLimitPolicy)]
    public async Task<IActionResult> RevokeToken(
        [FromForm(Name = "token")] string? token,
        [FromForm(Name = "client_id")] string? clientId,
        [FromForm(Name = "client_secret")] string? clientSecret)
    {
        NoStore();
        var app = AuthenticateClient(clientId, clientSecret, out var usedBasic);
        if (app is null)
        {
            return InvalidClient(usedBasic);
        }

        await connectionService.RevokeTokenAsync(app, token);
        return Ok();
    }

    private IntegrationApp? AuthenticateClient(string? formClientId, string? formClientSecret, out bool usedBasic)
    {
        usedBasic = false;
        if (AuthenticationHeaderValue.TryParse(Request.Headers.Authorization, out var header) &&
            string.Equals(header.Scheme, "Basic", StringComparison.OrdinalIgnoreCase))
        {
            usedBasic = true;
            if (!string.IsNullOrEmpty(formClientSecret))
            {
                // RFC 6749 section 2.3: clients must use only one authentication method per request.
                return null;
            }

            try
            {
                var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(header.Parameter ?? string.Empty));
                var separator = decoded.IndexOf(':');
                if (separator <= 0)
                {
                    return null;
                }

                var basicClientId = Uri.UnescapeDataString(decoded[..separator].Replace('+', ' '));
                var basicSecret = Uri.UnescapeDataString(decoded[(separator + 1)..].Replace('+', ' '));
                if (!string.IsNullOrEmpty(formClientId) && formClientId != basicClientId)
                {
                    return null;
                }

                return registry.Authenticate(basicClientId, basicSecret);
            }
            catch (FormatException)
            {
                return null;
            }
        }

        return registry.Authenticate(formClientId, formClientSecret);
    }

    private ObjectResult InvalidClient(bool usedBasic)
    {
        if (usedBasic)
        {
            Response.Headers.WWWAuthenticate = "Basic realm=\"OpenWish\"";
        }

        return StatusCode(StatusCodes.Status401Unauthorized, new IntegrationErrorModel
        {
            Error = "invalid_client",
            ErrorDescription = "The client credentials are not valid."
        });
    }

    private BadRequestObjectResult OAuthError(string error, string? description) =>
        BadRequest(new IntegrationErrorModel { Error = error, ErrorDescription = description });

    private void NoStore()
    {
        Response.Headers.CacheControl = "no-store";
        Response.Headers.Pragma = "no-cache";
    }
}