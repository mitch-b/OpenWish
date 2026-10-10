using System.Net;
using System.Net.Http.Json;
using OpenWish.Shared.Models;
using OpenWish.Shared.RequestModels;
using OpenWish.Shared.Services;

namespace OpenWish.Web.Client.Services;

public class ProductHttpClientService(HttpClient httpClient) : IProductService
{
    public async Task<ProductModel?> TryScrapeProductFromUrl(string url, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            "api/products/scrape",
            new ProductScrapeRequest { ProductUrl = url },
            cancellationToken);
        response.EnsureSuccessStatusCode();
        if (response.StatusCode == HttpStatusCode.OK)
        {
            return await response.Content.ReadFromJsonAsync<ProductModel>(cancellationToken);
        }
        return null;
    }
}