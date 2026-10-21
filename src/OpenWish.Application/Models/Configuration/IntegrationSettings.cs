namespace OpenWish.Application.Models.Configuration;

/// <summary>Apps the operator trusts to add and manage ideas for people who connect them.</summary>
public record IntegrationSettings
{
    public List<IntegrationAppSettings> Apps { get; set; } = [];
}

public record IntegrationAppSettings
{
    public string? ClientId { get; set; }
    public string? ClientSecret { get; set; }
    public string? DisplayName { get; set; }
    public string? Description { get; set; }
    public string? HomepageUrl { get; set; }
    public List<string> RedirectUris { get; set; } = [];
}