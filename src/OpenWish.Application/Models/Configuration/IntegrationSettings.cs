namespace OpenWish.Application.Models.Configuration;

/// <summary>Apps the operator trusts to add and manage ideas for people who connect them.</summary>
public record IntegrationSettings
{
    public List<IntegrationAppSettings> Apps { get; set; } = [];

    /// <summary>Lets AI assistants connect to the Model Context Protocol server at <c>/mcp</c>.</summary>
    public IntegrationMcpSettings Mcp { get; set; } = new();
}

public record IntegrationMcpSettings
{
    /// <summary>Turns on <c>/mcp</c> and lets MCP clients register themselves. Off by default.</summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Remote hosts that self-registered clients may redirect to, such as <c>claude.ai</c>. Loopback
    /// redirects for desktop assistants are always allowed. Empty allows any HTTPS host.
    /// </summary>
    public List<string> AllowedRedirectHosts { get; set; } = [];
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