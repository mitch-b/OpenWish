using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenWish.Shared.Models;
using OpenWish.Shared.RequestModels;
using OpenWish.Shared.Services;
using OpenWish.Web.Services;

namespace OpenWish.Web.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]s")]
public class ProductController(IProductService productService, ApiUserContextService userContextService) : ControllerBase
{
    private readonly IProductService _productService = productService;
    private readonly ApiUserContextService _userContextService = userContextService;

    // TODO: Rate Limit aggressively by user
    [HttpPost(Name = "scrape")]
    public async Task<ActionResult<WishlistModel>> TryScrape(ProductScrapeRequest productScrapeRequest)
    {
        var userId = await _userContextService.GetUserIdAsync();
        if (userId is null)
        {
            return Unauthorized();
        }
        var product = await _productService.TryScrapeProductFromUrl(productScrapeRequest.ProductUrl);
        if (product is null)
        {
            return NoContent();
        }
        return Ok(product);
    }
}
