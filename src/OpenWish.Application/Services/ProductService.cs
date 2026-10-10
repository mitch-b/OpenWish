using System.Buffers;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using OpenWish.Application.Products;
using OpenWish.Shared.Models;
using OpenWish.Shared.Products;
using OpenWish.Shared.Services;

namespace OpenWish.Application.Services;

public partial class ProductService : IProductService
{
    internal const string UserAgent = "Mozilla/5.0 (compatible; OpenWish/1.0; +https://github.com/mitch-b/OpenWish)";
    internal const int MaxResponseBytes = 4 * 1024 * 1024;

    private const int MaxRedirects = 5;
    private const int MaxUrlLength = 4096;
    private const int MaxImageCandidates = 3;
    private const string AcceptHeader = "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,*/*;q=0.8";
    private const string AcceptLanguageHeader = "en-US,en;q=0.9";

    private static readonly TimeSpan _defaultLookupTimeout = TimeSpan.FromSeconds(12);
    private static readonly TimeSpan _retryWindow = TimeSpan.FromSeconds(4);
    private static readonly TimeSpan _retryDelay = TimeSpan.FromMilliseconds(400);
    private static readonly TimeSpan _cacheDuration = TimeSpan.FromMinutes(30);

    private static readonly HashSet<string> _shortLinkHosts = new(StringComparer.OrdinalIgnoreCase)
    {
        "a.co", "amzn.to", "amzn.eu", "amzn.asia", "etsy.me", "bit.ly", "tinyurl.com", "t.co", "ow.ly", "buff.ly",
        "shopstyle.it", "liketk.it", "rstyle.me", "go.shopmy.us", "tidd.ly", "trib.al", "lnk.to", "sovrn.co"
    };

    [GeneratedRegex(@"<meta[^>]+charset\s*=\s*[""']?\s*([a-zA-Z0-9_.:-]+)", RegexOptions.IgnoreCase)]
    private static partial Regex MetaCharsetRegex();

    private readonly HttpClient _client;
    private readonly ILogger<ProductService> _logger;
    private readonly ProductLookupCache? _cache;

    static ProductService()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    public ProductService(IHttpClientFactory httpClientFactory, ILogger<ProductService> logger, ProductLookupCache? cache = null)
    {
        _client = httpClientFactory.CreateClient("ProductHttpClient");
        _logger = logger;
        _cache = cache;
    }

    /// <summary>
    /// The longest a single lookup may take before the link's own details are used instead.
    /// </summary>
    internal TimeSpan LookupTimeout { get; init; } = _defaultLookupTimeout;

    internal enum AddressSafety
    {
        Safe,
        Unsafe,
        Unresolved
    }

    /// <summary>
    /// Validates that a URL is safe to fetch: must use http/https and must not target
    /// publicly routable unicast addresses rather than special-purpose or private networks.
    /// </summary>
    internal static async Task<bool> IsSafeUrlAsync(Uri uri) =>
        await CheckAddressAsync(uri, CancellationToken.None) == AddressSafety.Safe;

    internal static async Task<AddressSafety> CheckAddressAsync(Uri uri, CancellationToken cancellationToken)
    {
        if (!uri.IsAbsoluteUri || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            return AddressSafety.Unsafe;
        }

        if (IPAddress.TryParse(uri.DnsSafeHost, out var literal))
        {
            return IsSafeAddress(literal) ? AddressSafety.Safe : AddressSafety.Unsafe;
        }

        if (string.Equals(uri.DnsSafeHost, "localhost", StringComparison.OrdinalIgnoreCase) ||
            uri.DnsSafeHost.EndsWith(".localhost", StringComparison.OrdinalIgnoreCase))
        {
            return AddressSafety.Unsafe;
        }

        IPAddress[] addresses;
        try
        {
            addresses = await Dns.GetHostAddressesAsync(uri.DnsSafeHost, cancellationToken);
        }
        catch (SocketException)
        {
            return AddressSafety.Unresolved;
        }
        catch (ArgumentException)
        {
            return AddressSafety.Unsafe;
        }

        if (addresses.Length == 0)
        {
            return AddressSafety.Unresolved;
        }

        return addresses.All(IsSafeAddress) ? AddressSafety.Safe : AddressSafety.Unsafe;
    }

    internal static bool IsSafeAddress(IPAddress address)
    {
        if (address.IsIPv4MappedToIPv6)
        {
            address = address.MapToIPv4();
        }

        if (IPAddress.IsLoopback(address) ||
            address.Equals(IPAddress.Any) ||
            address.Equals(IPAddress.IPv6Any) ||
            address.Equals(IPAddress.None) ||
            address.Equals(IPAddress.IPv6None))
        {
            return false;
        }

        var bytes = address.GetAddressBytes();
        if (address.AddressFamily == AddressFamily.InterNetwork)
        {
            return bytes[0] switch
            {
                0 or 10 or 127 => false,
                100 when bytes[1] is >= 64 and <= 127 => false,
                169 when bytes[1] == 254 => false,
                172 when bytes[1] is >= 16 and <= 31 => false,
                192 when bytes[1] == 0 && bytes[2] == 0 => false,
                192 when bytes[1] == 0 && bytes[2] == 2 => false,
                192 when bytes[1] == 168 => false,
                192 when bytes[1] == 88 && bytes[2] == 99 => false,
                198 when bytes[1] is 18 or 19 => false,
                198 when bytes[1] == 51 && bytes[2] == 100 => false,
                203 when bytes[1] == 0 && bytes[2] == 113 => false,
                >= 224 => false,
                _ => true
            };
        }

        if (address.AddressFamily != AddressFamily.InterNetworkV6)
        {
            return false;
        }

        var isGlobalUnicast = bytes[0] is >= 0x20 and <= 0x3f;
        var isIetfSpecialPurpose = bytes[0] == 0x20 &&
                                   bytes[1] == 0x01 &&
                                   ((bytes[2] == 0x00 && bytes[3] == 0x00) ||
                                    (bytes[2] == 0x00 && bytes[3] == 0x02 && bytes[4] == 0x00 && bytes[5] == 0x00) ||
                                    (bytes[2] == 0x00 && (bytes[3] & 0xf0) is 0x10 or 0x20) ||
                                    (bytes[2] == 0x0d && bytes[3] == 0xb8));
        var isSixToFour = bytes[0] == 0x20 && bytes[1] == 0x02;
        var isDocumentation = bytes[0] == 0x3f &&
                              bytes[1] == 0xff &&
                              (bytes[2] & 0xf0) == 0x00;

        return isGlobalUnicast && !isIetfSpecialPurpose && !isSixToFour && !isDocumentation;
    }

    public async Task<ProductModel?> TryScrapeProductFromUrl(string url, CancellationToken cancellationToken = default)
    {
        if (!TryReadProductUri(url, out var requestedUri, out var titleHint))
        {
            _logger.LogWarning("Rejected an invalid product URL.");
            return null;
        }

        var cleanUri = ProductLink.Clean(requestedUri);
        if (_cache?.TryGet(cleanUri, out var cached) == true && cached is not null)
        {
            return WithName(cached, titleHint, cleanUri);
        }

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(LookupTimeout);
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var safety = await CheckAddressAsync(cleanUri, timeout.Token);
            if (safety == AddressSafety.Unsafe)
            {
                _logger.LogWarning("Rejected an unsafe product URL.");
                return null;
            }

            if (safety == AddressSafety.Unresolved)
            {
                _logger.LogInformation("Product host {Host} could not be resolved; using details from the link.", cleanUri.Host);
                return FromLink(cleanUri, titleHint);
            }

            for (var attempt = 0; ; attempt++)
            {
                var page = await FetchPageAsync(cleanUri, timeout.Token);
                var product = await BuildProductAsync(page, cleanUri, timeout.Token);
                if (product is not null)
                {
                    _logger.LogInformation(
                        "Read product details from {Host} in {ElapsedMilliseconds} ms.",
                        page.Uri.Host,
                        stopwatch.ElapsedMilliseconds);
                    // Cache only what the store said; the caller's shared text stays with the caller.
                    _cache?.Set(cleanUri, product, _cacheDuration);
                    return WithName(product, titleHint, cleanUri);
                }

                if (attempt == 0 && page.MayRetry && stopwatch.Elapsed < _retryWindow)
                {
                    await Task.Delay(_retryDelay, timeout.Token);
                    continue;
                }

                _logger.LogInformation(
                    "Product page from {Host} was unavailable ({Reason}); using details from the link.",
                    page.Uri.Host,
                    page.Reason);
                return FromLink(LinkForFallback(cleanUri, page), titleHint, cleanUri, page.FallbackName);
            }
        }
        catch (UnsafeRedirectException)
        {
            _logger.LogWarning("Rejected a product URL that redirected to a non-public address.");
            return null;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogInformation("Product lookup for {Host} timed out; using details from the link.", cleanUri.Host);
            return FromLink(cleanUri, titleHint);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogInformation("Product page from {Host} could not be fetched ({Message}); using details from the link.", cleanUri.Host, ex.Message);
            return FromLink(cleanUri, titleHint);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Error reading product details from {Host}.", cleanUri.Host);
            return FromLink(cleanUri, titleHint);
        }
    }

    private static bool TryReadProductUri(string? url, out Uri uri, out string? titleHint)
    {
        uri = null!;
        titleHint = null;
        if (string.IsNullOrWhiteSpace(url) || url.Length > MaxUrlLength)
        {
            return false;
        }

        var trimmed = url.Trim();
        // Shared text can put words after the link, and Uri would otherwise escape them into the path.
        if (!trimmed.Any(char.IsWhiteSpace) &&
            Uri.TryCreate(trimmed, UriKind.Absolute, out var parsed) &&
            (parsed.Scheme == Uri.UriSchemeHttp || parsed.Scheme == Uri.UriSchemeHttps) &&
            !string.IsNullOrWhiteSpace(parsed.Host))
        {
            uri = parsed;
            return true;
        }

        if (ProductLink.TryParse(trimmed, out var link))
        {
            uri = link.Url;
            titleHint = link.TitleHint;
            return true;
        }

        return false;
    }

    private async Task<ProductModel?> BuildProductAsync(FetchedPage page, Uri requestedUri, CancellationToken cancellationToken)
    {
        if (page.IsImage)
        {
            return new ProductModel
            {
                ImageUrl = page.Uri.AbsoluteUri,
                Url = requestedUri.AbsoluteUri,
                StoreName = ProductLink.GetStoreName(requestedUri)
            };
        }

        if (page.Html is null)
        {
            return null;
        }

        if (IsDifferentPage(requestedUri, page.Uri))
        {
            // Stores often redirect discontinued or sold-out items to a category or another product.
            page.Reason = "redirected to a different page";
            page.RedirectedAway = true;
            return null;
        }

        var details = ProductPageParser.Parse(page.Html, page.Uri);
        if (details.IsBlocked)
        {
            page.Reason = "bot check";
            page.MayRetry = true;
            return null;
        }

        var pageUri = ProductLink.Clean(page.Uri);
        var storeName = ProductLink.GetStoreName(pageUri, details.SiteName);
        var pageName = ProductLink.CleanProductName(details.Name, storeName);
        if (ProductLink.IsStoreName(pageName, storeName) || ProductLink.IsStoreName(pageName, details.SiteName))
        {
            pageName = null;
        }

        var imageUrl = await ResolveImageAsync(details.ImageCandidates, page.Uri, cancellationToken);
        var description = ProductLink.CleanDescription(details.Description, pageName);
        if (details.Price is null && imageUrl is null && description is null)
        {
            // A bare page title is often an app shell or interstitial rather than the product.
            page.Reason = "no product details";
            page.FallbackName = pageName;
            return null;
        }

        return new ProductModel
        {
            Name = pageName,
            Description = description,
            Price = details.Price,
            ImageUrl = imageUrl,
            Url = pageUri.AbsoluteUri,
            StoreName = storeName
        };
    }

    /// <summary>
    /// Names a product read from a store page, preferring the page's own name, then the caller's
    /// shared text, then words from the link.
    /// </summary>
    private static ProductModel WithName(ProductModel product, string? titleHint, Uri requestedUri)
    {
        if (product.Name is not null)
        {
            return product;
        }

        var name = titleHint ??
                   (Uri.TryCreate(product.Url, UriKind.Absolute, out var productUri) ? ProductLink.GuessNameFromUrl(productUri) : null) ??
                   ProductLink.GuessNameFromUrl(requestedUri);
        return product with { Name = name };
    }

    private static ProductModel FromLink(Uri uri, string? titleHint, Uri? originalUri = null, string? pageName = null)
    {
        var name = titleHint ??
                   ProductLink.GuessNameFromUrl(uri) ??
                   (originalUri is null ? null : ProductLink.GuessNameFromUrl(originalUri)) ??
                   pageName;
        return new ProductModel
        {
            Name = name,
            Url = uri.AbsoluteUri,
            StoreName = ProductLink.GetStoreName(uri),
            FromLinkOnly = true
        };
    }

    internal static bool IsDifferentPage(Uri requestedUri, Uri pageUri)
    {
        var requestedPath = requestedUri.AbsolutePath.TrimEnd('/');
        var pagePath = pageUri.AbsolutePath.TrimEnd('/');
        if (string.Equals(requestedPath, pagePath, StringComparison.OrdinalIgnoreCase) ||
            _shortLinkHosts.Contains(requestedUri.Host))
        {
            return false;
        }

        if (pagePath.Length == 0)
        {
            return true;
        }

        var requestedWords = SignificantWords(ProductLink.GuessNameFromUrl(requestedUri));
        var pageWords = SignificantWords(ProductLink.GuessNameFromUrl(pageUri));
        return requestedWords.Count > 0 && pageWords.Count > 0 && !requestedWords.Overlaps(pageWords);
    }

    private static HashSet<string> SignificantWords(string? text) =>
        text is null
            ? []
            : text.Split((char[])[' ', ',', '.', '-', '&', '/', '(', ')'], StringSplitOptions.RemoveEmptyEntries)
                .Where(word => word.Length >= 3)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

    private static Uri LinkForFallback(Uri requestedUri, FetchedPage page)
    {
        if (page.RedirectedAway)
        {
            return requestedUri;
        }

        // A shortened link is less useful than where it leads, unless it led to a bot check URL.
        if (!_shortLinkHosts.Contains(requestedUri.Host) || page.Uri == requestedUri)
        {
            return requestedUri;
        }

        var path = page.Uri.AbsolutePath;
        return path.Contains("block", StringComparison.OrdinalIgnoreCase) ||
               path.Contains("captcha", StringComparison.OrdinalIgnoreCase) ||
               path.Contains("challenge", StringComparison.OrdinalIgnoreCase) ||
               path.Contains("verify", StringComparison.OrdinalIgnoreCase)
            ? requestedUri
            : ProductLink.Clean(page.Uri);
    }

    private async Task<string?> ResolveImageAsync(IReadOnlyList<string> candidates, Uri pageUri, CancellationToken cancellationToken)
    {
        foreach (var candidate in candidates.Take(MaxImageCandidates))
        {
            if (Uri.TryCreate(pageUri, candidate, out var imageUri) &&
                (imageUri.Scheme == Uri.UriSchemeHttp || imageUri.Scheme == Uri.UriSchemeHttps) &&
                await CheckAddressAsync(imageUri, cancellationToken) == AddressSafety.Safe)
            {
                return imageUri.AbsoluteUri;
            }
        }

        return null;
    }

    private async Task<FetchedPage> FetchPageAsync(Uri uri, CancellationToken cancellationToken)
    {
        var (response, pageUri) = await GetFollowingSafeRedirectsAsync(uri, cancellationToken);
        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                return new FetchedPage(pageUri)
                {
                    Reason = $"HTTP {(int)response.StatusCode}",
                    MayRetry = response.StatusCode is HttpStatusCode.ServiceUnavailable or HttpStatusCode.TooManyRequests
                };
            }

            var mediaType = response.Content.Headers.ContentType?.MediaType;
            if (mediaType?.StartsWith("image/", StringComparison.OrdinalIgnoreCase) == true)
            {
                return new FetchedPage(pageUri) { IsImage = true };
            }

            if (mediaType is not null &&
                !mediaType.Contains("html", StringComparison.OrdinalIgnoreCase) &&
                !mediaType.Contains("xml", StringComparison.OrdinalIgnoreCase) &&
                !mediaType.StartsWith("text/", StringComparison.OrdinalIgnoreCase))
            {
                return new FetchedPage(pageUri) { Reason = $"unsupported content type {mediaType}" };
            }

            var html = await ReadBoundedTextAsync(response.Content, cancellationToken);
            return new FetchedPage(pageUri) { Html = html };
        }
    }

    /// <summary>
    /// Reads at most <see cref="MaxResponseBytes"/>. Large store pages are truncated rather than
    /// rejected, because product metadata appears near the top of the document.
    /// </summary>
    internal static async Task<string> ReadBoundedTextAsync(HttpContent content, CancellationToken cancellationToken)
    {
        await using var stream = await content.ReadAsStreamAsync(cancellationToken);
        var buffer = ArrayPool<byte>.Shared.Rent(MaxResponseBytes);
        try
        {
            var length = 0;
            int read;
            while (length < MaxResponseBytes &&
                   (read = await stream.ReadAsync(buffer.AsMemory(length, MaxResponseBytes - length), cancellationToken)) > 0)
            {
                length += read;
            }

            var bytes = buffer.AsSpan(0, length);
            var encoding = DetectEncoding(content.Headers.ContentType?.CharSet, bytes, out var preambleLength);
            return encoding.GetString(bytes[preambleLength..]);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    private static Encoding DetectEncoding(string? headerCharset, ReadOnlySpan<byte> bytes, out int preambleLength)
    {
        preambleLength = 0;
        if (bytes.StartsWith(Encoding.UTF8.Preamble))
        {
            preambleLength = Encoding.UTF8.Preamble.Length;
            return Encoding.UTF8;
        }

        if (bytes.StartsWith(Encoding.Unicode.Preamble))
        {
            preambleLength = Encoding.Unicode.Preamble.Length;
            return Encoding.Unicode;
        }

        if (bytes.StartsWith(Encoding.BigEndianUnicode.Preamble))
        {
            preambleLength = Encoding.BigEndianUnicode.Preamble.Length;
            return Encoding.BigEndianUnicode;
        }

        var charset = headerCharset;
        if (string.IsNullOrWhiteSpace(charset))
        {
            var head = Encoding.ASCII.GetString(bytes[..Math.Min(bytes.Length, 4096)]);
            var match = MetaCharsetRegex().Match(head);
            charset = match.Success ? match.Groups[1].Value : null;
        }

        if (!string.IsNullOrWhiteSpace(charset))
        {
            try
            {
                return Encoding.GetEncoding(charset.Trim().Trim('"', '\''));
            }
            catch (ArgumentException)
            {
                // Unknown charsets fall back to UTF-8, which nearly all stores use.
            }
        }

        return Encoding.UTF8;
    }

    private async Task<(HttpResponseMessage Response, Uri PageUri)> GetFollowingSafeRedirectsAsync(Uri initialUri, CancellationToken cancellationToken)
    {
        var currentUri = initialUri;
        for (var redirect = 0; redirect <= MaxRedirects; redirect++)
        {
            switch (await CheckAddressAsync(currentUri, cancellationToken))
            {
                case AddressSafety.Unsafe when currentUri.Scheme == Uri.UriSchemeHttp || currentUri.Scheme == Uri.UriSchemeHttps:
                    throw new UnsafeRedirectException();
                case AddressSafety.Unsafe:
                    throw new HttpRequestException("The product URL redirected to an unsupported address.");
                case AddressSafety.Unresolved:
                    throw new HttpRequestException("The product URL redirected to an address that could not be resolved.");
            }

            using var request = new HttpRequestMessage(HttpMethod.Get, currentUri)
            {
                Version = _client.DefaultRequestVersion,
                VersionPolicy = _client.DefaultVersionPolicy
            };
            request.Headers.TryAddWithoutValidation("User-Agent", UserAgent);
            request.Headers.TryAddWithoutValidation("Accept", AcceptHeader);
            request.Headers.TryAddWithoutValidation("Accept-Language", AcceptLanguageHeader);

            var response = await _client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            if (!IsRedirect(response.StatusCode))
            {
                return (response, currentUri);
            }

            if (redirect == MaxRedirects)
            {
                response.Dispose();
                throw new HttpRequestException("The product URL exceeded the redirect limit.");
            }

            var location = response.Headers.Location;
            response.Dispose();
            if (location is null)
            {
                throw new HttpRequestException("The product URL returned a redirect without a location.");
            }

            currentUri = location.IsAbsoluteUri ? location : new Uri(currentUri, location);
        }

        throw new HttpRequestException("The product URL could not be retrieved.");
    }

    private static bool IsRedirect(HttpStatusCode statusCode) =>
        statusCode is HttpStatusCode.Moved or
            HttpStatusCode.Redirect or
            HttpStatusCode.RedirectMethod or
            HttpStatusCode.TemporaryRedirect or
            HttpStatusCode.PermanentRedirect;

    private sealed class FetchedPage(Uri uri)
    {
        public Uri Uri { get; } = uri;
        public string? Html { get; init; }
        public bool IsImage { get; init; }
        public string Reason { get; set; } = "unreadable page";
        public bool MayRetry { get; set; }
        public bool RedirectedAway { get; set; }
        public string? FallbackName { get; set; }
    }

    private sealed class UnsafeRedirectException : Exception;
}