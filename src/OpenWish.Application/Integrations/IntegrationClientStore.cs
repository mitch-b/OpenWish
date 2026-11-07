using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenWish.Application.Models.Configuration;
using OpenWish.Data;
using OpenWish.Data.Entities;

namespace OpenWish.Application.Integrations;

public sealed record ClientRegistrationRequest(
    IReadOnlyList<string>? RedirectUris,
    string? ClientName,
    string? ClientUri,
    string? TokenEndpointAuthMethod,
    IReadOnlyList<string>? GrantTypes,
    IReadOnlyList<string>? ResponseTypes);

public sealed record ClientRegistrationResult(
    string? ClientId,
    string? ClientSecret,
    string? ClientName,
    IReadOnlyList<string> RedirectUris,
    string TokenEndpointAuthMethod,
    DateTimeOffset IssuedOn,
    string? Error,
    string? ErrorDescription)
{
    public static ClientRegistrationResult Failed(string error, string description) =>
        new(null, null, null, [], ClientAuthMethods.None, default, error, description);
}

public static class ClientAuthMethods
{
    public const string None = "none";
    public const string ClientSecretPost = "client_secret_post";
    public const string ClientSecretBasic = "client_secret_basic";
}

/// <summary>Finds and authenticates connected apps from configuration and MCP self-registration.</summary>
public interface IIntegrationClientStore
{
    /// <summary>True when <c>/mcp</c> and dynamic client registration are turned on.</summary>
    bool IsMcpEnabled { get; }

    Task<IntegrationApp?> FindAsync(string? clientId);

    /// <summary>
    /// Authenticates a client at the token endpoint. Public clients send no secret; confidential
    /// clients must send the right one.
    /// </summary>
    Task<IntegrationApp?> AuthenticateAsync(string? clientId, string? clientSecret);

    /// <summary>Registers an MCP client (RFC 7591).</summary>
    Task<ClientRegistrationResult> RegisterAsync(ClientRegistrationRequest request);
}

public sealed class IntegrationClientStore(
    IDbContextFactory<ApplicationDbContext> contextFactory,
    IntegrationAppRegistry registry,
    IOptions<OpenWishSettings> settings,
    ILogger<IntegrationClientStore> logger) : IIntegrationClientStore
{
    public const int MaxRedirectUris = 10;
    public const int MaxRedirectUriLength = 2048;
    public const string DefaultClientName = "An AI assistant";
    private static readonly TimeSpan _unusedRegistrationLifetime = TimeSpan.FromDays(1);

    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory = contextFactory;
    private readonly IntegrationAppRegistry _registry = registry;
    private readonly IntegrationMcpSettings _mcp = settings.Value.Integrations?.Mcp ?? new();
    private readonly ILogger<IntegrationClientStore> _logger = logger;

    public bool IsMcpEnabled => _mcp.Enabled;

    public async Task<IntegrationApp?> FindAsync(string? clientId)
    {
        var app = _registry.Find(clientId);
        if (app is not null || !IsSelfRegisteredId(clientId))
        {
            return app;
        }

        await using var context = await _contextFactory.CreateDbContextAsync();
        var registration = await context.IntegrationClientRegistrations
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.ClientId == clientId);

        return registration is null ? null : ToApp(registration);
    }

    public async Task<IntegrationApp?> AuthenticateAsync(string? clientId, string? clientSecret)
    {
        if (_registry.Find(clientId) is not null)
        {
            return _registry.Authenticate(clientId, clientSecret);
        }

        var app = await FindAsync(clientId);
        if (app is null)
        {
            return null;
        }

        if (app.IsPublicClient)
        {
            // A public client cannot keep a secret, so one it sends proves nothing.
            return string.IsNullOrEmpty(clientSecret) ? app : null;
        }

        if (string.IsNullOrEmpty(clientSecret))
        {
            return null;
        }

        var presented = SHA256.HashData(Encoding.UTF8.GetBytes(clientSecret));
        return CryptographicOperations.FixedTimeEquals(presented, app.SecretHash) ? app : null;
    }

    public async Task<ClientRegistrationResult> RegisterAsync(ClientRegistrationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var redirectUris = new List<string>();
        foreach (var candidate in request.RedirectUris ?? [])
        {
            var error = ValidateRedirectUri(candidate);
            if (error is not null)
            {
                return ClientRegistrationResult.Failed("invalid_redirect_uri", error);
            }

            redirectUris.Add(candidate);
        }

        redirectUris = redirectUris.Distinct(StringComparer.Ordinal).ToList();
        if (redirectUris.Count == 0)
        {
            return ClientRegistrationResult.Failed("invalid_redirect_uri", "Register at least one redirect_uri.");
        }

        if (redirectUris.Count > MaxRedirectUris)
        {
            return ClientRegistrationResult.Failed("invalid_redirect_uri", $"Register at most {MaxRedirectUris} redirect URIs.");
        }

        var authMethod = string.IsNullOrEmpty(request.TokenEndpointAuthMethod)
            ? ClientAuthMethods.None
            : request.TokenEndpointAuthMethod;
        if (authMethod is not (ClientAuthMethods.None or ClientAuthMethods.ClientSecretPost or ClientAuthMethods.ClientSecretBasic))
        {
            return ClientRegistrationResult.Failed(
                "invalid_client_metadata",
                "token_endpoint_auth_method must be none, client_secret_post, or client_secret_basic.");
        }

        if (request.GrantTypes?.Any(grant => grant is not ("authorization_code" or "refresh_token")) == true)
        {
            return ClientRegistrationResult.Failed(
                "invalid_client_metadata",
                "Only the authorization_code and refresh_token grant types are supported.");
        }

        if (request.ResponseTypes?.Any(type => type != "code") == true)
        {
            return ClientRegistrationResult.Failed("invalid_client_metadata", "Only the code response type is supported.");
        }

        string? clientUri = null;
        if (!string.IsNullOrWhiteSpace(request.ClientUri))
        {
            if (!Uri.TryCreate(request.ClientUri.Trim(), UriKind.Absolute, out var parsed) ||
                (parsed.Scheme != Uri.UriSchemeHttps && parsed.Scheme != Uri.UriSchemeHttp) ||
                parsed.OriginalString.Length > MaxRedirectUriLength)
            {
                return ClientRegistrationResult.Failed("invalid_client_metadata", "client_uri must be an http or https URL.");
            }

            clientUri = parsed.ToString();
        }

        var clientName = CleanName(request.ClientName);
        var clientId = IntegrationTokens.NewClientId();
        var clientSecret = authMethod == ClientAuthMethods.None ? null : IntegrationTokens.NewClientSecret();
        var now = DateTimeOffset.UtcNow;

        await using var context = await _contextFactory.CreateDbContextAsync();
        context.IntegrationClientRegistrations.Add(new IntegrationClientRegistration
        {
            ClientId = clientId,
            ClientName = clientName,
            ClientUri = clientUri,
            RedirectUris = string.Join('\n', redirectUris),
            SecretHash = clientSecret is null ? null : IntegrationTokens.Hash(clientSecret),
            CreatedOn = now
        });
        await context.SaveChangesAsync();

        if (context.Database.IsRelational())
        {
            // Assistants register again for each new connection, so drop registrations nobody finished connecting.
            var staleBefore = now - _unusedRegistrationLifetime;
            await context.IntegrationClientRegistrations
                .Where(r => r.LastUsedOn == null &&
                    r.CreatedOn < staleBefore &&
                    !context.IntegrationConnections.Any(c => c.ClientId == r.ClientId))
                .ExecuteDeleteAsync();
        }

        _logger.LogInformation(
            "MCP client '{ClientName}' registered as {ClientId} redirecting to {Hosts}.",
            clientName,
            clientId,
            string.Join(", ", redirectUris.Select(uri => new Uri(uri).Authority).Distinct(StringComparer.OrdinalIgnoreCase)));

        return new ClientRegistrationResult(clientId, clientSecret, clientName, redirectUris, authMethod, now, null, null);
    }

    internal static bool IsSelfRegisteredId(string? clientId) =>
        !string.IsNullOrEmpty(clientId) &&
        clientId.StartsWith(IntegrationTokens.ClientIdPrefix, StringComparison.Ordinal) &&
        clientId.Length <= 100;

    private IntegrationApp? ToApp(IntegrationClientRegistration registration) =>
        !IsMcpEnabled
            ? null
            : new IntegrationApp(
                registration.ClientId,
                registration.ClientName,
                null,
                null,
                registration.RedirectUris.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                IsSelfRegistered = true,
                SecretHash = registration.SecretHash is null ? [] : Convert.FromHexString(registration.SecretHash)
            };

    private string? ValidateRedirectUri(string? candidate)
    {
        if (string.IsNullOrEmpty(candidate) ||
            candidate.Length > MaxRedirectUriLength ||
            !Uri.TryCreate(candidate, UriKind.Absolute, out var uri) ||
            !string.IsNullOrEmpty(uri.Fragment) ||
            !string.IsNullOrEmpty(uri.UserInfo))
        {
            return "Each redirect_uri must be an absolute URL without a fragment or credentials.";
        }

        if (uri.Scheme != Uri.UriSchemeHttps && !(uri.Scheme == Uri.UriSchemeHttp && uri.IsLoopback))
        {
            return "Each redirect_uri must use https, or http on a loopback address such as 127.0.0.1.";
        }

        // Loopback redirects only reach the person's own computer, so the allowlist limits remote hosts.
        if (_mcp.AllowedRedirectHosts.Count > 0 && !uri.IsLoopback &&
            !_mcp.AllowedRedirectHosts.Any(host => string.Equals(host?.Trim(), uri.IdnHost, StringComparison.OrdinalIgnoreCase)))
        {
            return $"This OpenWish doesn't allow assistants to redirect to {uri.Host}.";
        }

        return null;
    }

    private static string CleanName(string? name)
    {
        var cleaned = new string((name ?? string.Empty).Where(c => !char.IsControl(c)).ToArray()).Trim();
        if (cleaned.Length == 0)
        {
            return DefaultClientName;
        }

        return cleaned.Length <= 80 ? cleaned : cleaned[..80];
    }
}