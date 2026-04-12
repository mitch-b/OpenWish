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
    /// loopback addresses, link-local ranges, or private (RFC-1918/RFC-4193) networks.
    /// </summary>
    private static async Task<bool> IsSafeUrlAsync(Uri uri)
    {
        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            return false;

        IPAddress[] addresses;
        try
        {
            addresses = await Dns.GetHostAddressesAsync(uri.DnsSafeHost);
        }
        catch
        {
            return false;
        }

        foreach (var address in addresses)
        {
            if (IPAddress.IsLoopback(address))
                return false;

            var bytes = address.GetAddressBytes();

            if (address.AddressFamily == AddressFamily.InterNetwork)
            {
                // 10.0.0.0/8
                if (bytes[0] == 10) return false;
                // 172.16.0.0/12
                if (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31) return false;
                // 192.168.0.0/16
                if (bytes[0] == 192 && bytes[1] == 168) return false;
                // 169.254.0.0/16  (link-local / cloud metadata)
                if (bytes[0] == 169 && bytes[1] == 254) return false;
            }
            else if (address.AddressFamily == AddressFamily.InterNetworkV6)
            {
                // ::1 is covered by IsLoopback(); also block fc00::/7 (ULA) and fe80::/10 (link-local)
                if (bytes[0] == 0xfc || bytes[0] == 0xfd) return false;
                if (bytes[0] == 0xfe && (bytes[1] & 0xc0) == 0x80) return false;
            }
        }

        return true;
    }

    public async Task<ProductModel?> TryScrapeProductFromUrl(string url)
    {
        try
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || !await IsSafeUrlAsync(uri))
            {
                _logger.LogWarning("Rejected unsafe or invalid URL for product scrape: {Url}", url);
                return null;
            }

            var response = await _client.GetAsync(uri);
            response.EnsureSuccessStatusCode();

            var html = await response.Content.ReadAsStringAsync();
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            string? name = TrySelectors(doc, ProductSelectors.TitleSelectors);
            string? description = TrySelectors(doc, ProductSelectors.DescriptionSelectors);
            string? price = TrySelectors(doc, ProductSelectors.PriceSelectors);
            string? imageUrl = TrySelectors(doc, ProductSelectors.ImageSelectors);

            if (!string.IsNullOrEmpty(imageUrl) && !imageUrl.StartsWith("http"))
            {
                imageUrl = new Uri(new Uri(url), imageUrl).AbsoluteUri;
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
            _logger.LogError(ex, "Error fetching URL: {Message}", ex.Message);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing HTML: {Message}", ex.Message);
            return null;
        }
    }

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