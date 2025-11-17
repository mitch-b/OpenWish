using OpenWish.Shared.Models;

namespace OpenWish.Shared.Services;

public interface IWishlistService
{
    Task<WishlistModel> CreateWishlistAsync(WishlistModel wishlist, string ownerId);
    Task<WishlistModel> GetWishlistAsync(int id, string? userId = null);
    Task<WishlistModel> GetWishlistByPublicIdAsync(string publicId, string? userId = null);
    Task<IEnumerable<WishlistModel>> GetUserWishlistsAsync(string userId);
    Task<WishlistModel> UpdateWishlistAsync(int id, WishlistModel wishlist);
    Task<WishlistModel> UpdateWishlistByPublicIdAsync(string publicId, WishlistModel wishlist);
    Task DeleteWishlistAsync(int id);
    Task DeleteWishlistByPublicIdAsync(string publicId);

    // Wishlist sharing and permissions
    Task<WishlistPermissionModel> ShareWishlistAsync(int wishlistId, string userId, string permissionType);
    Task<WishlistPermissionModel> ShareWishlistByPublicIdAsync(string wishlistPublicId, string userId, string permissionType);
    Task<string> CreateSharingLinkAsync(int wishlistId, string permissionType, TimeSpan? expiration = null);
    Task<string> CreateSharingLinkByPublicIdAsync(string wishlistPublicId, string permissionType, TimeSpan? expiration = null);
    Task<bool> AcceptSharingLinkAsync(string token, string userId);
    Task<IEnumerable<WishlistPermissionModel>> GetWishlistPermissionsAsync(int wishlistId);
    Task<IEnumerable<WishlistPermissionModel>> GetWishlistPermissionsByPublicIdAsync(string wishlistPublicId);
    Task<bool> RemoveWishlistPermissionAsync(int wishlistId, string userId);
    Task<bool> RemoveWishlistPermissionByPublicIdAsync(string wishlistPublicId, string userId);
    Task<IEnumerable<WishlistModel>> GetSharedWithMeWishlistsAsync(string userId);
    Task<IEnumerable<WishlistModel>> GetFriendsWishlistsAsync(string userId);
    Task<bool> CanUserAccessWishlistAsync(int wishlistId, string userId);
    Task<bool> CanUserAccessWishlistByPublicIdAsync(string wishlistPublicId, string userId);
    Task<bool> CanUserEditWishlistAsync(int wishlistId, string userId);
    Task<bool> CanUserEditWishlistByPublicIdAsync(string wishlistPublicId, string userId);

    // WishlistItems
    Task<WishlistItemModel> GetWishlistItemAsync(int wishlistId, int itemId);
    Task<IEnumerable<WishlistItemModel>> GetWishlistItemsAsync(int wishlistId);
    Task<IEnumerable<WishlistItemModel>> GetWishlistItemsByPublicIdAsync(string wishlistPublicId, string? requestingUserId = null);
    Task<WishlistItemModel> GetWishlistItemByPublicIdAsync(string wishlistPublicId, int itemId, string? requestingUserId = null);
    Task<WishlistItemModel> AddItemToWishlistAsync(int wishlistId, WishlistItemModel item);
    Task<WishlistItemModel> AddItemToWishlistByPublicIdAsync(string wishlistPublicId, WishlistItemModel item);
    Task<bool> RemoveItemFromWishlistAsync(int wishlistId, int itemId);
    Task<bool> RemoveItemFromWishlistByPublicIdAsync(string wishlistPublicId, int itemId);
    Task<WishlistItemModel> UpdateWishlistItemAsync(int wishlistId, int itemId, WishlistItemModel item);
    Task<WishlistItemModel> UpdateWishlistItemByPublicIdAsync(string wishlistPublicId, int itemId, WishlistItemModel item);

    // Item comments
    Task<ItemCommentModel> AddCommentToItemAsync(int wishlistId, int itemId, string userId, string text);
    Task<ItemCommentModel> AddCommentToItemByPublicIdAsync(string wishlistPublicId, int itemId, string userId, string text);
    Task<IEnumerable<ItemCommentModel>> GetItemCommentsAsync(int wishlistId, int itemId);
    Task<IEnumerable<ItemCommentModel>> GetItemCommentsByPublicIdAsync(string wishlistPublicId, int itemId);
    Task<bool> RemoveItemCommentAsync(int commentId, string userId);

    // Item reservations
    Task<bool> ReserveItemAsync(int wishlistId, int itemId, string userId, bool isAnonymous = false);
    Task<bool> ReserveItemByPublicIdAsync(string wishlistPublicId, int itemId, string userId, bool isAnonymous = false);
    Task<bool> CancelReservationAsync(int wishlistId, int itemId, string userId);
    Task<bool> CancelReservationByPublicIdAsync(string wishlistPublicId, int itemId, string userId);
    Task<ItemReservationModel?> GetItemReservationAsync(int wishlistId, int itemId);
    Task<ItemReservationModel?> GetItemReservationByPublicIdAsync(string wishlistPublicId, int itemId);
    Task<bool> IsItemReservedAsync(int itemId);
}