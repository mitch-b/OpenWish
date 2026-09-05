using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using OpenWish.Application.Models;
using OpenWish.Shared.Models;
using OpenWish.Shared.Services;

namespace OpenWish.Application.Services;

public partial class ProductService : IProductService
{
    private const int MaxRedirects = 5;
    private const int MaxResponseBytes = 2 * 1024 * 1024;
    [GeneratedRegex(@"[^0-9.,]+")]
    private static partial Regex PriceParseRegex();

    private readonly HttpClient _client;
    private readonly ILogger<ProductService> _logger;
    private const string UserAgent = "OpenWish/1.0 (Compatible; Modern Browser)";

    public ProductService(IHttpClientFactory httpClientFactory, ILogger<ProductService> logger)
    {
        _client = httpClientFactory.CreateClient("ProductHttpClient");
        _client.DefaultRequestHeaders.Add("User-Agent", UserAgent);
        _client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("text/html"));
        _client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/xhtml+xml"));
        _client.DefaultRequestHeaders.AcceptLanguage.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("en-US"));
        _logger = logger;
    }

    /// <summary>
    /// Validates that a URL is safe to fetch: must use http/https and must not target
    /// publicly routable unicast addresses rather than special-purpose or private networks.
    /// </summary>
    internal static async Task<bool> IsSafeUrlAsync(Uri uri)
    {
        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
        {
            return false;
        }

        IPAddress[] addresses;
        try
        {
            addresses = await Dns.GetHostAddressesAsync(uri.DnsSafeHost);
        }
        catch
        {
            return false;
        }

        return addresses.Length > 0 && addresses.All(IsSafeAddress);
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

    public async Task<ProductModel?> TryScrapeProductFromUrl(string url)
    {
        try
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || !await IsSafeUrlAsync(uri))
            {
                _logger.LogWarning("Rejected an unsafe or invalid product URL.");
                return null;
            }

            using var response = await GetFollowingSafeRedirectsAsync(uri);

            await response.Content.LoadIntoBufferAsync(MaxResponseBytes);
            var html = await response.Content.ReadAsStringAsync();
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            string? name = TrySelectors(doc, ProductSelectors.TitleSelectors);
            string? description = TrySelectors(doc, ProductSelectors.DescriptionSelectors);
            string? price = TrySelectors(doc, ProductSelectors.PriceSelectors);
            string? imageUrl = TrySelectors(doc, ProductSelectors.ImageSelectors);

            if (!string.IsNullOrEmpty(imageUrl) && !imageUrl.StartsWith("http"))
            {
                imageUrl = new Uri(uri, imageUrl).AbsoluteUri;
            }

            if (!string.IsNullOrEmpty(imageUrl) &&
                (!Uri.TryCreate(imageUrl, UriKind.Absolute, out var imageUri) ||
                 !await IsSafeUrlAsync(imageUri)))
            {
                imageUrl = null;
            }

            decimal? parsedPrice = null;
            if (!string.IsNullOrEmpty(price))
            {
                price = PriceParseRegex().Replace(price, "");
                if (decimal.TryParse(price, out decimal value))
                {
                    parsedPrice = value;
                }
            }

            return new ProductModel
            {
                Name = name,
                Description = description,
                Price = parsedPrice,
                ImageUrl = imageUrl,
                Url = url
            };
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, $"Error fetching URL: {ex.Message}");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error parsing HTML: {ex.Message}");
            return null;
        }
    }

    private async Task<HttpResponseMessage> GetFollowingSafeRedirectsAsync(Uri initialUri)
    {
        var currentUri = initialUri;
        for (var redirect = 0; redirect <= MaxRedirects; redirect++)
        {
            if (!await IsSafeUrlAsync(currentUri))
            {
                throw new HttpRequestException("The requested address is not publicly routable.");
            }

            var response = await _client.GetAsync(currentUri, HttpCompletionOption.ResponseHeadersRead);
            if (!IsRedirect(response.StatusCode))
            {
                response.EnsureSuccessStatusCode();
                return response;
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

    private static string? TrySelectors(HtmlDocument doc, List<string> selectors)
    {
        foreach (var selector in selectors)
        {
            var node = doc.DocumentNode.SelectSingleNode(selector);
            if (node != null)
            {
                return node.Name == "meta" ? node.GetAttributeValue("content", null) : node.InnerText.Trim();
            }
        }
        return null;
    }
}