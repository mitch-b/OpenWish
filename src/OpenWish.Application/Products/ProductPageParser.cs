using System.Text.Json;
using HtmlAgilityPack;
using OpenWish.Shared.Products;

namespace OpenWish.Application.Products;

/// <summary>
/// Product details read from a store page before they are cleaned for a wishlist.
/// </summary>
internal sealed record ProductPageDetails(
    string? Name,
    string? Description,
    decimal? Price,
    IReadOnlyList<string> ImageCandidates,
    string? SiteName,
    bool IsBlocked)
{
    public static readonly ProductPageDetails Blocked = new(null, null, null, [], null, true);
}

/// <summary>
/// Reads product details from HTML using, in order: schema.org JSON-LD, Open Graph and other
/// meta tags, store-specific markup, microdata, and common product page markup.
/// </summary>
internal static class ProductPageParser
{
    private const int MaxJsonLdLength = 512 * 1024;
    private const int MaxJsonLdDepth = 24;
    private const int SmallPageLength = 150 * 1024;
    private const int MaxHeadingLength = 200;

    private static readonly JsonDocumentOptions _jsonLdOptions = new()
    {
        AllowTrailingCommas = true,
        CommentHandling = JsonCommentHandling.Skip,
        MaxDepth = 64
    };

    private static readonly string[] _productTypes =
        ["Product", "ProductGroup", "IndividualProduct", "ProductModel", "SomeProducts", "Book", "Car", "Vehicle"];

    private static readonly string[] _blockedTitleFragments =
    [
        "robot or human", "access denied", "just a moment", "attention required", "are you a robot",
        "pardon our interruption", "security check", "verify you are a human", "verify you are human",
        "please verify", "403 forbidden", "access to this page has been denied", "checking your browser",
        "you have been blocked", "request blocked", "bot check", "captcha", "human verification",
        "sorry! something went wrong", "service unavailable", "too many requests", "unusual traffic", "hang tight",
        "routing to checkout", "one more step", "please wait while", "you are in line", "you're in line"
    ];

    private static readonly string[] _blockedMarkupFragments =
    [
        "/errors/validateCaptcha", "captcha-delivery.com", "px-captcha", "_Incapsula_Resource",
        "window._cf_chl_opt", "cf-chl-widget", "Type the characters you see", "queue-it.net"
    ];

    private static readonly string[] _nameSelectors =
    [
        "//h1[@class='product-name']",
        "//h1[contains(@class, 'product-name')]",
        "//h1[contains(@class, 'product-title')]",
        "//h1[contains(@class, 'product_title')]",
        "//h1[contains(@class, 'pdp-title')]",
        "//*[@data-testid='product-title']",
        "//*[@data-test='product-title']"
    ];

    private static readonly string[] _descriptionSelectors =
    [
        "//div[@class='product-description']",
        "//div[contains(@class, 'product-description')]",
        "//div[contains(@class, 'pdp-description')]",
        "//div[contains(@class, 'product__description')]"
    ];

    private static readonly string[] _priceSelectors =
    [
        "//span[@class='price-value']",
        "//*[@data-testid='product-price']",
        "//*[@data-test='product-price']",
        "//div[contains(@class, 'product-price')]//span[contains(@class, 'price')]",
        "//span[contains(@class, 'price-item--sale')]",
        "//span[contains(@class, 'price-item--regular')]",
        "//p[contains(@class, 'price')]//span[contains(@class, 'amount')]"
    ];

    private static readonly string[] _imageSelectors =
    [
        "//img[@id='main-image']",
        "//img[contains(@class, 'product-image')]",
        "//img[contains(@class, 'product__image')]",
        "//link[@rel='image_src']"
    ];

    private static readonly string[] _amazonPriceSelectors =
    [
        "//span[contains(@class, 'priceToPay')]//span[contains(@class, 'a-offscreen')]",
        "//span[@id='apex-pricetopay-accessibility-label']",
        "//div[@id='corePriceDisplay_desktop_feature_div']//span[contains(@class, 'a-offscreen')]",
        "//div[@id='corePrice_feature_div']//span[contains(@class, 'a-offscreen')]",
        "//div[@id='corePrice_desktop']//span[contains(@class, 'a-offscreen')]",
        "//div[@id='apex_desktop']//span[contains(@class, 'a-offscreen')]",
        "//span[@id='priceblock_ourprice']",
        "//span[@id='priceblock_dealprice']",
        "//span[@id='kindle-price']",
        "//span[@id='price']",
        "//input[@id='attach-base-product-price']"
    ];

    public static ProductPageDetails Parse(string html, Uri pageUri)
    {
        var document = new HtmlDocument();
        document.LoadHtml(html);
        var root = document.DocumentNode;

        var title = ReadText(root.SelectSingleNode("//title"));
        if (IsBlockedPage(title, html))
        {
            return ProductPageDetails.Blocked;
        }

        var meta = ReadMeta(root);
        var jsonLd = ReadJsonLd(root);
        var amazon = IsAmazon(pageUri, root) ? ReadAmazon(root) : null;
        var microdata = new Lazy<PageFields>(() => ReadMicrodata(root));

        var name = FirstText(
            () => jsonLd?.Name,
            () => meta.GetValueOrDefault("og:title"),
            () => meta.GetValueOrDefault("twitter:title"),
            () => amazon?.Name,
            () => microdata.Value.Name,
            () => SelectText(root, _nameSelectors),
            () => ReadText(root.SelectSingleNode("//h1"), MaxHeadingLength),
            () => title);

        var description = FirstText(
            () => jsonLd?.Description,
            () => amazon?.Description,
            () => meta.GetValueOrDefault("og:description"),
            () => meta.GetValueOrDefault("twitter:description"),
            () => microdata.Value.Description,
            () => SelectText(root, _descriptionSelectors),
            () => amazon is null ? meta.GetValueOrDefault("description") : null);

        var price = FirstPrice(
            () => jsonLd?.Price,
            () => meta.GetValueOrDefault("product:price:amount"),
            () => meta.GetValueOrDefault("og:price:amount"),
            () => meta.GetValueOrDefault("product:sale_price:amount"),
            () => amazon?.Price,
            () => microdata.Value.Price,
            () => ReadTwitterPrice(meta),
            () => SelectText(root, _priceSelectors));

        var images = new List<string>();
        AddImage(images, jsonLd?.Image);
        AddImage(images, meta.GetValueOrDefault("og:image:secure_url"));
        AddImage(images, meta.GetValueOrDefault("og:image"));
        AddImage(images, meta.GetValueOrDefault("og:image:url"));
        AddImage(images, meta.GetValueOrDefault("twitter:image"));
        AddImage(images, meta.GetValueOrDefault("twitter:image:src"));
        AddImage(images, amazon?.Image);
        if (images.Count == 0)
        {
            AddImage(images, microdata.Value.Image);
            foreach (var selector in _imageSelectors)
            {
                AddImage(images, ReadImageSource(root.SelectSingleNode(selector)));
            }
        }

        var siteName = FirstText(() => meta.GetValueOrDefault("og:site_name"), () => meta.GetValueOrDefault("application-name"));
        return new ProductPageDetails(name, description, price, images, siteName, false);
    }

    internal static bool IsBlockedPage(string? title, string html)
    {
        if (title is not null)
        {
            var lowerTitle = title.ToLowerInvariant();
            if (_blockedTitleFragments.Any(lowerTitle.Contains))
            {
                return true;
            }
        }

        return html.Length < SmallPageLength &&
               _blockedMarkupFragments.Any(fragment => html.Contains(fragment, StringComparison.OrdinalIgnoreCase));
    }

    private static Dictionary<string, string> ReadMeta(HtmlNode root)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var nodes = root.SelectNodes("//meta");
        if (nodes is null)
        {
            return values;
        }

        foreach (var node in nodes)
        {
            var key = node.GetAttributeValue("property", null) ??
                      node.GetAttributeValue("name", null) ??
                      node.GetAttributeValue("itemprop", null);
            var value = Attribute(node, "content") ?? Attribute(node, "value");
            if (!string.IsNullOrWhiteSpace(key) && value is not null)
            {
                values.TryAdd(key.Trim(), value);
            }
        }

        return values;
    }

    private static string? ReadTwitterPrice(Dictionary<string, string> meta)
    {
        for (var index = 1; index <= 4; index++)
        {
            if (meta.TryGetValue($"twitter:label{index}", out var label) &&
                label.Contains("price", StringComparison.OrdinalIgnoreCase) &&
                meta.TryGetValue($"twitter:data{index}", out var data))
            {
                return data;
            }
        }

        return null;
    }

    private static bool IsAmazon(Uri pageUri, HtmlNode root) =>
        pageUri.Host.Contains("amazon.", StringComparison.OrdinalIgnoreCase) ||
        root.SelectSingleNode("//span[@id='productTitle']") is not null;

    private static PageFields ReadAmazon(HtmlNode root)
    {
        var name = FirstText(
            () => ReadText(root.SelectSingleNode("//span[@id='productTitle']")),
            () => ReadText(root.SelectSingleNode("//h1[@id='title']")));

        string? price = null;
        foreach (var selector in _amazonPriceSelectors)
        {
            var node = root.SelectSingleNode(selector);
            var text = node?.Name == "input" ? Attribute(node, "value") : ReadText(node);
            if (ProductLink.TryParsePrice(text, out _))
            {
                price = text;
                break;
            }
        }

        if (price is null)
        {
            var whole = ReadText(root.SelectSingleNode("//span[contains(@class, 'a-price-whole')]"));
            var fraction = ReadText(root.SelectSingleNode("//span[contains(@class, 'a-price-fraction')]"));
            var wholeDigits = new string((whole ?? string.Empty).Where(char.IsAsciiDigit).ToArray());
            if (wholeDigits.Length > 0)
            {
                // The whole part carries the store's separators ("1.299," on amazon.de), so keep only its digits.
                var fractionDigits = new string((fraction ?? string.Empty).Where(char.IsAsciiDigit).ToArray());
                price = wholeDigits + "." + (fractionDigits.Length > 0 ? fractionDigits : "00");
            }
        }

        string? image = null;
        var landingImage = root.SelectSingleNode("//img[@id='landingImage']") ??
                           root.SelectSingleNode("//img[@id='imgBlkFront']") ??
                           root.SelectSingleNode("//img[@id='ebooksImgBlkFront']") ??
                           root.SelectSingleNode("//div[@id='imgTagWrapperId']//img");
        if (landingImage is not null)
        {
            image = Attribute(landingImage, "data-old-hires") ??
                    ReadLargestDynamicImage(Attribute(landingImage, "data-a-dynamic-image")) ??
                    Attribute(landingImage, "src");
        }

        string? description = null;
        var bullets = root.SelectNodes("//div[@id='feature-bullets']//li//span[contains(@class, 'a-list-item')]");
        if (bullets is not null)
        {
            description = string.Join(' ', bullets
                .Select(bullet => ReadText(bullet))
                .Where(text => text is { Length: > 3 })
                .Take(3));
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            description = FirstText(
                () => ReadText(root.SelectSingleNode("//div[@id='bookDescription_feature_div']//noscript")),
                () => ReadText(root.SelectSingleNode("//div[@id='productDescription']")));
        }

        return new PageFields(name, description, price, image);
    }

    private static string? ReadLargestDynamicImage(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            string? best = null;
            var bestWidth = -1;
            foreach (var property in document.RootElement.EnumerateObject())
            {
                var width = property.Value.ValueKind == JsonValueKind.Array &&
                            property.Value.GetArrayLength() > 0 &&
                            property.Value[0].TryGetInt32(out var parsedWidth)
                    ? parsedWidth
                    : 0;
                if (width > bestWidth)
                {
                    bestWidth = width;
                    best = property.Name;
                }
            }

            return best;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static PageFields ReadMicrodata(HtmlNode root)
    {
        var scope = root.SelectSingleNode(
            "//*[@itemtype and contains(translate(@itemtype, 'PRODUCT', 'product'), 'schema.org/product')]");
        var prefix = scope is null ? "//" : ".//";
        scope ??= root;
        return new PageFields(
            ReadItemProp(scope.SelectSingleNode($"{prefix}*[@itemprop='name']")),
            ReadItemProp(scope.SelectSingleNode($"{prefix}*[@itemprop='description']")),
            ReadItemProp(scope.SelectSingleNode($"{prefix}*[@itemprop='price']")) ??
            ReadItemProp(scope.SelectSingleNode($"{prefix}*[@itemprop='lowPrice']")),
            ReadImageSource(scope.SelectSingleNode($"{prefix}*[@itemprop='image']")));
    }

    private static string? ReadItemProp(HtmlNode? node)
    {
        if (node is null)
        {
            return null;
        }

        return Attribute(node, "content") ?? (node.Name == "meta" ? null : ReadText(node));
    }

    private static JsonLdProduct? ReadJsonLd(HtmlNode root)
    {
        var scripts = root.SelectNodes("//script[@type]");
        if (scripts is null)
        {
            return null;
        }

        JsonLdProduct? best = null;
        foreach (var script in scripts)
        {
            if (!script.GetAttributeValue("type", string.Empty).Contains("ld+json", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var json = CleanJsonLd(script.InnerHtml);
            if (json is null)
            {
                continue;
            }

            try
            {
                using var document = JsonDocument.Parse(json, _jsonLdOptions);
                var productElement = FindProduct(document.RootElement, 0);
                if (productElement is null)
                {
                    continue;
                }

                var product = ReadJsonLdProduct(productElement.Value);
                if (best is null || (best.Price is null && product.Price is not null))
                {
                    best = product;
                }

                if (best.Name is not null && best.Price is not null)
                {
                    break;
                }
            }
            catch (JsonException)
            {
                // Stores often publish malformed JSON-LD; the other sources still apply.
            }
        }

        return best;
    }

    private static string? CleanJsonLd(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw) || raw.Length > MaxJsonLdLength)
        {
            return null;
        }

        var json = raw.Trim();
        foreach (var wrapper in new[] { "//<![CDATA[", "//]]>", "<![CDATA[", "]]>", "<!--", "-->" })
        {
            json = json.Replace(wrapper, string.Empty, StringComparison.Ordinal);
        }

        // Raw line breaks inside JSON strings are invalid but common in store markup.
        return json.Replace('\r', ' ').Replace('\n', ' ').Replace('\t', ' ').Trim();
    }

    private static JsonElement? FindProduct(JsonElement element, int depth)
    {
        if (depth > MaxJsonLdDepth)
        {
            return null;
        }

        if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                var found = FindProduct(item, depth + 1);
                if (found is not null)
                {
                    return found;
                }
            }

            return null;
        }

        if (element.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        if (HasProductType(element))
        {
            return element;
        }

        foreach (var property in element.EnumerateObject())
        {
            if (property.Value.ValueKind is JsonValueKind.Object or JsonValueKind.Array)
            {
                var found = FindProduct(property.Value, depth + 1);
                if (found is not null)
                {
                    return found;
                }
            }
        }

        return null;
    }

    private static bool HasProductType(JsonElement element)
    {
        if (!element.TryGetProperty("@type", out var type))
        {
            return false;
        }

        return type.ValueKind switch
        {
            JsonValueKind.String => IsProductType(type.GetString()),
            JsonValueKind.Array => type.EnumerateArray()
                .Any(item => item.ValueKind == JsonValueKind.String && IsProductType(item.GetString())),
            _ => false
        };
    }

    private static bool IsProductType(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var lastSlash = value.LastIndexOf('/');
        var typeName = lastSlash >= 0 ? value[(lastSlash + 1)..] : value;
        return _productTypes.Contains(typeName, StringComparer.OrdinalIgnoreCase);
    }

    private static JsonLdProduct ReadJsonLdProduct(JsonElement product)
    {
        var name = ReadJsonString(product, "name");
        var description = ReadJsonString(product, "description");
        var image = product.TryGetProperty("image", out var imageElement) ? ReadJsonImage(imageElement, 0) : null;
        var price = product.TryGetProperty("offers", out var offers) ? ReadJsonPrice(offers, 0) : null;
        if (product.TryGetProperty("hasVariant", out var variants) && variants.ValueKind == JsonValueKind.Array)
        {
            foreach (var variant in variants.EnumerateArray())
            {
                if (variant.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                if (image is null && variant.TryGetProperty("image", out var variantImage))
                {
                    image = ReadJsonImage(variantImage, 0);
                }

                if (price is null && variant.TryGetProperty("offers", out var variantOffers))
                {
                    price = ReadJsonPrice(variantOffers, 0);
                }

                if (image is not null && price is not null)
                {
                    break;
                }
            }
        }

        return new JsonLdProduct(name, description, price, image);
    }

    private static string? ReadJsonString(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var value))
        {
            return null;
        }

        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString(),
            JsonValueKind.Array => value.EnumerateArray()
                .Where(item => item.ValueKind == JsonValueKind.String)
                .Select(item => item.GetString())
                .FirstOrDefault(item => !string.IsNullOrWhiteSpace(item)),
            JsonValueKind.Object when value.TryGetProperty("@value", out var literal) && literal.ValueKind == JsonValueKind.String =>
                literal.GetString(),
            _ => null
        };
    }

    private static string? ReadJsonImage(JsonElement element, int depth)
    {
        if (depth > 4)
        {
            return null;
        }

        switch (element.ValueKind)
        {
            case JsonValueKind.String:
                return element.GetString();
            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                {
                    var image = ReadJsonImage(item, depth + 1);
                    if (!string.IsNullOrWhiteSpace(image))
                    {
                        return image;
                    }
                }

                return null;
            case JsonValueKind.Object:
                return ReadJsonString(element, "url") ?? ReadJsonString(element, "contentUrl") ?? ReadJsonString(element, "@id");
            default:
                return null;
        }
    }

    private static string? ReadJsonPrice(JsonElement element, int depth)
    {
        if (depth > 4)
        {
            return null;
        }

        if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                var price = ReadJsonPrice(item, depth + 1);
                if (price is not null)
                {
                    return price;
                }
            }

            return null;
        }

        if (element.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        foreach (var propertyName in new[] { "price", "lowPrice", "highPrice" })
        {
            if (element.TryGetProperty(propertyName, out var value))
            {
                var text = value.ValueKind switch
                {
                    JsonValueKind.Number => value.GetRawText(),
                    JsonValueKind.String => value.GetString(),
                    _ => null
                };
                if (ProductLink.TryParsePrice(text, out _))
                {
                    return text;
                }
            }
        }

        foreach (var propertyName in new[] { "priceSpecification", "offers" })
        {
            if (element.TryGetProperty(propertyName, out var nested))
            {
                var price = ReadJsonPrice(nested, depth + 1);
                if (price is not null)
                {
                    return price;
                }
            }
        }

        return null;
    }

    private static string? SelectText(HtmlNode root, IEnumerable<string> selectors)
    {
        foreach (var selector in selectors)
        {
            var text = ReadText(root.SelectSingleNode(selector));
            if (text is not null)
            {
                return text;
            }
        }

        return null;
    }

    private static string? ReadImageSource(HtmlNode? node)
    {
        if (node is null)
        {
            return null;
        }

        return node.Name switch
        {
            "meta" => Attribute(node, "content"),
            "link" or "a" => Attribute(node, "href"),
            "img" or "source" => Attribute(node, "src") ?? Attribute(node, "data-src") ??
                                 FirstSrcSetCandidate(Attribute(node, "srcset") ?? Attribute(node, "data-srcset")),
            _ => Attribute(node, "content") ?? Attribute(node, "src") ?? Attribute(node, "href")
        };
    }

    private static string? FirstSrcSetCandidate(string? srcSet)
    {
        var first = srcSet?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault();
        return first?.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
    }

    private static string? Attribute(HtmlNode node, string name)
    {
        var value = node.GetAttributeValue(name, null);
        return string.IsNullOrWhiteSpace(value) ? null : HtmlEntity.DeEntitize(value).Trim();
    }

    private static string? ReadText(HtmlNode? node, int maxLength = int.MaxValue)
    {
        if (node is null)
        {
            return null;
        }

        var text = ProductLink.NormalizeText(node.InnerText);
        return text is null || text.Length > maxLength ? null : text;
    }

    private static string? FirstText(params Func<string?>[] sources)
    {
        foreach (var source in sources)
        {
            var text = ProductLink.NormalizeText(source());
            if (text is not null)
            {
                return text;
            }
        }

        return null;
    }

    private static decimal? FirstPrice(params Func<string?>[] sources)
    {
        foreach (var source in sources)
        {
            if (ProductLink.TryParsePrice(source(), out var price))
            {
                return price;
            }
        }

        return null;
    }

    private static void AddImage(List<string> images, string? image)
    {
        if (!string.IsNullOrWhiteSpace(image) && !images.Contains(image.Trim(), StringComparer.Ordinal))
        {
            images.Add(image.Trim());
        }
    }

    private sealed record PageFields(string? Name, string? Description, string? Price, string? Image);

    private sealed record JsonLdProduct(string? Name, string? Description, string? Price, string? Image);
}