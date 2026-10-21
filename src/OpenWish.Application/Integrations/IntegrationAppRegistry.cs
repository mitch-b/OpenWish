using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenWish.Application.Models.Configuration;

namespace OpenWish.Application.Integrations;

/// <summary>An app the operator registered in configuration.</summary>
public sealed record IntegrationApp(
    string ClientId,
    string DisplayName,
    string? Description,
    string? HomepageUrl,
    IReadOnlyList<string> RedirectUris)
{
    internal byte[] SecretHash { get; init; } = [];
}

/// <summary>
/// Validates and serves the connected apps an operator registered under
/// <c>OpenWishSettings:Integrations:Apps</c>. Invalid entries are skipped with a warning so a
/// typo never takes the instance down.
/// </summary>
public sealed partial class IntegrationAppRegistry
{
    public const int MinimumSecretLength = 32;

    private readonly Dictionary<string, IntegrationApp> _apps = new(StringComparer.Ordinal);

    public IntegrationAppRegistry(IOptions<OpenWishSettings> settings, ILogger<IntegrationAppRegistry> logger)
    {
        foreach (var candidate in settings.Value.Integrations?.Apps ?? [])
        {
            var app = Validate(candidate, logger);
            if (app is null)
            {
                continue;
            }

            if (!_apps.TryAdd(app.ClientId, app))
            {
                logger.LogWarning("Connected app '{ClientId}' is registered more than once; only the first entry is used.", app.ClientId);
            }
        }

        Apps = [.. _apps.Values.OrderBy(app => app.DisplayName, StringComparer.CurrentCultureIgnoreCase)];
        RedirectOrigins = [.. Apps
            .SelectMany(app => app.RedirectUris)
            .Select(uri => new Uri(uri).GetLeftPart(UriPartial.Authority))
            .Distinct(StringComparer.OrdinalIgnoreCase)];

        if (Apps.Count > 0)
        {
            logger.LogInformation("Connected apps enabled for {Count} registered app(s).", Apps.Count);
        }
    }

    public IReadOnlyList<IntegrationApp> Apps { get; }

    public bool IsEnabled => Apps.Count > 0;

    /// <summary>Origins that consent redirects may target; the content security policy must allow them.</summary>
    public IReadOnlyList<string> RedirectOrigins { get; }

    public IntegrationApp? Find(string? clientId) =>
        !string.IsNullOrEmpty(clientId) && _apps.TryGetValue(clientId, out var app) ? app : null;

    public IntegrationApp? Authenticate(string? clientId, string? clientSecret)
    {
        var app = Find(clientId);
        if (app is null || string.IsNullOrEmpty(clientSecret))
        {
            return null;
        }

        var presented = SHA256.HashData(Encoding.UTF8.GetBytes(clientSecret));
        return CryptographicOperations.FixedTimeEquals(presented, app.SecretHash) ? app : null;
    }

    public static bool IsRegisteredRedirect(IntegrationApp app, string? redirectUri) =>
        !string.IsNullOrEmpty(redirectUri) &&
        app.RedirectUris.Contains(redirectUri, StringComparer.Ordinal);

    private static IntegrationApp? Validate(IntegrationAppSettings candidate, ILogger logger)
    {
        var clientId = candidate.ClientId?.Trim();
        if (string.IsNullOrEmpty(clientId) || !ClientIdPattern().IsMatch(clientId))
        {
            logger.LogWarning(
                "Skipping a connected app: ClientId must be 1-100 letters, digits, '.', '_' or '-'.");
            return null;
        }

        if (string.IsNullOrEmpty(candidate.ClientSecret) || candidate.ClientSecret.Length < MinimumSecretLength)
        {
            logger.LogWarning(
                "Skipping connected app '{ClientId}': ClientSecret must be at least {Length} characters.",
                clientId,
                MinimumSecretLength);
            return null;
        }

        var redirectUris = new List<string>();
        foreach (var redirectUri in candidate.RedirectUris)
        {
            var trimmed = redirectUri?.Trim();
            if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var parsed) ||
                (parsed.Scheme != Uri.UriSchemeHttps && parsed.Scheme != Uri.UriSchemeHttp) ||
                !string.IsNullOrEmpty(parsed.Fragment) ||
                !string.IsNullOrEmpty(parsed.UserInfo))
            {
                logger.LogWarning(
                    "Ignoring redirect URI for connected app '{ClientId}': use an absolute http or https URL without a fragment.",
                    clientId);
                continue;
            }

            if (parsed.Scheme == Uri.UriSchemeHttp && !parsed.IsLoopback)
            {
                logger.LogWarning(
                    "Connected app '{ClientId}' redirects over plain HTTP to {Host}. Use HTTPS outside a trusted network.",
                    clientId,
                    parsed.Host);
            }

            redirectUris.Add(trimmed!);
        }

        if (redirectUris.Count == 0)
        {
            logger.LogWarning("Skipping connected app '{ClientId}': no valid RedirectUris are configured.", clientId);
            return null;
        }

        string? homepageUrl = null;
        if (!string.IsNullOrWhiteSpace(candidate.HomepageUrl))
        {
            if (Uri.TryCreate(candidate.HomepageUrl.Trim(), UriKind.Absolute, out var homepage) &&
                (homepage.Scheme == Uri.UriSchemeHttps || homepage.Scheme == Uri.UriSchemeHttp))
            {
                homepageUrl = homepage.ToString();
            }
            else
            {
                logger.LogWarning("Ignoring HomepageUrl for connected app '{ClientId}': use an absolute http or https URL.", clientId);
            }
        }

        var displayName = string.IsNullOrWhiteSpace(candidate.DisplayName) ? clientId : candidate.DisplayName.Trim();
        var description = string.IsNullOrWhiteSpace(candidate.Description) ? null : candidate.Description.Trim();

        return new IntegrationApp(
            clientId,
            Truncate(displayName, 80),
            description is null ? null : Truncate(description, 280),
            homepageUrl,
            redirectUris.Distinct(StringComparer.Ordinal).ToList())
        {
            SecretHash = SHA256.HashData(Encoding.UTF8.GetBytes(candidate.ClientSecret))
        };
    }

    private static string Truncate(string value, int length) =>
        value.Length <= length ? value : value[..length];

    [GeneratedRegex("^[A-Za-z0-9._-]{1,100}$")]
    private static partial Regex ClientIdPattern();
}