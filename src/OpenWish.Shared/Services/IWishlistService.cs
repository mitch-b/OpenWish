using OpenWish.Shared.Models;

namespace OpenWish.Shared.Services;

public interface IWishlistService
{
    Task<WishlistModel> CreateWishlistAsync(WishlistModel wishlist, string ownerId);
    Task<WishlistModel> GetWishlistAsync(int id);
    Task<IEnumerable<WishlistModel>> GetUserWishlistsAsync(string userId);
    Task<WishlistModel> UpdateWishlistAsync(int id, WishlistModel wishlist);
    Task DeleteWishlistAsync(int id);

    // WishlistItems
    Task<WishlistItemModel> GetWishlistItemAsync(int wishlistId, int itemId);
    Task<IEnumerable<WishlistItemModel>> GetWishlistItemsAsync(int wishlistId);
    Task<WishlistItemModel> AddItemToWishlistAsync(int wishlistId, WishlistItemModel item);
    Task<bool> RemoveItemFromWishlistAsync(int wishlistId, int itemId);
    Task<WishlistItemModel> UpdateWishlistItemAsync(int wishlistId, int itemId, WishlistItemModel item);

}
