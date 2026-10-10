using OpenWish.Shared.Models;

namespace OpenWish.Shared.Products;

public enum ProductImportKind
{
    /// <summary>At least one detail was filled in from the store's page.</summary>
    Imported,

    /// <summary>The store's page had details, but every matching field was already filled in.</summary>
    KeptExisting,

    /// <summary>The store's page couldn't be read, so a name was filled in from the link or shared text.</summary>
    FromLink,

    /// <summary>Nothing beyond the link itself was found.</summary>
    LinkOnly,

    /// <summary>OpenWish can't look up this link, such as a link to a private network address.</summary>
    Unavailable
}

public sealed record ProductImportResult(ProductImportKind Kind, string StoreName, bool ItemHasName = false)
{
    public bool FoundDetails => Kind is ProductImportKind.Imported or ProductImportKind.KeptExisting;

    public string Message => Kind switch
    {
        ProductImportKind.Imported => $"Imported details from {StoreName}. Review them before saving.",
        ProductImportKind.KeptExisting => "Product details checked. Your existing details were kept.",
        ProductImportKind.FromLink =>
            $"{StoreName} didn't share product details, so we filled in what we could from the link. Check the name before saving.",
        ProductImportKind.LinkOnly when ItemHasName => "The product link is ready. Review the details before saving.",
        ProductImportKind.LinkOnly => "No product details were found. The product link is ready; add any details you want.",
        _ => "We couldn't look up details for this link. The link is ready; add the details you know."
    };
}

/// <summary>The link and store filled in before a lookup, so the looked-up page can refine them afterward.</summary>
public sealed record ProductLinkSeed(string? Url, string? WhereToBuy);

/// <summary>
/// Applies looked-up product details to a wishlist item without replacing anything the person already entered.
/// </summary>
public static class ProductImport
{
    /// <summary>
    /// Fills an empty link and store right away, so they are kept even when the lookup fails or times out.
    /// Pass the returned seed to <see cref="Apply"/> so the store page's own link can replace the placeholder.
    /// </summary>
    public static ProductLinkSeed SeedLink(WishlistItemModel item, Uri link)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(link);

        string? seededUrl = null;
        string? seededStore = null;
        if (string.IsNullOrWhiteSpace(item.Url))
        {
            item.Url = seededUrl = ProductLink.Clean(link).AbsoluteUri;
        }

        if (string.IsNullOrWhiteSpace(item.WhereToBuy))
        {
            item.WhereToBuy = seededStore = ProductLink.GetStoreName(link);
        }

        return new ProductLinkSeed(seededUrl, seededStore);
    }

    public static ProductImportResult Apply(WishlistItemModel item, Uri link, ProductModel? product, ProductLinkSeed? seed = null)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(link);

        // Placeholders from SeedLink give way to the page's details unless the person changed them during the lookup.
        if (seed?.Url is not null && string.Equals(item.Url, seed.Url, StringComparison.Ordinal))
        {
            item.Url = null;
        }

        if (seed?.WhereToBuy is not null && string.Equals(item.WhereToBuy, seed.WhereToBuy, StringComparison.Ordinal))
        {
            item.WhereToBuy = null;
        }

        var storeName = !string.IsNullOrWhiteSpace(product?.StoreName)
            ? product.StoreName.Trim()
            : ProductLink.GetStoreName(link);

        if (string.IsNullOrWhiteSpace(item.Url))
        {
            item.Url = TryGetWebAddress(product?.Url) ?? ProductLink.Clean(link).AbsoluteUri;
        }

        if (string.IsNullOrWhiteSpace(item.WhereToBuy))
        {
            item.WhereToBuy = storeName;
        }

        if (product is null)
        {
            return new ProductImportResult(ProductImportKind.Unavailable, storeName, !string.IsNullOrWhiteSpace(item.Name));
        }

        var name = product.Name?.Trim();
        var description = product.Description?.Trim();
        var price = product.Price is >= 0 ? decimal.Round(product.Price.Value, 2, MidpointRounding.AwayFromZero) : (decimal?)null;
        var image = TryGetWebAddress(product.ImageUrl);

        var filledName = false;
        var filledDetail = false;
        if (string.IsNullOrWhiteSpace(item.Name) && !string.IsNullOrWhiteSpace(name))
        {
            item.Name = name;
            filledName = true;
        }

        if (string.IsNullOrWhiteSpace(item.Description) && !string.IsNullOrWhiteSpace(description))
        {
            item.Description = description;
            filledDetail = true;
        }

        if (!item.Price.HasValue && price.HasValue)
        {
            item.Price = price;
            filledDetail = true;
        }

        if (string.IsNullOrWhiteSpace(item.Image) && image is not null)
        {
            item.Image = image;
            filledDetail = true;
        }

        var itemHasName = !string.IsNullOrWhiteSpace(item.Name);
        if (product.FromLinkOnly)
        {
            return new ProductImportResult(filledName ? ProductImportKind.FromLink : ProductImportKind.LinkOnly, storeName, itemHasName);
        }

        var hadDetails = !string.IsNullOrWhiteSpace(name) ||
            !string.IsNullOrWhiteSpace(description) ||
            price.HasValue ||
            image is not null;
        var kind = filledName || filledDetail
            ? ProductImportKind.Imported
            : hadDetails ? ProductImportKind.KeptExisting : ProductImportKind.LinkOnly;
        return new ProductImportResult(kind, storeName, itemHasName);
    }

    private static string? TryGetWebAddress(string? value) =>
        Uri.TryCreate(value?.Trim(), UriKind.Absolute, out var uri) &&
        (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps) &&
        !string.IsNullOrWhiteSpace(uri.Host)
            ? uri.AbsoluteUri
            : null;
}