using System.Net.Http.Headers;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using OpenWish.Application.Integrations;
using OpenWish.Application.Models.Configuration;
using OpenWish.Data.Entities;
using OpenWish.Shared.Integrations;
using OpenWish.Web.Integrations;

namespace OpenWish.Web.Controllers;

/// <summary>
/// OAuth 2.0 endpoints that connected apps call server to server, plus the discovery and dynamic
/// client registration endpoints that MCP clients use.
/// </summary>
[ApiController]
[AllowAnonymous]
[IgnoreAntiforgeryToken]
public class IntegrationOAuthController(
    IntegrationAppRegistry registry,
    IIntegrationClientStore clientStore,
    IIntegrationConnectionService connectionService,
    IOptions<OpenWishSettings> settings) : ControllerBase
{
    public const string TokenRateLimitPolicy = "integration-token";
    public const string RegisterRateLimitPolicy = "integration-register";

    [HttpGet(".well-known/oauth-authorization-server")]
    public IActionResult GetMetadata()
    {
        if (!registry.IsEnabled && !clientStore.IsMcpEnabled)
        {
            return NotFound();
        }

        var baseUri = PublicBaseUri.Resolve(Request, settings.Value.BaseUri);
        var metadata = new Dictionary<string, object>
        {
            ["issuer"] = baseUri.ToString(),
            ["authorization_endpoint"] = new Uri(baseUri, "oauth/authorize").ToString(),
            ["token_endpoint"] = new Uri(baseUri, "oauth/token").ToString(),
            ["revocation_endpoint"] = new Uri(baseUri, "oauth/revoke").ToString(),
            ["response_types_supported"] = new[] { "code" },
            ["response_modes_supported"] = new[] { "query" },
            ["grant_types_supported"] = new[] { "authorization_code" },
            ["code_challenge_methods_supported"] = new[] { "S256" },
            ["token_endpoint_auth_methods_supported"] = new[] { ClientAuthMethods.ClientSecretPost, ClientAuthMethods.ClientSecretBasic },
            ["revocation_endpoint_auth_methods_supported"] = new[] { ClientAuthMethods.ClientSecretPost, ClientAuthMethods.ClientSecretBasic },
            ["scopes_supported"] = new[] { IntegrationScopes.WishlistItems },
            ["authorization_response_iss_parameter_supported"] = true,
            ["openwish_api_base"] = new Uri(baseUri, "api/integrations/v1/").ToString()
        };

        if (clientStore.IsMcpEnabled)
        {
            string[] authMethods = [ClientAuthMethods.None, ClientAuthMethods.ClientSecretPost, ClientAuthMethods.ClientSecretBasic];
            metadata["registration_endpoint"] = new Uri(baseUri, "oauth/register").ToString();
            metadata["grant_types_supported"] = new[] { "authorization_code", "refresh_token" };
            metadata["token_endpoint_auth_methods_supported"] = authMethods;
            metadata["revocation_endpoint_auth_methods_supported"] = authMethods;
            metadata["client_id_metadata_document_supported"] = false;
        }

        return Ok(metadata);
    }

    /// <summary>Protected resource metadata for the MCP server (RFC 9728).</summary>
    [HttpGet(".well-known/oauth-protected-resource")]
    [HttpGet(".well-known/oauth-protected-resource/mcp")]
    public IActionResult GetProtectedResourceMetadata()
    {
        if (!clientStore.IsMcpEnabled)
        {
            return NotFound();
        }

        var baseUri = PublicBaseUri.Resolve(Request, settings.Value.BaseUri);
        return Ok(new Dictionary<string, object>
        {
            ["resource"] = PublicBaseUri.McpResource(baseUri).ToString(),
            ["authorization_servers"] = new[] { baseUri.ToString() },
            ["scopes_supported"] = new[] { IntegrationScopes.WishlistItems },
            ["bearer_methods_supported"] = new[] { "header" },
            ["resource_name"] = "OpenWish"
        });
    }

    /// <summary>Dynamic client registration for MCP clients (RFC 7591).</summary>
    [HttpPost("oauth/register")]
    [EnableRateLimiting(RegisterRateLimitPolicy)]
    public async Task<IActionResult> RegisterClient([FromBody] ClientRegistrationBody? body)
    {
        if (!clientStore.IsMcpEnabled)
        {
            return NotFound();
        }

        NoStore();
        if (body is null)
        {
            return OAuthError("invalid_client_metadata", "Send the client metadata as a JSON object.");
        }

        var result = await clientStore.RegisterAsync(new ClientRegistrationRequest(
            body.RedirectUris,
            body.ClientName,
            body.ClientUri,
            body.TokenEndpointAuthMethod,
            body.GrantTypes,
            body.ResponseTypes));
        if (result.ClientId is null)
        {
            return OAuthError(result.Error ?? "invalid_client_metadata", result.ErrorDescription);
        }

        return StatusCode(StatusCodes.Status201Created, new Dictionary<string, object?>
        {
            ["client_id"] = result.ClientId,
            ["client_secret"] = result.ClientSecret,
            ["client_id_issued_at"] = result.IssuedOn.ToUnixTimeSeconds(),
            ["client_secret_expires_at"] = result.ClientSecret is null ? null : 0,
            ["client_name"] = result.ClientName,
            ["redirect_uris"] = result.RedirectUris,
            ["grant_types"] = new[] { "authorization_code", "refresh_token" },
            ["response_types"] = new[] { "code" },
            ["token_endpoint_auth_method"] = result.TokenEndpointAuthMethod
        }.Where(pair => pair.Value is not null).ToDictionary());
    }

    [HttpPost("oauth/token")]
    [EnableRateLimiting(TokenRateLimitPolicy)]
    public async Task<IActionResult> ExchangeToken(
        [FromForm(Name = "grant_type")] string? grantType,
        [FromForm(Name = "code")] string? code,
        [FromForm(Name = "redirect_uri")] string? redirectUri,
        [FromForm(Name = "code_verifier")] string? codeVerifier,
        [FromForm(Name = "refresh_token")] string? refreshToken,
        [FromForm(Name = "resource")] string? resource,
        [FromForm(Name = "client_id")] string? clientId,
        [FromForm(Name = "client_secret")] string? clientSecret)
    {
        NoStore();
        var (app, usedBasic) = await AuthenticateClientAsync(clientId, clientSecret);
        if (app is null)
        {
            return InvalidClient(usedBasic);
        }

        var baseUri = PublicBaseUri.Resolve(Request, settings.Value.BaseUri);
        if (!PublicBaseUri.TryResolveAudience(resource, baseUri, clientStore.IsMcpEnabled, out var requestedAudience))
        {
            return OAuthError("invalid_target", "resource is not a resource this OpenWish serves.");
        }

        var result = grantType switch
        {
            "authorization_code" => await connectionService.ExchangeCodeAsync(app, code, redirectUri, codeVerifier, requestedAudience),
            "refresh_token" when clientStore.IsMcpEnabled => await connectionService.RefreshAsync(app, refreshToken, requestedAudience),
            _ => null
        };
        if (result is null)
        {
            return OAuthError(
                "unsupported_grant_type",
                clientStore.IsMcpEnabled
                    ? "Only the authorization_code and refresh_token grants are supported."
                    : "Only the authorization_code grant is supported.");
        }

        if (result.AccessToken is null || result.ConnectionPublicId is null)
        {
            return OAuthError(result.Error ?? "invalid_grant", result.ErrorDescription);
        }

        return Ok(new IntegrationTokenResponse
        {
            AccessToken = result.AccessToken,
            ConnectionId = result.ConnectionPublicId,
            ExpiresIn = result.ExpiresInSeconds,
            RefreshToken = result.RefreshToken
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
        var (app, usedBasic) = await AuthenticateClientAsync(clientId, clientSecret);
        if (app is null)
        {
            return InvalidClient(usedBasic);
        }

        await connectionService.RevokeTokenAsync(app, token);
        return Ok();
    }

    private async Task<(IntegrationApp? App, bool UsedBasic)> AuthenticateClientAsync(string? formClientId, string? formClientSecret)
    {
        if (AuthenticationHeaderValue.TryParse(Request.Headers.Authorization, out var header) &&
            string.Equals(header.Scheme, "Basic", StringComparison.OrdinalIgnoreCase))
        {
            if (!string.IsNullOrEmpty(formClientSecret))
            {
                // RFC 6749 section 2.3: clients must use only one authentication method per request.
                return (null, true);
            }

            string basicClientId;
            string basicSecret;
            try
            {
                var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(header.Parameter ?? string.Empty));
                var separator = decoded.IndexOf(':');
                if (separator <= 0)
                {
                    return (null, true);
                }

                basicClientId = Uri.UnescapeDataString(decoded[..separator].Replace('+', ' '));
                basicSecret = Uri.UnescapeDataString(decoded[(separator + 1)..].Replace('+', ' '));
            }
            catch (FormatException)
            {
                return (null, true);
            }

            if ((!string.IsNullOrEmpty(formClientId) && formClientId != basicClientId) || string.IsNullOrEmpty(basicSecret))
            {
                return (null, true);
            }

            return (await clientStore.AuthenticateAsync(basicClientId, basicSecret), true);
        }

        // Public MCP clients identify themselves with client_id alone and prove possession with PKCE.
        return (await clientStore.AuthenticateAsync(formClientId, formClientSecret), false);
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

    public sealed class ClientRegistrationBody
    {
        [JsonPropertyName("redirect_uris")]
        public List<string>? RedirectUris { get; set; }

        [JsonPropertyName("client_name")]
        public string? ClientName { get; set; }

        [JsonPropertyName("client_uri")]
        public string? ClientUri { get; set; }

        [JsonPropertyName("token_endpoint_auth_method")]
        public string? TokenEndpointAuthMethod { get; set; }

        [JsonPropertyName("grant_types")]
        public List<string>? GrantTypes { get; set; }

        [JsonPropertyName("response_types")]
        public List<string>? ResponseTypes { get; set; }
    }
}