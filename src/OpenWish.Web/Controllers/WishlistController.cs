using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenWish.Shared.Models;
using OpenWish.Shared.Services;
using OpenWish.Web.Services;

namespace OpenWish.Web.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]s")]
public class WishlistController(IWishlistService wishlistService, ApiUserContextService userContextService) : ControllerBase
{
    private readonly IWishlistService _wishlistService = wishlistService;
    private readonly ApiUserContextService _userContextService = userContextService;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<WishlistModel>>> GetWishlists()
    {
        var userId = await _userContextService.GetUserIdAsync();
        if (userId is null)
        {
            return Unauthorized();
        }
        var wishlists = await _wishlistService.GetUserWishlistsAsync(userId);
        return Ok(wishlists);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<WishlistModel>> GetWishlist(int id)
    {
        var wishlist = await _wishlistService.GetWishlistAsync(id);
        if (wishlist == null)
        {
            return NotFound();
        }
        return Ok(wishlist);
    }

    [HttpPost]
    public async Task<ActionResult<WishlistModel>> CreateWishlist(WishlistModel wishlist)
    {
        var userId = await _userContextService.GetUserIdAsync();
        if (userId is null)
        {
            return Unauthorized();
        }
        var createdWishlist = await _wishlistService.CreateWishlistAsync(wishlist, userId);
        return CreatedAtAction(nameof(GetWishlist), new { id = createdWishlist.Id }, createdWishlist);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateWishlist(int id, WishlistModel wishlist)
    {
        if (id != wishlist.Id)
        {
            return BadRequest();
        }
        var updatedWishlist = await _wishlistService.UpdateWishlistAsync(id, wishlist);
        if (updatedWishlist == null)
        {
            return NotFound();
        }
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteWishlist(int id)
    {
        await _wishlistService.DeleteWishlistAsync(id);
        return NoContent();
    }

    [HttpGet("{wishlistId}/items")]
    public async Task<ActionResult<IEnumerable<WishlistItemModel>>> GetWishlistItems(int wishlistId)
    {
        var items = await _wishlistService.GetWishlistItemsAsync(wishlistId);
        return Ok(items);
    }

    [HttpGet("{wishlistId}/items/{itemId}")]
    public async Task<ActionResult<WishlistItemModel>> GetWishlistItem(int wishlistId, int itemId)
    {
        var item = await _wishlistService.GetWishlistItemAsync(wishlistId, itemId);
        if (item == null)
        {
            return NotFound();
        }
        return Ok(item);
    }

    [HttpPost("{wishlistId}/items")]
    public async Task<ActionResult<WishlistItemModel>> AddItemToWishlist(int wishlistId, WishlistItemModel item)
    {
        var addedItem = await _wishlistService.AddItemToWishlistAsync(wishlistId, item);
        return CreatedAtAction(nameof(GetWishlistItem), new { wishlistId, itemId = addedItem.WishlistId }, addedItem);
    }

    [HttpPut("{wishlistId}/items/{itemId}")]
    public async Task<IActionResult> UpdateWishlistItem(int wishlistId, int itemId, WishlistItemModel item)
    {
        if (itemId != item.WishlistId)
        {
            return BadRequest();
        }
        var updatedItem = await _wishlistService.UpdateWishlistItemAsync(wishlistId, itemId, item);
        if (updatedItem == null)
        {
            return NotFound();
        }
        return NoContent();
    }

    [HttpDelete("{wishlistId}/items/{itemId}")]
    public async Task<IActionResult> RemoveItemFromWishlist(int wishlistId, int itemId)
    {
        var result = await _wishlistService.RemoveItemFromWishlistAsync(wishlistId, itemId);
        if (!result)
        {
            return NotFound();
        }
        return NoContent();
    }
}