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

    [HttpGet("{publicId}")]
    public async Task<ActionResult<WishlistModel>> GetWishlist(string publicId)
    {
        var userId = await _userContextService.GetUserIdAsync();
        if (userId is null)
        {
            return Unauthorized();
        }

        try
        {
            var wishlist = await _wishlistService.GetWishlistByPublicIdAsync(publicId, userId);
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

    [HttpPut("{publicId}")]
    public async Task<IActionResult> UpdateWishlist(string publicId, WishlistModel wishlist)
    {
        var updatedWishlist = await _wishlistService.UpdateWishlistByPublicIdAsync(publicId, wishlist);
        if (updatedWishlist == null)
        {
            return NotFound();
        }
        return Ok(updatedWishlist);
    }

    [HttpDelete("{publicId}")]
    public async Task<IActionResult> DeleteWishlist(string publicId)
    {
        await _wishlistService.DeleteWishlistByPublicIdAsync(publicId);
        return NoContent();
    }

    [HttpGet("{wishlistPublicId}/items")]
    public async Task<ActionResult<IEnumerable<WishlistItemModel>>> GetWishlistItems(string wishlistPublicId)
    {
        var userId = await _userContextService.GetUserIdAsync();
        if (userId is null)
        {
            return Unauthorized();
        }

        try
        {
            // Check if user can access the wishlist first
            var canAccess = await _wishlistService.CanUserAccessWishlistByPublicIdAsync(wishlistPublicId, userId);
            if (!canAccess)
            {
                return Forbid();
            }

            var items = await _wishlistService.GetWishlistItemsByPublicIdAsync(wishlistPublicId, userId);
            return Ok(items);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpGet("{wishlistPublicId}/items/{itemId}")]
    public async Task<ActionResult<WishlistItemModel>> GetWishlistItem(string wishlistPublicId, int itemId)
    {
        var userId = await _userContextService.GetUserIdAsync();
        if (userId is null)
        {
            return Unauthorized();
        }

        try
        {
            // Check if user can access the wishlist first
            var canAccess = await _wishlistService.CanUserAccessWishlistByPublicIdAsync(wishlistPublicId, userId);
            if (!canAccess)
            {
                return Forbid();
            }

            var item = await _wishlistService.GetWishlistItemByPublicIdAsync(wishlistPublicId, itemId, userId);
            return Ok(item);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPost("{wishlistPublicId}/items")]
    public async Task<ActionResult<WishlistItemModel>> AddItemToWishlist(string wishlistPublicId, WishlistItemModel item)
    {
        var userId = await _userContextService.GetUserIdAsync();
        if (userId is null)
        {
            return Unauthorized();
        }

        try
        {
            // Check if user can edit the wishlist
            var canEdit = await _wishlistService.CanUserEditWishlistByPublicIdAsync(wishlistPublicId, userId);
            if (!canEdit)
            {
                return Forbid();
            }

            var addedItem = await _wishlistService.AddItemToWishlistByPublicIdAsync(wishlistPublicId, item);
            return CreatedAtAction(nameof(GetWishlistItem), new { wishlistPublicId, itemId = addedItem.Id }, addedItem);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPut("{wishlistPublicId}/items/{itemId}")]
    public async Task<IActionResult> UpdateWishlistItem(string wishlistPublicId, int itemId, WishlistItemModel item)
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
            var canEdit = await _wishlistService.CanUserEditWishlistByPublicIdAsync(wishlistPublicId, userId);
            if (!canEdit)
            {
                return Forbid();
            }

            var updatedItem = await _wishlistService.UpdateWishlistItemByPublicIdAsync(wishlistPublicId, itemId, item);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpDelete("{wishlistPublicId}/items/{itemId}")]
    public async Task<IActionResult> RemoveItemFromWishlist(string wishlistPublicId, int itemId)
    {
        var userId = await _userContextService.GetUserIdAsync();
        if (userId is null)
        {
            return Unauthorized();
        }

        try
        {
            // Check if user can edit the wishlist
            var canEdit = await _wishlistService.CanUserEditWishlistByPublicIdAsync(wishlistPublicId, userId);
            if (!canEdit)
            {
                return Forbid();
            }

            var result = await _wishlistService.RemoveItemFromWishlistByPublicIdAsync(wishlistPublicId, itemId);
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
    [HttpPost("{wishlistPublicId}/permissions")]
    public async Task<ActionResult<WishlistPermissionModel>> ShareWishlist(string wishlistPublicId, [FromBody] ShareRequest request)
    {
        var permission = await _wishlistService.ShareWishlistByPublicIdAsync(wishlistPublicId, request.UserId, request.PermissionType);
        return Ok(permission);
    }

    [HttpPost("{wishlistPublicId}/share-link")]
    public async Task<ActionResult<string>> CreateSharingLink(string wishlistPublicId, [FromBody] SharingLinkRequest request)
    {
        var token = await _wishlistService.CreateSharingLinkByPublicIdAsync(wishlistPublicId, request.PermissionType, request.Expiration);
        return Ok(token);
    }

    [HttpPost("accept-link/{token}")]
    public async Task<ActionResult<bool>> AcceptSharingLink(string token, [FromBody] AcceptLinkRequest request)
    {
        var result = await _wishlistService.AcceptSharingLinkAsync(token, request.UserId);
        return Ok(result);
    }

    [HttpGet("{wishlistPublicId}/permissions")]
    public async Task<ActionResult<IEnumerable<WishlistPermissionModel>>> GetWishlistPermissions(string wishlistPublicId)
    {
        var permissions = await _wishlistService.GetWishlistPermissionsByPublicIdAsync(wishlistPublicId);
        return Ok(permissions);
    }

    [HttpDelete("{wishlistPublicId}/permissions/{userId}")]
    public async Task<ActionResult<bool>> RemovePermission(string wishlistPublicId, string userId)
    {
        var result = await _wishlistService.RemoveWishlistPermissionByPublicIdAsync(wishlistPublicId, userId);
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

    [HttpGet("{wishlistPublicId}/can-access/{userId}")]
    public async Task<ActionResult<bool>> CanUserAccess(string wishlistPublicId, string userId)
    {
        var result = await _wishlistService.CanUserAccessWishlistByPublicIdAsync(wishlistPublicId, userId);
        return Ok(result);
    }

    [HttpGet("{wishlistPublicId}/can-edit/{userId}")]
    public async Task<ActionResult<bool>> CanUserEdit(string wishlistPublicId, string userId)
    {
        var result = await _wishlistService.CanUserEditWishlistByPublicIdAsync(wishlistPublicId, userId);
        return Ok(result);
    }

    // Item comments endpoints
    [HttpPost("{wishlistPublicId}/items/{itemId}/comments")]
    public async Task<ActionResult<ItemCommentModel>> AddComment(string wishlistPublicId, int itemId, [FromBody] CommentRequest request)
    {
        var comment = await _wishlistService.AddCommentToItemByPublicIdAsync(wishlistPublicId, itemId, request.UserId, request.Text);
        return Ok(comment);
    }

    [HttpGet("{wishlistPublicId}/items/{itemId}/comments")]
    public async Task<ActionResult<IEnumerable<ItemCommentModel>>> GetComments(string wishlistPublicId, int itemId)
    {
        var comments = await _wishlistService.GetItemCommentsByPublicIdAsync(wishlistPublicId, itemId);
        return Ok(comments);
    }

    [HttpDelete("comments/{commentId}")]
    public async Task<ActionResult<bool>> RemoveComment(int commentId, [FromQuery] string userId)
    {
        var result = await _wishlistService.RemoveItemCommentAsync(commentId, userId);
        return Ok(result);
    }

    // Item reservations endpoints
    [HttpPost("{wishlistPublicId}/items/{itemId}/reserve")]
    public async Task<ActionResult<bool>> ReserveItem(string wishlistPublicId, int itemId, [FromBody] ReservationRequest request)
    {
        var result = await _wishlistService.ReserveItemByPublicIdAsync(wishlistPublicId, itemId, request.UserId, request.IsAnonymous);
        return Ok(result);
    }

    [HttpDelete("{wishlistPublicId}/items/{itemId}/reservation")]
    public async Task<ActionResult<bool>> CancelReservation(string wishlistPublicId, int itemId, [FromQuery] string userId)
    {
        var result = await _wishlistService.CancelReservationByPublicIdAsync(wishlistPublicId, itemId, userId);
        return Ok(result);
    }

    [HttpGet("{wishlistPublicId}/items/{itemId}/reservation")]
    public async Task<ActionResult<ItemReservationModel>> GetReservation(string wishlistPublicId, int itemId)
    {
        var reservation = await _wishlistService.GetItemReservationByPublicIdAsync(wishlistPublicId, itemId);
        return Ok(reservation);
    }

    [HttpGet("items/{itemId}/is-reserved")]
    public async Task<ActionResult<bool>> IsItemReserved(int itemId)
    {
        var isReserved = await _wishlistService.IsItemReservedAsync(itemId);
        return Ok(isReserved);
    }

    [HttpGet("{wishlistPublicId}/friends-with-access")]
    public async Task<ActionResult<IEnumerable<ApplicationUserModel>>> GetFriendsWithAccess(string wishlistPublicId)
    {
        var userId = await _userContextService.GetUserIdAsync();
        if (userId is null)
        {
            return Unauthorized();
        }

        try
        {
            var friends = await _wishlistService.GetFriendsWithAccessByPublicIdAsync(wishlistPublicId);
            return Ok(friends);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
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