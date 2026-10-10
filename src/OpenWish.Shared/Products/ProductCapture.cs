using System.Globalization;
using OpenWish.Shared.Models;

namespace OpenWish.Shared.Products;

/// <summary>
/// A product someone sent to OpenWish from elsewhere: a phone's share sheet, the "Add to OpenWish"
/// bookmarklet, or a pasted link. Values arrive as untrusted query-string text and are cleaned here.
/// </summary>
public sealed record ProductCapture
{
    private const int MaxImageUrlLength = 2048;

    public Uri? Link { get; init; }
    public string? StoreName { get; init; }
    public string? Name { get; init; }
    public string? Description { get; init; }
    public decimal? Price { get; init; }
    public string? Image { get; init; }

    public bool HasLink => Link is not null;
    public bool IsEmpty => Link is null && Name is null;

    /// <summary>
    /// Reads share target or bookmarklet values. Shopping apps put links in any of url, text, or title,
    /// often with the product name around them, so each is checked.
    /// </summary>
    public static ProductCapture FromQuery(
        string? url,
        string? text = null,
        string? title = null,
        string? description = null,
        string? price = null,
        string? image = null)
    {
        SharedProductLink? sharedLink = null;
        foreach (var candidate in new[] { url, text, title })
        {
            if (ProductLink.TryParse(candidate, out var parsed))
            {
                sharedLink = parsed;
                break;
            }
        }

        var link = sharedLink is null ? null : ProductLink.Clean(sharedLink.Url);
        var storeName = link is null ? null : ProductLink.GetStoreName(link);
        var name = CleanName(title, storeName) ?? CleanName(text, storeName) ?? sharedLink?.TitleHint;
        var cleanDescription = ProductLink.CleanDescription(description, name);
        decimal? cleanPrice = ProductLink.TryParsePrice(price, out var parsedPrice) ? parsedPrice : null;

        return new ProductCapture
        {
            Link = link,
            StoreName = storeName,
            Name = name,
            Description = cleanDescription,
            Price = cleanPrice,
            Image = CleanImage(image)
        };
    }

    /// <summary>Fills empty fields on a new item with the captured details.</summary>
    public void ApplyTo(WishlistItemModel item)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (string.IsNullOrWhiteSpace(item.Name) && Name is not null)
        {
            item.Name = Name;
        }

        if (string.IsNullOrWhiteSpace(item.Description) && Description is not null)
        {
            item.Description = Description;
        }

        if (!item.Price.HasValue && Price.HasValue)
        {
            item.Price = Price;
        }

        if (string.IsNullOrWhiteSpace(item.Image) && Image is not null)
        {
            item.Image = Image;
        }

        if (string.IsNullOrWhiteSpace(item.Url) && Link is not null)
        {
            item.Url = Link.AbsoluteUri;
        }

        if (string.IsNullOrWhiteSpace(item.WhereToBuy) && StoreName is not null)
        {
            item.WhereToBuy = StoreName;
        }
    }

    /// <summary>Builds the query string that carries this capture to the add-item page.</summary>
    public string ToQueryString()
    {
        var parts = new List<string>();
        addPart("url", Link?.AbsoluteUri);
        addPart("title", Name);
        addPart("description", Description);
        addPart("price", Price?.ToString("0.##", CultureInfo.InvariantCulture));
        addPart("image", Image);
        return parts.Count == 0 ? string.Empty : "?" + string.Join('&', parts);

        void addPart(string key, string? value)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                parts.Add($"{key}={Uri.EscapeDataString(value)}");
            }
        }
    }

    private static string? CleanName(string? value, string? storeName)
    {
        if (ProductLink.ContainsWebAddress(value))
        {
            return null;
        }

        var name = ProductLink.CleanProductName(value, storeName);
        return name is null || ProductLink.IsStoreName(name, storeName) ? null : name;
    }

    private static string? CleanImage(string? image)
    {
        if (string.IsNullOrWhiteSpace(image) || image.Length > MaxImageUrlLength)
        {
            return null;
        }

        return Uri.TryCreate(image.Trim(), UriKind.Absolute, out var uri) &&
            (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps) &&
            !string.IsNullOrWhiteSpace(uri.Host)
                ? uri.AbsoluteUri
                : null;
    }
}