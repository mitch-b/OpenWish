namespace OpenWish.Shared.Models;

public record ProductModel
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public decimal? Price { get; set; }
    public string? ImageUrl { get; set; }
    public string? Url { get; set; }

    /// <summary>
    /// A friendly name for the shop, such as "Amazon" or the page's site name.
    /// </summary>
    public string? StoreName { get; set; }

    /// <summary>
    /// True when the store's page could not be read, so details came only from the link itself.
    /// </summary>
    public bool FromLinkOnly { get; set; }
}