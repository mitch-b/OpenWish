using OpenWish.Shared.Models;
using OpenWish.Shared.Services;
using System.Net.Http.Json;

namespace OpenWish.Web.Client.Services;

public class WishlistHttpClientService(HttpClient httpClient) : IWishlistService
{
    public async Task<WishlistModel> CreateWishlistAsync(WishlistModel wishlist, string ownerId)
    {
        var response = await httpClient.PostAsJsonAsync("api/wishlists", wishlist);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<WishlistModel>();
    }

    public async Task<WishlistModel> GetWishlistAsync(int id)
    {
        return await httpClient.GetFromJsonAsync<WishlistModel>($"api/wishlists/{id}");
    }

    public async Task<IEnumerable<WishlistModel>> GetUserWishlistsAsync(string userId)
    {
        // Assuming userId is not required since the server knows the authenticated user
        return await httpClient.GetFromJsonAsync<IEnumerable<WishlistModel>>("api/wishlists");
    }

    public async Task<WishlistModel> UpdateWishlistAsync(int id, WishlistModel wishlist)
    {
        var response = await httpClient.PutAsJsonAsync($"api/wishlists/{id}", wishlist);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<WishlistModel>();
    }

    public async Task DeleteWishlistAsync(int id)
    {
        var response = await httpClient.DeleteAsync($"api/wishlists/{id}");
        response.EnsureSuccessStatusCode();
    }

    // Wishlist Items
    public async Task<WishlistItemModel> AddItemToWishlistAsync(int wishlistId, WishlistItemModel item)
    {
        var response = await httpClient.PostAsJsonAsync($"api/wishlists/{wishlistId}/items", item);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<WishlistItemModel>();
    }

    public async Task<WishlistItemModel> GetWishlistItemAsync(int wishlistId, int itemId)
    {
        return await httpClient.GetFromJsonAsync<WishlistItemModel>($"api/wishlists/{wishlistId}/items/{itemId}");
    }

    public async Task<IEnumerable<WishlistItemModel>> GetWishlistItemsAsync(int wishlistId)
    {
        return await httpClient.GetFromJsonAsync<IEnumerable<WishlistItemModel>>($"api/wishlists/{wishlistId}/items");
    }

    public async Task<WishlistItemModel> UpdateWishlistItemAsync(int wishlistId, int itemId, WishlistItemModel item)
    {
        var response = await httpClient.PutAsJsonAsync($"api/wishlists/{wishlistId}/items/{itemId}", item);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<WishlistItemModel>();
    }

    public async Task<bool> RemoveItemFromWishlistAsync(int wishlistId, int itemId)
    {
        var response = await httpClient.DeleteAsync($"api/wishlists/{wishlistId}/items/{itemId}");
        if (response.IsSuccessStatusCode)
        {
            return true;
        }
        return false;
    }
}