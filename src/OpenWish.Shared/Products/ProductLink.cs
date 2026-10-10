using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace OpenWish.Shared.Products;

/// <summary>
/// A product link found in pasted or shared text, plus any title that came with it.
/// </summary>
public sealed record SharedProductLink(Uri Url, string? TitleHint);

/// <summary>
/// Pure helpers for turning whatever a person pasted or shared into a clean product link.
/// These run on both the server and the WebAssembly client, so they never touch the network.
/// </summary>
public static partial class ProductLink
{
    public const int MaxNameLength = 120;
    public const int MaxDescriptionLength = 500;
    public const decimal MaxPrice = 10_000_000m;

    private const int PreferredNameLength = 100;
    private const int MinimumNamePrefixLength = 20;

    [GeneratedRegex(@"https?://[^\s<>""'`]+", RegexOptions.IgnoreCase)]
    private static partial Regex SchemeUrlRegex();

    [GeneratedRegex(@"(?<![\w@./-])www\.[a-z0-9-]+(?:\.[a-z0-9-]+)+(?::\d+)?(?:/[^\s<>""'`]*)?", RegexOptions.IgnoreCase)]
    private static partial Regex WwwUrlRegex();

    [GeneratedRegex(@"^[a-z0-9-]+(?:\.[a-z0-9-]+)*\.[a-z]{2,}(?::\d+)?(?:/\S*)?$", RegexOptions.IgnoreCase)]
    private static partial Regex BareDomainRegex();

    [GeneratedRegex(@"/(?:dp|gp/product|gp/aw/d|exec/obidos/asin|o/asin)/([A-Z0-9]{10})(?:[/?]|$)", RegexOptions.IgnoreCase)]
    private static partial Regex AmazonAsinRegex();

    [GeneratedRegex(@"^(?:www\.|smile\.)?amazon\.(?:com|ca|com\.mx|com\.br|co\.uk|de|fr|it|es|nl|se|pl|com\.be|ie|com\.tr|ae|sa|eg|in|sg|com\.au|co\.jp)$", RegexOptions.IgnoreCase)]
    private static partial Regex AmazonHostRegex();

    [GeneratedRegex(@"^/listing/(\d+)(?:/([^/?#]+))?", RegexOptions.IgnoreCase)]
    private static partial Regex EtsyListingRegex();

    [GeneratedRegex(@"\.(?:html?|jsp|php|aspx?|do|cfm|p|jpe?g|png|gif|webp|avif)$", RegexOptions.IgnoreCase)]
    private static partial Regex PageExtensionRegex();

    [GeneratedRegex(@"[._-]+(?:product|prd|item|sku|p|ip)?[._-]*\d{7,}$", RegexOptions.IgnoreCase)]
    private static partial Regex TrailingIdRegex();

    [GeneratedRegex(@"^[a-z]{2}(?:[-_][a-z]{2})?$", RegexOptions.IgnoreCase)]
    private static partial Regex LocaleSegmentRegex();

    [GeneratedRegex(@"[-_+\s]+")]
    private static partial Regex SlugSeparatorRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();

    [GeneratedRegex(@"^\d+(?:\.\d+)?$")]
    private static partial Regex PlainDecimalRegex();

    [GeneratedRegex(@"\d{1,3}(?:[.,\s\u00A0\u202F']\d{3})+(?:[.,]\d{1,2})?(?!\d)|\d+(?:[.,]\d{1,2})?(?!\d)")]
    private static partial Regex PriceTokenRegex();

    [GeneratedRegex(@"^amazon(?:\.[a-z.]+)?\s*:\s*", RegexOptions.IgnoreCase)]
    private static partial Regex AmazonTitlePrefixRegex();

    [GeneratedRegex(@"\s:\s[^:|]{2,48}$")]
    private static partial Regex AmazonCategorySuffixRegex();

    [GeneratedRegex(@"\s+(?:\||-|–|—|:|::|·|•)\s+")]
    private static partial Regex TitleSeparatorRegex();

    [GeneratedRegex(@"^(?:check\s+(?:this|it)\s+out|check\s+out(?:\s+(?:this|these|that))?|look\s+what\s+i\s+found|i\s+found\s+this|found\s+this|i\s+(?:want|like|love)\s+(?:this|these)|sharing|shared)\b[\s:!.,-]*", RegexOptions.IgnoreCase)]
    private static partial Regex ShareLeadInRegex();

    [GeneratedRegex(@"^(?<text>.*?)[\s,]*\b(?:on|at|from|via|in)\s+(?<store>[^:!,]{1,40}?)\s*[:!.,-]*$", RegexOptions.IgnoreCase)]
    private static partial Regex ShareStoreTailRegex();

    [GeneratedRegex(@"^(?:(?:this|these|it|that|an?)\s*)?(?:(?:great|cool|awesome|amazing|nice|new)\s*)?(?:deal|product|item|listing|find|thing|page|one|gift|idea)?s?$", RegexOptions.IgnoreCase)]
    private static partial Regex GenericShareTextRegex();

    private static readonly string[] _trackingParameterPrefixes =
    [
        "utm_", "pd_rd_", "pf_rd_", "_ga", "_gl", "mc_", "hsa_", "_hs", "_branch_", "trk_", "ga_", "pk_", "mtm_",
        "oly_", "vero_", "_trk"
    ];

    private static readonly HashSet<string> _trackingParameters = new(StringComparer.OrdinalIgnoreCase)
    {
        "fbclid", "gclid", "gclsrc", "dclid", "gbraid", "wbraid", "msclkid", "yclid", "twclid", "ttclid", "li_fat_id",
        "rdt_cid", "igshid", "igsh", "mkt_tok", "srsltid", "epik", "irclickid", "irgwc", "clickid", "cjevent", "cjdata",
        "ref", "ref_", "ref_src", "refsrc", "spm", "scm", "share_id", "cmpid", "s_kwcid", "ef_id", "affid", "aff_id",
        "wt.mc_id", "trk", "trkcampaign", "hsctatracking", "_pos", "_sid", "_ss", "_fid", "_psq", "mkevt", "mkcid",
        "mkrid", "campid", "customid", "toolid", "si", "dm_persistentcookiecreated", "oosredirected"
    };

    private static readonly HashSet<string> _amazonOnlyTrackingParameters = new(StringComparer.OrdinalIgnoreCase)
    {
        "tag", "linkcode", "linkid", "ascsubtag", "smid", "_encoding", "content-id", "crid", "sprefix", "qid", "sr",
        "dib", "dib_tag", "th", "psc", "camp", "creative", "creativeasin"
    };

    private static readonly HashSet<string> _genericPathWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "product", "products", "shop", "store", "item", "items", "detail", "details", "view", "cart", "catalog",
        "collection", "collections", "category", "categories", "search", "sale", "home", "index", "default",
        "listing", "listings", "site", "page", "pages", "buy", "new", "browse"
    };

    // Shopping hosts whose links people paste most often.
    private static readonly (string Domain, string Name)[] _knownStores =
    [
        ("a.co", "Amazon"), ("amzn.to", "Amazon"), ("amzn.com", "Amazon"), ("amzn.eu", "Amazon"), ("amzn.asia", "Amazon"),
        ("target.com", "Target"), ("walmart.com", "Walmart"), ("walmart.ca", "Walmart"), ("bestbuy.com", "Best Buy"),
        ("bestbuy.ca", "Best Buy"), ("etsy.com", "Etsy"), ("etsy.me", "Etsy"), ("homedepot.com", "The Home Depot"),
        ("lowes.com", "Lowe's"), ("costco.com", "Costco"), ("kohls.com", "Kohl's"), ("rei.com", "REI"), ("lego.com", "LEGO"),
        ("ikea.com", "IKEA"), ("nordstrom.com", "Nordstrom"), ("macys.com", "Macy's"), ("wayfair.com", "Wayfair"),
        ("barnesandnoble.com", "Barnes & Noble"), ("apple.com", "Apple"), ("nike.com", "Nike"), ("adidas.com", "adidas"),
        ("sephora.com", "Sephora"), ("ulta.com", "Ulta Beauty"), ("williams-sonoma.com", "Williams Sonoma"),
        ("potterybarn.com", "Pottery Barn"), ("westelm.com", "West Elm"), ("crateandbarrel.com", "Crate & Barrel"),
        ("dickssportinggoods.com", "Dick's Sporting Goods"), ("oldnavy.gap.com", "Old Navy"), ("gap.com", "Gap"),
        ("uniqlo.com", "Uniqlo"), ("zappos.com", "Zappos"), ("chewy.com", "Chewy"), ("newegg.com", "Newegg"),
        ("bhphotovideo.com", "B&H Photo"), ("gamestop.com", "GameStop"), ("michaels.com", "Michaels"),
        ("joann.com", "JOANN"), ("aliexpress.com", "AliExpress"), ("temu.com", "Temu"), ("shein.com", "SHEIN"),
        ("bookshop.org", "Bookshop.org"), ("powells.com", "Powell's Books"), ("anthropologie.com", "Anthropologie"),
        ("urbanoutfitters.com", "Urban Outfitters"), ("jcrew.com", "J.Crew"), ("llbean.com", "L.L.Bean"),
        ("patagonia.com", "Patagonia"), ("lululemon.com", "lululemon"), ("samsclub.com", "Sam's Club"),
        ("overstock.com", "Overstock"), ("staples.com", "Staples"), ("officedepot.com", "Office Depot"),
        ("petsmart.com", "PetSmart"), ("petco.com", "Petco"), ("acehardware.com", "Ace Hardware"),
        ("cabelas.com", "Cabela's"), ("basspro.com", "Bass Pro Shops"), ("guitarcenter.com", "Guitar Center"),
        ("sweetwater.com", "Sweetwater"), ("nintendo.com", "Nintendo"), ("playstation.com", "PlayStation"),
        ("xbox.com", "Xbox"), ("microsoft.com", "Microsoft"), ("dell.com", "Dell"), ("lenovo.com", "Lenovo"),
        ("samsung.com", "Samsung"), ("hm.com", "H&M"), ("zara.com", "Zara"), ("asos.com", "ASOS"),
        ("argos.co.uk", "Argos"), ("johnlewis.com", "John Lewis"), ("currys.co.uk", "Currys"),
        ("canadiantire.ca", "Canadian Tire"), ("indigo.ca", "Indigo"), ("allbirds.com", "Allbirds")
    ];

    /// <summary>
    /// True when text contains an http(s):// or www. web address. This matches the browser's paste
    /// check in app.js, which lets OpenWish take over a paste only when it contains a link.
    /// </summary>
    public static bool ContainsWebAddress(string? text) =>
        !string.IsNullOrWhiteSpace(text) && (SchemeUrlRegex().IsMatch(text) || WwwUrlRegex().IsMatch(text));

    /// <summary>
    /// Finds the first web link in pasted or shared text. Text around the link, such as the
    /// product title a shopping app includes when sharing, is returned as a title hint.
    /// </summary>
    public static bool TryParse(string? text, [NotNullWhen(true)] out SharedProductLink? link)
    {
        link = null;
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var trimmed = text.Trim();
        var match = SchemeUrlRegex().Match(trimmed);
        string candidate;
        if (match.Success)
        {
            candidate = match.Value;
        }
        else
        {
            match = WwwUrlRegex().Match(trimmed);
            if (match.Success)
            {
                candidate = "https://" + match.Value;
            }
            else if (!trimmed.Any(char.IsWhiteSpace) && BareDomainRegex().IsMatch(trimmed))
            {
                candidate = "https://" + trimmed;
            }
            else
            {
                return false;
            }
        }

        candidate = TrimTrailingPunctuation(candidate);
        if (!Uri.TryCreate(candidate, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) ||
            string.IsNullOrWhiteSpace(uri.Host))
        {
            return false;
        }

        string? titleHint = null;
        if (match.Success)
        {
            var surroundingText = trimmed.Remove(match.Index, match.Length);
            titleHint = CleanShareText(surroundingText, GetStoreName(uri));
        }

        link = new SharedProductLink(uri, titleHint);
        return true;
    }

    /// <summary>
    /// Removes tracking parameters and reduces well-known product links to their stable form.
    /// Parameters that may select a variant are kept.
    /// </summary>
    public static Uri Clean(Uri uri)
    {
        ArgumentNullException.ThrowIfNull(uri);
        if (!uri.IsAbsoluteUri || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            return uri;
        }

        var host = uri.Host.ToLowerInvariant();
        var isAmazon = AmazonHostRegex().IsMatch(host);
        if (isAmazon)
        {
            var asinMatch = AmazonAsinRegex().Match(uri.AbsolutePath);
            if (asinMatch.Success)
            {
                var amazonHost = host.StartsWith("smile.", StringComparison.Ordinal) ? "www." + host["smile.".Length..] : host;
                return new Uri($"https://{amazonHost}/dp/{asinMatch.Groups[1].Value.ToUpperInvariant()}");
            }
        }

        if (host is "etsy.com" or "www.etsy.com")
        {
            var listingMatch = EtsyListingRegex().Match(uri.AbsolutePath);
            if (listingMatch.Success)
            {
                var slug = listingMatch.Groups[2].Success ? "/" + listingMatch.Groups[2].Value : string.Empty;
                return new Uri($"https://www.etsy.com/listing/{listingMatch.Groups[1].Value}{slug}");
            }
        }

        var builder = new UriBuilder(uri);
        if (builder.Fragment.StartsWith("#:~:", StringComparison.Ordinal) ||
            builder.Fragment.StartsWith("#lnk=", StringComparison.Ordinal))
        {
            builder.Fragment = string.Empty;
        }

        if ((host.EndsWith("walmart.com", StringComparison.Ordinal) || host.EndsWith("walmart.ca", StringComparison.Ordinal)) &&
            uri.AbsolutePath.StartsWith("/ip/", StringComparison.OrdinalIgnoreCase))
        {
            builder.Query = string.Empty;
        }
        else if (!string.IsNullOrEmpty(uri.Query))
        {
            var keptParameters = uri.Query.TrimStart('?')
                .Split('&', StringSplitOptions.RemoveEmptyEntries)
                .Where(parameter => !IsTrackingParameter(parameter, isAmazon))
                .ToArray();
            builder.Query = string.Join('&', keptParameters);
        }

        if (builder.Uri.IsDefaultPort)
        {
            builder.Port = -1;
        }

        return builder.Uri;
    }

    /// <summary>
    /// Builds a readable name from a product link's path, for stores that will not share
    /// their page with a server. Returns <c>null</c> when the link has no descriptive words.
    /// </summary>
    public static string? GuessNameFromUrl(Uri uri)
    {
        ArgumentNullException.ThrowIfNull(uri);
        if (!uri.IsAbsoluteUri)
        {
            return null;
        }

        string? best = null;
        string? lastSegmentName = null;
        var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        for (var index = 0; index < segments.Length; index++)
        {
            var segment = Uri.UnescapeDataString(segments[index].Replace('+', ' ')).Trim();
            if (LocaleSegmentRegex().IsMatch(segment) || segment.Contains('='))
            {
                continue;
            }

            segment = PageExtensionRegex().Replace(segment, string.Empty);
            segment = TrailingIdRegex().Replace(segment, string.Empty);
            var words = SlugSeparatorRegex().Split(segment)
                .Select(word => word.Trim('.', ',', ';'))
                .Where(word => word.Length > 0 && !LooksLikeIdentifier(word))
                .ToArray();
            var letterWords = words.Count(word => word.Any(char.IsLetter));
            var letterCount = words.Sum(word => word.Count(char.IsLetter));
            if (words.Length >= 2 && letterWords >= 2 && letterCount >= 5)
            {
                // Product slugs usually follow category segments, so the last descriptive one wins.
                best = string.Join(' ', words);
            }
            else if (index == segments.Length - 1 &&
                     letterCount >= 4 &&
                     words.Any(word => word.Length >= 3 && word.All(char.IsLetter) && !_genericPathWords.Contains(word)))
            {
                // A short final segment such as "orchid-10311" still names the product.
                lastSegmentName = string.Join(' ', words);
            }
        }

        best ??= lastSegmentName;
        if (best is null)
        {
            return null;
        }

        if (!best.Any(char.IsUpper))
        {
            best = char.ToUpper(best[0], CultureInfo.InvariantCulture) + best[1..];
        }

        return CleanProductName(best, null);
    }

    /// <summary>
    /// Returns a friendly store name: a well-known shop name, then the page's own site name,
    /// then the host name without "www.".
    /// </summary>
    public static string GetStoreName(Uri uri, string? siteName = null)
    {
        ArgumentNullException.ThrowIfNull(uri);
        var host = uri.Host.ToLowerInvariant();
        if (host.StartsWith("www.", StringComparison.Ordinal))
        {
            host = host[4..];
        }

        if (AmazonHostRegex().IsMatch(host))
        {
            return "Amazon";
        }

        if (host.StartsWith("ebay.", StringComparison.Ordinal) || host.Contains(".ebay.", StringComparison.Ordinal))
        {
            return "eBay";
        }

        foreach (var (domain, name) in _knownStores)
        {
            if (host == domain || host.EndsWith("." + domain, StringComparison.Ordinal))
            {
                return name;
            }
        }

        var cleanSiteName = NormalizeText(siteName);
        if (cleanSiteName is { Length: >= 2 and <= 40 } && !cleanSiteName.Contains("://", StringComparison.Ordinal))
        {
            return cleanSiteName;
        }

        return host;
    }

    /// <summary>
    /// True when a title is only the store's name, such as "Amazon.com" on a verification page.
    /// </summary>
    public static bool IsStoreName(string? value, string? storeName) =>
        !string.IsNullOrWhiteSpace(value) && IsStoreLabel(value.Trim(), storeName);

    /// <summary>
    /// Reads a price from text such as "$1,299.99", "1.299,99 €", "12", or "$10 - $20".
    /// Returns the first price rounded to cents; zero and negative prices are ignored.
    /// </summary>
    public static bool TryParsePrice(string? text, out decimal price)
    {
        price = 0;
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var trimmed = text.Trim();
        if (trimmed[0] is '-' or '\u2212')
        {
            return false;
        }

        if (!PlainDecimalRegex().IsMatch(trimmed) || !decimal.TryParse(trimmed, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var value))
        {
            var match = PriceTokenRegex().Match(trimmed);
            if (!match.Success)
            {
                return false;
            }

            var token = match.Value.Replace(" ", string.Empty).Replace("\u00A0", string.Empty).Replace("\u202F", string.Empty).Replace("'", string.Empty);
            var lastDot = token.LastIndexOf('.');
            var lastComma = token.LastIndexOf(',');
            if (lastDot >= 0 && lastComma >= 0)
            {
                var decimalSeparator = lastDot > lastComma ? '.' : ',';
                var groupSeparator = decimalSeparator == '.' ? ',' : '.';
                token = token.Replace(groupSeparator.ToString(), string.Empty).Replace(decimalSeparator, '.');
            }
            else if (lastComma >= 0 || lastDot >= 0)
            {
                var separator = lastComma >= 0 ? ',' : '.';
                var separatorCount = token.Count(character => character == separator);
                var digitsAfter = token.Length - token.LastIndexOf(separator) - 1;
                token = separatorCount == 1 && digitsAfter is 1 or 2
                    ? token.Replace(separator, '.')
                    : token.Replace(separator.ToString(), string.Empty);
            }

            if (!decimal.TryParse(token, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out value))
            {
                return false;
            }
        }

        value = Math.Round(value, 2, MidpointRounding.AwayFromZero);
        if (value <= 0 || value > MaxPrice)
        {
            return false;
        }

        price = value;
        return true;
    }

    /// <summary>
    /// Tidies a product name from a page or share text: decodes entities, removes store
    /// prefixes and suffixes, and shortens very long marketplace titles.
    /// </summary>
    public static string? CleanProductName(string? value, string? storeName)
    {
        var name = NormalizeText(value);
        if (string.IsNullOrEmpty(name))
        {
            return null;
        }

        if (AmazonTitlePrefixRegex().IsMatch(name))
        {
            name = AmazonTitlePrefixRegex().Replace(name, string.Empty);
            name = AmazonCategorySuffixRegex().Replace(name, string.Empty);
        }

        name = RemoveStoreAffix(name, storeName);
        if (name.Length > PreferredNameLength)
        {
            name = Shorten(name);
        }

        name = name.Trim(' ', '|', '-', ':', '–', '—', ',');
        return name.Length == 0 ? null : name;
    }

    /// <summary>
    /// Normalizes a product description and limits it to a length that fits a wishlist card.
    /// </summary>
    public static string? CleanDescription(string? value, string? name = null)
    {
        var description = NormalizeText(value);
        if (string.IsNullOrEmpty(description) ||
            string.Equals(description, name, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return TruncateAtWord(description, MaxDescriptionLength);
    }

    /// <summary>
    /// Collapses whitespace, decodes HTML entities, and removes invisible characters.
    /// </summary>
    public static string? NormalizeText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var decoded = WebUtility.HtmlDecode(value);
        var builder = new StringBuilder(decoded.Length);
        foreach (var character in decoded)
        {
            builder.Append(char.IsControl(character) || character is '\u00A0' or '\u200B' or '\u200E' or '\u200F' or '\uFEFF'
                ? ' '
                : character);
        }

        var normalized = WhitespaceRegex().Replace(builder.ToString(), " ").Trim();
        return normalized.Length == 0 ? null : normalized;
    }

    private static string? CleanShareText(string text, string storeName)
    {
        string? best = null;
        foreach (var line in text.Split('\n'))
        {
            var candidate = NormalizeText(line);
            if (candidate is null)
            {
                continue;
            }

            candidate = ShareLeadInRegex().Replace(candidate, string.Empty);
            var storeTail = ShareStoreTailRegex().Match(candidate);
            if (storeTail.Success && IsStoreLabel(storeTail.Groups["store"].Value.Trim(), storeName))
            {
                candidate = storeTail.Groups["text"].Value;
            }

            candidate = candidate.Trim(' ', ':', '-', '|', '"', '\'', '“', '”', '‘', '’', '«', '»', '.', ',', '!', '(', ')');
            if (candidate.Length < 3 || !candidate.Any(char.IsLetter) || GenericShareTextRegex().IsMatch(candidate))
            {
                continue;
            }

            if (best is null || candidate.Length > best.Length)
            {
                best = candidate;
            }
        }

        return best is null || best.Length > 400 ? null : CleanProductName(best, storeName);
    }

    private static string RemoveStoreAffix(string name, string? storeName)
    {
        var separators = TitleSeparatorRegex().Matches(name);
        if (separators.Count == 0)
        {
            return name;
        }

        var lastSeparator = separators[^1];
        if (IsStoreLabel(name[(lastSeparator.Index + lastSeparator.Length)..].Trim(), storeName))
        {
            return name[..lastSeparator.Index].Trim();
        }

        var firstSeparator = separators[0];
        if (IsStoreLabel(name[..firstSeparator.Index].Trim(), storeName))
        {
            return name[(firstSeparator.Index + firstSeparator.Length)..].Trim();
        }

        return name;
    }

    private static bool IsStoreLabel(string part, string? storeName)
    {
        if (part.Length == 0 || part.Length > 48)
        {
            return false;
        }

        if (!part.Contains(' ') &&
            (part.EndsWith(".com", StringComparison.OrdinalIgnoreCase) ||
             part.Contains(".co.", StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(storeName))
        {
            return false;
        }

        var normalizedPart = StoreKey(part);
        var normalizedStore = StoreKey(storeName);
        return normalizedStore.Length >= 2 && normalizedPart == normalizedStore;
    }

    private static string StoreKey(string value)
    {
        var builder = new StringBuilder(value.Length);
        foreach (var character in value)
        {
            if (char.IsLetterOrDigit(character))
            {
                builder.Append(char.ToLowerInvariant(character));
            }
        }

        var key = builder.ToString();
        if (key.StartsWith("the", StringComparison.Ordinal) && key.Length > 6)
        {
            key = key[3..];
        }

        var trimmed = true;
        while (trimmed)
        {
            trimmed = false;
            foreach (var suffix in new[] { "officialsite", "officialstore", "onlinestore", "official", "online", "store", "shop", "com", "couk", "ca", "usa", "us", "uk", "canada" })
            {
                if (key.Length > suffix.Length + 2 && key.EndsWith(suffix, StringComparison.Ordinal))
                {
                    key = key[..^suffix.Length];
                    trimmed = true;
                    break;
                }
            }
        }

        return key;
    }

    private static string Shorten(string name)
    {
        var pipeIndex = name.IndexOf(" | ", StringComparison.Ordinal);
        if (pipeIndex >= MinimumNamePrefixLength && pipeIndex <= PreferredNameLength)
        {
            return name[..pipeIndex];
        }

        var commaIndex = name.IndexOf(", ", MinimumNamePrefixLength, StringComparison.Ordinal);
        if (commaIndex >= MinimumNamePrefixLength && commaIndex <= PreferredNameLength)
        {
            return name[..commaIndex];
        }

        return TruncateAtWord(name, MaxNameLength);
    }

    private static string TruncateAtWord(string value, int maxLength)
    {
        if (value.Length <= maxLength)
        {
            return value;
        }

        var cut = value.LastIndexOf(' ', maxLength - 1);
        if (cut < maxLength / 2)
        {
            cut = maxLength - 1;
        }

        return value[..cut].TrimEnd(' ', ',', ';', ':', '-', '|') + "…";
    }

    private static bool LooksLikeIdentifier(string word)
    {
        var digits = word.Count(char.IsDigit);
        return digits >= 5 && word.Length >= 7;
    }

    private static string TrimTrailingPunctuation(string url)
    {
        while (url.Length > 0)
        {
            var last = url[^1];
            if (".,;:!?\"'»›>*".Contains(last))
            {
                url = url[..^1];
                continue;
            }

            if ((last == ')' && url.Count(c => c == '(') < url.Count(c => c == ')')) ||
                (last == ']' && url.Count(c => c == '[') < url.Count(c => c == ']')))
            {
                url = url[..^1];
                continue;
            }

            break;
        }

        return url;
    }

    private static bool IsTrackingParameter(string parameter, bool isAmazon)
    {
        var separator = parameter.IndexOf('=');
        var name = Uri.UnescapeDataString(separator >= 0 ? parameter[..separator] : parameter);
        return _trackingParameters.Contains(name) ||
               (isAmazon && _amazonOnlyTrackingParameters.Contains(name)) ||
               _trackingParameterPrefixes.Any(prefix => name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
    }
}