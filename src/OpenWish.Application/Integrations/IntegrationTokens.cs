using System.Security.Cryptography;
using System.Text;

namespace OpenWish.Application.Integrations;

internal static class IntegrationTokens
{
    public const string AccessTokenPrefix = "owi_";
    public const string AuthorizationCodePrefix = "owc_";
    public const string RefreshTokenPrefix = "owr_";
    public const string ClientIdPrefix = "owm_";
    public const string ClientSecretPrefix = "owms_";

    public static string NewAccessToken() => AccessTokenPrefix + RandomSegment();

    public static string NewAuthorizationCode() => AuthorizationCodePrefix + RandomSegment();

    public static string NewRefreshToken() => RefreshTokenPrefix + RandomSegment();

    public static string NewClientId() => ClientIdPrefix + Base64UrlEncode(RandomNumberGenerator.GetBytes(16));

    public static string NewClientSecret() => ClientSecretPrefix + RandomSegment();

    public static string Hash(string value) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    /// <summary>Checks an RFC 7636 S256 code verifier against the stored challenge.</summary>
    public static bool VerifyPkce(string? codeVerifier, string codeChallenge)
    {
        if (string.IsNullOrEmpty(codeVerifier) || codeVerifier.Length is < 43 or > 128)
        {
            return false;
        }

        var computed = Base64UrlEncode(SHA256.HashData(Encoding.ASCII.GetBytes(codeVerifier)));
        return CryptographicOperations.FixedTimeEquals(
            Encoding.ASCII.GetBytes(computed),
            Encoding.ASCII.GetBytes(codeChallenge));
    }

    public static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static string RandomSegment() => Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
}