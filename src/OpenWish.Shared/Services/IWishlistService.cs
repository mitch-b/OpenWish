using OpenWish.Shared.Models;

namespace OpenWish.Shared.Services;

public interface IWishlistService
{
    Task<WishlistModel> CreateWishlistAsync(WishlistModel wishlist, string ownerId);
    Task<WishlistModel> GetWishlistAsync(int id, string? userId = null);
    Task<IEnumerable<WishlistModel>> GetUserWishlistsAsync(string userId);
    Task<WishlistModel> UpdateWishlistAsync(int id, WishlistModel wishlist);
    Task DeleteWishlistAsync(int id);

    // Wishlist sharing and permissions
    Task<WishlistPermissionModel> ShareWishlistAsync(int wishlistId, string userId, string permissionType);
    Task<string> CreateSharingLinkAsync(int wishlistId, string permissionType, TimeSpan? expiration = null);
    Task<bool> AcceptSharingLinkAsync(string token, string userId);
    Task<IEnumerable<WishlistPermissionModel>> GetWishlistPermissionsAsync(int wishlistId);
    Task<bool> RemoveWishlistPermissionAsync(int wishlistId, string userId);
    Task<IEnumerable<WishlistModel>> GetSharedWithMeWishlistsAsync(string userId);
    Task<IEnumerable<WishlistModel>> GetFriendsWishlistsAsync(string userId);
    Task<bool> CanUserAccessWishlistAsync(int wishlistId, string userId);
    Task<bool> CanUserEditWishlistAsync(int wishlistId, string userId);

    // WishlistItems
    Task<WishlistItemModel> GetWishlistItemAsync(int wishlistId, int itemId);
    Task<IEnumerable<WishlistItemModel>> GetWishlistItemsAsync(int wishlistId);
    Task<WishlistItemModel> AddItemToWishlistAsync(int wishlistId, WishlistItemModel item);
    Task<bool> RemoveItemFromWishlistAsync(int wishlistId, int itemId);
    Task<WishlistItemModel> UpdateWishlistItemAsync(int wishlistId, int itemId, WishlistItemModel item);

    // Item comments
    Task<ItemCommentModel> AddCommentToItemAsync(int wishlistId, int itemId, string userId, string text);
    Task<IEnumerable<ItemCommentModel>> GetItemCommentsAsync(int wishlistId, int itemId);
    Task<bool> RemoveItemCommentAsync(int commentId, string userId);

    // Item reservations
    Task<bool> ReserveItemAsync(int wishlistId, int itemId, string userId, bool isAnonymous = false);
    Task<bool> CancelReservationAsync(int wishlistId, int itemId, string userId);
    Task<ItemReservationModel?> GetItemReservationAsync(int wishlistId, int itemId);
    Task<bool> IsItemReservedAsync(int itemId);
}