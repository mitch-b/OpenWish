using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using OpenWish.Application.Models;
using OpenWish.Shared.Models;
using OpenWish.Shared.Services;

public partial class ProductService : IProductService, IDisposable
{
    [System.Text.RegularExpressions.GeneratedRegex(@"[^0-9.,]+")]
    private static partial System.Text.RegularExpressions.Regex PriceParseRegex();

    private readonly HttpClient _client;
    private readonly ILogger<ProductService> _logger;

    public ProductService(ILogger<ProductService> logger)
    {
        _client = new HttpClient();
        _logger = logger;
    }

    public async Task<ProductModel?> TryScrapeProductFromUrl(string url)
    {
        try
        {
            var response = await _client.GetAsync(url);
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
            _logger.LogError(ex, $"Error fetching URL: {ex.Message}");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error parsing HTML: {ex.Message}");
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

    public void Dispose() => _client.Dispose();
}
