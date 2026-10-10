using OpenWish.Shared.Models;

namespace OpenWish.Shared.Services;

/// <summary>
/// Service for managing wishlists, items, comments, and reservations.
/// Provides both internal ID and public ID-based access patterns for security and discoverability.
/// </summary>
public interface IWishlistService
{
    /// <summary>Creates a new wishlist owned by the specified user.</summary>
    /// <param name="wishlist">The wishlist model containing name, description, and other details.</param>
    /// <param name="ownerId">The user ID of the wishlist owner.</param>
    /// <returns>The created wishlist model with generated public ID.</returns>
    Task<WishlistModel> CreateWishlistAsync(WishlistModel wishlist, string ownerId);

    /// <summary>Retrieves a wishlist by its internal ID, with optional access by a specific user.</summary>
    /// <param name="id">The internal wishlist ID.</param>
    /// <param name="userId">Optional user ID for access-level filtering.</param>
    /// <returns>The wishlist model if accessible.</returns>
    Task<WishlistModel> GetWishlistAsync(int id, string? userId = null);

    /// <summary>Retrieves a wishlist by its public ID, suitable for discovery and sharing links.</summary>
    /// <param name="publicId">The public wishlist identifier.</param>
    /// <param name="userId">Optional user ID for access-level filtering.</param>
    /// <returns>The wishlist model if accessible.</returns>
    Task<WishlistModel> GetWishlistByPublicIdAsync(string publicId, string? userId = null);

    /// <summary>Lists all wishlists owned by the specified user.</summary>
    /// <param name="userId">The owner's user ID.</param>
    /// <returns>Collection of wishlist models owned by the user.</returns>
    Task<IEnumerable<WishlistModel>> GetUserWishlistsAsync(string userId);

    /// <summary>Updates a wishlist by its internal ID.</summary>
    /// <param name="id">The internal wishlist ID.</param>
    /// <param name="wishlist">The updated wishlist model.</param>
    /// <returns>The updated wishlist model.</returns>
    Task<WishlistModel> UpdateWishlistAsync(int id, WishlistModel wishlist);

    /// <summary>Updates a wishlist by its public ID, verifying the requestor has edit permission.</summary>
    /// <param name="publicId">The public wishlist identifier.</param>
    /// <param name="wishlist">The updated wishlist model.</param>
    /// <param name="requestorId">The user ID of the requestor for permission verification.</param>
    /// <returns>The updated wishlist model.</returns>
    Task<WishlistModel> UpdateWishlistByPublicIdAsync(string publicId, WishlistModel wishlist, string requestorId);

    /// <summary>Soft-deletes a wishlist by its internal ID.</summary>
    /// <param name="id">The internal wishlist ID.</param>
    Task DeleteWishlistAsync(int id);

    /// <summary>Soft-deletes a wishlist by its public ID.</summary>
    /// <param name="publicId">The public wishlist identifier.</param>
    Task DeleteWishlistByPublicIdAsync(string publicId);

    /// <summary>Grants a user permission to access a wishlist.</summary>
    /// <param name="wishlistId">The internal wishlist ID.</param>
    /// <param name="userId">The user ID to grant permission to.</param>
    /// <param name="permissionType">The permission level (e.g., "View", "Edit").</param>
    /// <returns>The permission model.</returns>
    Task<WishlistPermissionModel> ShareWishlistAsync(int wishlistId, string userId, string permissionType);

    /// <summary>Grants a user permission to access a wishlist by its public ID.</summary>
    /// <param name="wishlistPublicId">The public wishlist identifier.</param>
    /// <param name="userId">The user ID to grant permission to.</param>
    /// <param name="permissionType">The permission level (e.g., "View", "Edit").</param>
    /// <returns>The permission model.</returns>
    Task<WishlistPermissionModel> ShareWishlistByPublicIdAsync(string wishlistPublicId, string userId, string permissionType);

    /// <summary>Creates a time-limited sharing link for a wishlist.</summary>
    /// <param name="wishlistId">The internal wishlist ID.</param>
    /// <param name="permissionType">The permission level for the link.</param>
    /// <param name="expiration">Optional link expiration duration.</param>
    /// <returns>The sharing token.</returns>
    Task<string> CreateSharingLinkAsync(int wishlistId, string permissionType, TimeSpan? expiration = null);

    /// <summary>Creates a time-limited sharing link for a wishlist by its public ID.</summary>
    /// <param name="wishlistPublicId">The public wishlist identifier.</param>
    /// <param name="permissionType">The permission level for the link.</param>
    /// <param name="expiration">Optional link expiration duration.</param>
    /// <returns>The sharing token.</returns>
    Task<string> CreateSharingLinkByPublicIdAsync(string wishlistPublicId, string permissionType, TimeSpan? expiration = null);

    /// <summary>Accepts and registers a sharing link for the current user.</summary>
    /// <param name="token">The sharing link token.</param>
    /// <param name="userId">The user ID accepting the link.</param>
    /// <returns>True if the link was successfully accepted.</returns>
    Task<bool> AcceptSharingLinkAsync(string token, string userId);

    /// <summary>Lists all users with permissions on a wishlist by its internal ID.</summary>
    /// <param name="wishlistId">The internal wishlist ID.</param>
    /// <returns>Collection of permission models.</returns>
    Task<IEnumerable<WishlistPermissionModel>> GetWishlistPermissionsAsync(int wishlistId);

    /// <summary>Lists all users with permissions on a wishlist by its public ID.</summary>
    /// <param name="wishlistPublicId">The public wishlist identifier.</param>
    /// <returns>Collection of permission models.</returns>
    Task<IEnumerable<WishlistPermissionModel>> GetWishlistPermissionsByPublicIdAsync(string wishlistPublicId);

    /// <summary>Removes a user's permission to access a wishlist.</summary>
    /// <param name="wishlistId">The internal wishlist ID.</param>
    /// <param name="userId">The user ID to remove permission from.</param>
    /// <returns>True if permission was successfully removed.</returns>
    Task<bool> RemoveWishlistPermissionAsync(int wishlistId, string userId);

    /// <summary>Removes a user's permission to access a wishlist by its public ID.</summary>
    /// <param name="wishlistPublicId">The public wishlist identifier.</param>
    /// <param name="userId">The user ID to remove permission from.</param>
    /// <returns>True if permission was successfully removed.</returns>
    Task<bool> RemoveWishlistPermissionByPublicIdAsync(string wishlistPublicId, string userId);

    /// <summary>Lists all wishlists shared with the specified user.</summary>
    /// <param name="userId">The user ID.</param>
    /// <returns>Collection of shared wishlist models.</returns>
    Task<IEnumerable<WishlistModel>> GetSharedWithMeWishlistsAsync(string userId);

    /// <summary>
    /// Lists every wishlist a user can add items to: their own, wishlists shared with edit access,
    /// and collaborative event wishlists for events they belong to.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <returns>Collection of wishlists the user can edit.</returns>
    Task<IEnumerable<WishlistModel>> GetEditableWishlistsAsync(string userId);

    /// <summary>Lists all wishlists owned by the user's friends.</summary>
    /// <param name="userId">The user ID.</param>
    /// <returns>Collection of friend wishlists.</returns>
    Task<IEnumerable<WishlistModel>> GetFriendsWishlistsAsync(string userId);

    /// <summary>Checks if a user has read access to a wishlist.</summary>
    /// <param name="wishlistId">The internal wishlist ID.</param>
    /// <param name="userId">The user ID to check access for.</param>
    /// <returns>True if the user can access the wishlist.</returns>
    Task<bool> CanUserAccessWishlistAsync(int wishlistId, string userId);

    /// <summary>Checks if a user has read access to a wishlist by its public ID.</summary>
    /// <param name="wishlistPublicId">The public wishlist identifier.</param>
    /// <param name="userId">The user ID to check access for.</param>
    /// <returns>True if the user can access the wishlist.</returns>
    Task<bool> CanUserAccessWishlistByPublicIdAsync(string wishlistPublicId, string userId);

    /// <summary>Checks if a user has edit access to a wishlist.</summary>
    /// <param name="wishlistId">The internal wishlist ID.</param>
    /// <param name="userId">The user ID to check edit access for.</param>
    /// <returns>True if the user can edit the wishlist.</returns>
    Task<bool> CanUserEditWishlistAsync(int wishlistId, string userId);

    /// <summary>Checks if a user has edit access to a wishlist by its public ID.</summary>
    /// <param name="wishlistPublicId">The public wishlist identifier.</param>
    /// <param name="userId">The user ID to check edit access for.</param>
    /// <returns>True if the user can edit the wishlist.</returns>
    Task<bool> CanUserEditWishlistByPublicIdAsync(string wishlistPublicId, string userId);

    /// <summary>Lists all friends who have access to a wishlist.</summary>
    /// <param name="wishlistId">The internal wishlist ID.</param>
    /// <returns>Collection of user models for friends with access.</returns>
    Task<IEnumerable<ApplicationUserModel>> GetFriendsWithAccessAsync(int wishlistId);

    /// <summary>Lists all friends who have access to a wishlist by its public ID.</summary>
    /// <param name="wishlistPublicId">The public wishlist identifier.</param>
    /// <returns>Collection of user models for friends with access.</returns>
    Task<IEnumerable<ApplicationUserModel>> GetFriendsWithAccessByPublicIdAsync(string wishlistPublicId);

    /// <summary>Retrieves a specific item from a wishlist by internal IDs.</summary>
    /// <param name="wishlistId">The internal wishlist ID.</param>
    /// <param name="itemId">The internal item ID.</param>
    /// <returns>The wishlist item model.</returns>
    Task<WishlistItemModel> GetWishlistItemAsync(int wishlistId, int itemId);

    /// <summary>Lists all items in a wishlist by internal ID.</summary>
    /// <param name="wishlistId">The internal wishlist ID.</param>
    /// <returns>Collection of wishlist item models.</returns>
    Task<IEnumerable<WishlistItemModel>> GetWishlistItemsAsync(int wishlistId);

    /// <summary>Lists all items in a wishlist by its public ID, filtered by requestor access.</summary>
    /// <param name="wishlistPublicId">The public wishlist identifier.</param>
    /// <param name="requestingUserId">Optional user ID for access-level filtering.</param>
    /// <returns>Collection of wishlist item models.</returns>
    Task<IEnumerable<WishlistItemModel>> GetWishlistItemsByPublicIdAsync(string wishlistPublicId, string? requestingUserId = null);

    /// <summary>Retrieves a specific item from a wishlist by public IDs.</summary>
    /// <param name="wishlistPublicId">The public wishlist identifier.</param>
    /// <param name="itemId">The internal item ID.</param>
    /// <param name="requestingUserId">Optional user ID for access-level filtering.</param>
    /// <returns>The wishlist item model.</returns>
    Task<WishlistItemModel> GetWishlistItemByPublicIdAsync(string wishlistPublicId, int itemId, string? requestingUserId = null);

    /// <summary>Adds a new item to a wishlist by internal ID.</summary>
    /// <param name="wishlistId">The internal wishlist ID.</param>
    /// <param name="item">The item model to add.</param>
    /// <returns>The created item model with generated ID.</returns>
    Task<WishlistItemModel> AddItemToWishlistAsync(int wishlistId, WishlistItemModel item);

    /// <summary>Adds a new item to a wishlist by its public ID.</summary>
    /// <param name="wishlistPublicId">The public wishlist identifier.</param>
    /// <param name="item">The item model to add.</param>
    /// <returns>The created item model with generated ID.</returns>
    Task<WishlistItemModel> AddItemToWishlistByPublicIdAsync(string wishlistPublicId, WishlistItemModel item);

    /// <summary>Removes an item from a wishlist by internal IDs.</summary>
    /// <param name="wishlistId">The internal wishlist ID.</param>
    /// <param name="itemId">The internal item ID to remove.</param>
    /// <returns>True if the item was successfully removed.</returns>
    Task<bool> RemoveItemFromWishlistAsync(int wishlistId, int itemId);

    /// <summary>Removes an item from a wishlist by its public ID.</summary>
    /// <param name="wishlistPublicId">The public wishlist identifier.</param>
    /// <param name="itemId">The internal item ID to remove.</param>
    /// <returns>True if the item was successfully removed.</returns>
    Task<bool> RemoveItemFromWishlistByPublicIdAsync(string wishlistPublicId, int itemId);

    /// <summary>Updates an item in a wishlist by internal IDs.</summary>
    /// <param name="wishlistId">The internal wishlist ID.</param>
    /// <param name="itemId">The internal item ID to update.</param>
    /// <param name="item">The updated item model.</param>
    /// <returns>The updated item model.</returns>
    Task<WishlistItemModel> UpdateWishlistItemAsync(int wishlistId, int itemId, WishlistItemModel item);

    /// <summary>Updates an item in a wishlist by its public ID.</summary>
    /// <param name="wishlistPublicId">The public wishlist identifier.</param>
    /// <param name="itemId">The internal item ID to update.</param>
    /// <param name="item">The updated item model.</param>
    /// <returns>The updated item model.</returns>
    Task<WishlistItemModel> UpdateWishlistItemByPublicIdAsync(string wishlistPublicId, int itemId, WishlistItemModel item);

    /// <summary>Adds a comment to a wishlist item by internal IDs.</summary>
    /// <param name="wishlistId">The internal wishlist ID.</param>
    /// <param name="itemId">The internal item ID.</param>
    /// <param name="userId">The user ID of the commenter.</param>
    /// <param name="text">The comment text.</param>
    /// <returns>The created comment model.</returns>
    Task<ItemCommentModel> AddCommentToItemAsync(int wishlistId, int itemId, string userId, string text);

    /// <summary>Adds a comment to a wishlist item by public IDs.</summary>
    /// <param name="wishlistPublicId">The public wishlist identifier.</param>
    /// <param name="itemId">The internal item ID.</param>
    /// <param name="userId">The user ID of the commenter.</param>
    /// <param name="text">The comment text.</param>
    /// <returns>The created comment model.</returns>
    Task<ItemCommentModel> AddCommentToItemByPublicIdAsync(string wishlistPublicId, int itemId, string userId, string text);

    /// <summary>Lists all comments on a wishlist item by internal IDs.</summary>
    /// <param name="wishlistId">The internal wishlist ID.</param>
    /// <param name="itemId">The internal item ID.</param>
    /// <returns>Collection of comment models.</returns>
    Task<IEnumerable<ItemCommentModel>> GetItemCommentsAsync(int wishlistId, int itemId);

    /// <summary>Lists all comments on a wishlist item by public IDs.</summary>
    /// <param name="wishlistPublicId">The public wishlist identifier.</param>
    /// <param name="itemId">The internal item ID.</param>
    /// <returns>Collection of comment models.</returns>
    Task<IEnumerable<ItemCommentModel>> GetItemCommentsByPublicIdAsync(string wishlistPublicId, int itemId);

    /// <summary>Removes a comment from an item, verifying the user is the comment author.</summary>
    /// <param name="commentId">The internal comment ID.</param>
    /// <param name="userId">The user ID requesting deletion (must be comment author).</param>
    /// <returns>True if the comment was successfully removed.</returns>
    Task<bool> RemoveItemCommentAsync(int commentId, string userId);

    /// <summary>Reserves an item for a user by internal IDs.</summary>
    /// <param name="wishlistId">The internal wishlist ID.</param>
    /// <param name="itemId">The internal item ID.</param>
    /// <param name="userId">The user ID making the reservation.</param>
    /// <param name="isAnonymous">If true, the reservation is hidden from the wishlist owner.</param>
    /// <returns>True if the reservation was successfully created.</returns>
    Task<bool> ReserveItemAsync(int wishlistId, int itemId, string userId, bool isAnonymous = false);

    /// <summary>Reserves an item for a user by public wishlist ID.</summary>
    /// <param name="wishlistPublicId">The public wishlist identifier.</param>
    /// <param name="itemId">The internal item ID.</param>
    /// <param name="userId">The user ID making the reservation.</param>
    /// <param name="isAnonymous">If true, the reservation is hidden from the wishlist owner.</param>
    /// <returns>True if the reservation was successfully created.</returns>
    Task<bool> ReserveItemByPublicIdAsync(string wishlistPublicId, int itemId, string userId, bool isAnonymous = false);

    /// <summary>Cancels a user's reservation on an item by internal IDs.</summary>
    /// <param name="wishlistId">The internal wishlist ID.</param>
    /// <param name="itemId">The internal item ID.</param>
    /// <param name="userId">The user ID canceling the reservation.</param>
    /// <returns>True if the reservation was successfully canceled.</returns>
    Task<bool> CancelReservationAsync(int wishlistId, int itemId, string userId);

    /// <summary>Cancels a user's reservation on an item by public wishlist ID.</summary>
    /// <param name="wishlistPublicId">The public wishlist identifier.</param>
    /// <param name="itemId">The internal item ID.</param>
    /// <param name="userId">The user ID canceling the reservation.</param>
    /// <returns>True if the reservation was successfully canceled.</returns>
    Task<bool> CancelReservationByPublicIdAsync(string wishlistPublicId, int itemId, string userId);

    /// <summary>Retrieves the current reservation on an item, if any, filtered by requestor access.</summary>
    /// <param name="wishlistId">The internal wishlist ID.</param>
    /// <param name="itemId">The internal item ID.</param>
    /// <param name="requestingUserId">The user ID requesting the reservation info.</param>
    /// <returns>The reservation model, or null if item is not reserved or requestor cannot view it.</returns>
    Task<ItemReservationModel?> GetItemReservationAsync(int wishlistId, int itemId, string requestingUserId);

    /// <summary>Retrieves the current reservation on an item by public wishlist ID, filtered by requestor access.</summary>
    /// <param name="wishlistPublicId">The public wishlist identifier.</param>
    /// <param name="itemId">The internal item ID.</param>
    /// <param name="requestingUserId">The user ID requesting the reservation info.</param>
    /// <returns>The reservation model, or null if item is not reserved or requestor cannot view it.</returns>
    Task<ItemReservationModel?> GetItemReservationByPublicIdAsync(string wishlistPublicId, int itemId, string requestingUserId);

    /// <summary>Checks if an item is reserved by any user.</summary>
    /// <param name="itemId">The internal item ID.</param>
    /// <returns>True if the item has an active reservation.</returns>
    Task<bool> IsItemReservedAsync(int itemId);
}