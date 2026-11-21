using OpenWish.Shared.Models;

namespace OpenWish.Shared.Products;

/// <summary>
/// Small suggestions shown while someone types a new idea, so OpenWish can reuse what it already knows.
/// </summary>
public static class ItemCaptureHints
{
    /// <summary>The store to suggest for a typed product link, or null when the text is not a web link.</summary>
    public static string? StoreForLink(string? link)
    {
        if (!ProductLink.TryParse(link, out var parsed))
        {
            return null;
        }

        var store = ProductLink.GetStoreName(parsed.Url);
        return string.IsNullOrWhiteSpace(store) ? null : store;
    }

    /// <summary>Fills an empty "where to buy" from the product link. Returns true when it changed the idea.</summary>
    public static bool SuggestStore(WishlistItemModel item)
    {
        ArgumentNullException.ThrowIfNull(item);

        if (!string.IsNullOrWhiteSpace(item.WhereToBuy) || StoreForLink(item.Url) is not { } store)
        {
            return false;
        }

        item.WhereToBuy = store;
        return true;
    }

    /// <summary>
    /// Finds an existing idea with the same name or product link, ignoring case and tracking parameters.
    /// Returns the existing idea's name, or null when nothing matches.
    /// </summary>
    public static string? FindDuplicateName(IEnumerable<WishlistItemModel> existing, string? name, string? link, string? ignorePublicId = null)
    {
        ArgumentNullException.ThrowIfNull(existing);

        var trimmedName = name?.Trim();
        var cleanLink = CleanLink(link);
        if (string.IsNullOrEmpty(trimmedName) && cleanLink is null)
        {
            return null;
        }

        foreach (var item in existing)
        {
            if (item.Deleted || (ignorePublicId is not null && item.PublicId == ignorePublicId))
            {
                continue;
            }

            if (!string.IsNullOrEmpty(trimmedName) &&
                string.Equals(item.Name?.Trim(), trimmedName, StringComparison.OrdinalIgnoreCase))
            {
                return item.Name;
            }

            if (cleanLink is not null && string.Equals(CleanLink(item.Url), cleanLink, StringComparison.OrdinalIgnoreCase))
            {
                return item.Name;
            }
        }

        return null;
    }

    private static string? CleanLink(string? link) =>
        ProductLink.TryParse(link, out var parsed) ? ProductLink.Clean(parsed.Url).AbsoluteUri.TrimEnd('/') : null;
}