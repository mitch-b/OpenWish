namespace OpenWish.Data.Entities;

/// <summary>Which OpenWish surface a connected-app token may call.</summary>
public enum IntegrationAudience
{
    /// <summary>The REST API at <c>/api/integrations/v1</c>.</summary>
    Api = 0,

    /// <summary>The Model Context Protocol server at <c>/mcp</c>.</summary>
    Mcp = 1
}