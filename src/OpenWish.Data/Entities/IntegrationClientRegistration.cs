using System.ComponentModel.DataAnnotations;

namespace OpenWish.Data.Entities;

/// <summary>
/// An MCP client that registered itself through OAuth dynamic client registration (RFC 7591).
/// Its name is self-asserted, so the consent screen labels it as unverified.
/// </summary>
public class IntegrationClientRegistration
{
    [Key]
    public int Id { get; set; }
    public required string ClientId { get; set; }
    public required string ClientName { get; set; }
    public string? ClientUri { get; set; }

    /// <summary>Registered redirect URIs, separated by newlines.</summary>
    public required string RedirectUris { get; set; }

    /// <summary>SHA-256 hash of the client secret, or null for a public client.</summary>
    public string? SecretHash { get; set; }
    public DateTimeOffset CreatedOn { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? LastUsedOn { get; set; }
}