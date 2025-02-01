using OpenWish.Shared.Models;

namespace OpenWish.Shared.Services;

public interface IProductService
{
    Task<ProductModel?> TryScrapeProductFromUrl(string url);
}
