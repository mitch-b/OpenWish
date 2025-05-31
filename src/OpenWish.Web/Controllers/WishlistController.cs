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
        var userId = await _userContextService.GetUserIdAsync();
        if (userId is null)
        {
            return Unauthorized();
        }

        try
        {
            var wishlist = await _wishlistService.GetWishlistAsync(id, userId);
            return Ok(wishlist);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
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
        return Ok(updatedWishlist);
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
        var userId = await _userContextService.GetUserIdAsync();
        if (userId is null)
        {
            return Unauthorized();
        }

        try
        {
            // Check if user can access the wishlist first
            var canAccess = await _wishlistService.CanUserAccessWishlistAsync(wishlistId, userId);
            if (!canAccess)
            {
                return Forbid();
            }

            var items = await _wishlistService.GetWishlistItemsAsync(wishlistId);
            return Ok(items);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpGet("{wishlistId}/items/{itemId}")]
    public async Task<ActionResult<WishlistItemModel>> GetWishlistItem(int wishlistId, int itemId)
    {
        var userId = await _userContextService.GetUserIdAsync();
        if (userId is null)
        {
            return Unauthorized();
        }

        try
        {
            // Check if user can access the wishlist first
            var canAccess = await _wishlistService.CanUserAccessWishlistAsync(wishlistId, userId);
            if (!canAccess)
            {
                return Forbid();
            }

            var item = await _wishlistService.GetWishlistItemAsync(wishlistId, itemId);
            return Ok(item);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPost("{wishlistId}/items")]
    public async Task<ActionResult<WishlistItemModel>> AddItemToWishlist(int wishlistId, WishlistItemModel item)
    {
        var userId = await _userContextService.GetUserIdAsync();
        if (userId is null)
        {
            return Unauthorized();
        }

        try
        {
            // Check if user can edit the wishlist
            var canEdit = await _wishlistService.CanUserEditWishlistAsync(wishlistId, userId);
            if (!canEdit)
            {
                return Forbid();
            }

            var addedItem = await _wishlistService.AddItemToWishlistAsync(wishlistId, item);
            return CreatedAtAction(nameof(GetWishlistItem), new { wishlistId, itemId = addedItem.WishlistId }, addedItem);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPut("{wishlistId}/items/{itemId}")]
    public async Task<IActionResult> UpdateWishlistItem(int wishlistId, int itemId, WishlistItemModel item)
    {
        var userId = await _userContextService.GetUserIdAsync();
        if (userId is null)
        {
            return Unauthorized();
        }

        if (itemId != item.Id)
        {
            return BadRequest();
        }

        try
        {
            // Check if user can edit the wishlist
            var canEdit = await _wishlistService.CanUserEditWishlistAsync(wishlistId, userId);
            if (!canEdit)
            {
                return Forbid();
            }

            var updatedItem = await _wishlistService.UpdateWishlistItemAsync(wishlistId, itemId, item);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpDelete("{wishlistId}/items/{itemId}")]
    public async Task<IActionResult> RemoveItemFromWishlist(int wishlistId, int itemId)
    {
        var userId = await _userContextService.GetUserIdAsync();
        if (userId is null)
        {
            return Unauthorized();
        }

        try
        {
            // Check if user can edit the wishlist
            var canEdit = await _wishlistService.CanUserEditWishlistAsync(wishlistId, userId);
            if (!canEdit)
            {
                return Forbid();
            }

            var result = await _wishlistService.RemoveItemFromWishlistAsync(wishlistId, itemId);
            if (!result)
            {
                return NotFound();
            }
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    // Wishlist sharing endpoints
    [HttpPost("{wishlistId}/permissions")]
    public async Task<ActionResult<WishlistPermissionModel>> ShareWishlist(int wishlistId, [FromBody] ShareRequest request)
    {
        var permission = await _wishlistService.ShareWishlistAsync(wishlistId, request.UserId, request.PermissionType);
        return Ok(permission);
    }

    [HttpPost("{wishlistId}/share-link")]
    public async Task<ActionResult<string>> CreateSharingLink(int wishlistId, [FromBody] SharingLinkRequest request)
    {
        var token = await _wishlistService.CreateSharingLinkAsync(wishlistId, request.PermissionType, request.Expiration);
        return Ok(token);
    }

    [HttpPost("accept-link/{token}")]
    public async Task<ActionResult<bool>> AcceptSharingLink(string token, [FromBody] AcceptLinkRequest request)
    {
        var result = await _wishlistService.AcceptSharingLinkAsync(token, request.UserId);
        return Ok(result);
    }

    [HttpGet("{wishlistId}/permissions")]
    public async Task<ActionResult<IEnumerable<WishlistPermissionModel>>> GetWishlistPermissions(int wishlistId)
    {
        var permissions = await _wishlistService.GetWishlistPermissionsAsync(wishlistId);
        return Ok(permissions);
    }

    [HttpDelete("{wishlistId}/permissions/{userId}")]
    public async Task<ActionResult<bool>> RemovePermission(int wishlistId, string userId)
    {
        var result = await _wishlistService.RemoveWishlistPermissionAsync(wishlistId, userId);
        return Ok(result);
    }

    [HttpGet("shared-with-me")]
    public async Task<ActionResult<IEnumerable<WishlistModel>>> GetSharedWithMe()
    {
        var userId = await _userContextService.GetUserIdAsync();
        if (userId is null)
        {
            return Unauthorized();
        }

        var wishlists = await _wishlistService.GetSharedWithMeWishlistsAsync(userId);
        return Ok(wishlists);
    }

    [HttpGet("friends")]
    public async Task<ActionResult<IEnumerable<WishlistModel>>> GetFriendsWishlists()
    {
        var userId = await _userContextService.GetUserIdAsync();
        if (userId is null)
        {
            return Unauthorized();
        }

        var wishlists = await _wishlistService.GetFriendsWishlistsAsync(userId);
        return Ok(wishlists);
    }

    [HttpGet("{wishlistId}/can-access/{userId}")]
    public async Task<ActionResult<bool>> CanUserAccess(int wishlistId, string userId)
    {
        var result = await _wishlistService.CanUserAccessWishlistAsync(wishlistId, userId);
        return Ok(result);
    }

    [HttpGet("{wishlistId}/can-edit/{userId}")]
    public async Task<ActionResult<bool>> CanUserEdit(int wishlistId, string userId)
    {
        var result = await _wishlistService.CanUserEditWishlistAsync(wishlistId, userId);
        return Ok(result);
    }

    // Item comments endpoints
    [HttpPost("{wishlistId}/items/{itemId}/comments")]
    public async Task<ActionResult<ItemCommentModel>> AddComment(int wishlistId, int itemId, [FromBody] CommentRequest request)
    {
        var comment = await _wishlistService.AddCommentToItemAsync(wishlistId, itemId, request.UserId, request.Text);
        return Ok(comment);
    }

    [HttpGet("{wishlistId}/items/{itemId}/comments")]
    public async Task<ActionResult<IEnumerable<ItemCommentModel>>> GetComments(int wishlistId, int itemId)
    {
        var comments = await _wishlistService.GetItemCommentsAsync(wishlistId, itemId);
        return Ok(comments);
    }

    [HttpDelete("comments/{commentId}")]
    public async Task<ActionResult<bool>> RemoveComment(int commentId, [FromQuery] string userId)
    {
        var result = await _wishlistService.RemoveItemCommentAsync(commentId, userId);
        return Ok(result);
    }

    // Item reservations endpoints
    [HttpPost("{wishlistId}/items/{itemId}/reserve")]
    public async Task<ActionResult<bool>> ReserveItem(int wishlistId, int itemId, [FromBody] ReservationRequest request)
    {
        var result = await _wishlistService.ReserveItemAsync(wishlistId, itemId, request.UserId, request.IsAnonymous);
        return Ok(result);
    }

    [HttpDelete("{wishlistId}/items/{itemId}/reservation")]
    public async Task<ActionResult<bool>> CancelReservation(int wishlistId, int itemId, [FromQuery] string userId)
    {
        var result = await _wishlistService.CancelReservationAsync(wishlistId, itemId, userId);
        return Ok(result);
    }

    [HttpGet("{wishlistId}/items/{itemId}/reservation")]
    public async Task<ActionResult<ItemReservationModel>> GetReservation(int wishlistId, int itemId)
    {
        var reservation = await _wishlistService.GetItemReservationAsync(wishlistId, itemId);
        return Ok(reservation);
    }

    [HttpGet("items/{itemId}/is-reserved")]
    public async Task<ActionResult<bool>> IsItemReserved(int itemId)
    {
        var isReserved = await _wishlistService.IsItemReservedAsync(itemId);
        return Ok(isReserved);
    }

    public class ShareRequest
    {
        public string UserId { get; set; }
        public string PermissionType { get; set; }
    }

    public class SharingLinkRequest
    {
        public string PermissionType { get; set; }
        public TimeSpan? Expiration { get; set; }
    }

    public class AcceptLinkRequest
    {
        public string UserId { get; set; }
    }

    public class CommentRequest
    {
        public string UserId { get; set; }
        public string Text { get; set; }
    }

    public class ReservationRequest
    {
        public string UserId { get; set; }
        public bool IsAnonymous { get; set; }
    }
}