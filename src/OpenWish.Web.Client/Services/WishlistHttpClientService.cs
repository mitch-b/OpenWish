using OpenWish.Shared.Models;
using OpenWish.Shared.Services;
using System.Net.Http.Json;

namespace OpenWish.Web.Client.Services;

public class WishlistHttpClientService(HttpClient httpClient) : IWishlistService
{
    private readonly HttpClient _httpClient = httpClient;
    private const string BaseUrl = "api/wishlists";
    
    public async Task<WishlistModel> CreateWishlistAsync(WishlistModel wishlist, string ownerId)
    {
        var response = await _httpClient.PostAsJsonAsync(BaseUrl, wishlist);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<WishlistModel>();
    }

    public async Task<WishlistModel> GetWishlistAsync(int id)
    {
        return await _httpClient.GetFromJsonAsync<WishlistModel>($"{BaseUrl}/{id}");
    }

    public async Task<IEnumerable<WishlistModel>> GetUserWishlistsAsync(string userId)
    {
        // Assuming userId is not required since the server knows the authenticated user
        return await _httpClient.GetFromJsonAsync<IEnumerable<WishlistModel>>(BaseUrl);
    }

    public async Task<WishlistModel> UpdateWishlistAsync(int id, WishlistModel wishlist)
    {
        var response = await _httpClient.PutAsJsonAsync($"{BaseUrl}/{id}", wishlist);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<WishlistModel>();
    }

    public async Task DeleteWishlistAsync(int id)
    {
        var response = await _httpClient.DeleteAsync($"{BaseUrl}/{id}");
        response.EnsureSuccessStatusCode();
    }

    // Wishlist Items
    public async Task<WishlistItemModel> AddItemToWishlistAsync(int wishlistId, WishlistItemModel item)
    {
        var response = await _httpClient.PostAsJsonAsync($"{BaseUrl}/{wishlistId}/items", item);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<WishlistItemModel>();
    }

    public async Task<WishlistItemModel> GetWishlistItemAsync(int wishlistId, int itemId)
    {
        return await _httpClient.GetFromJsonAsync<WishlistItemModel>($"{BaseUrl}/{wishlistId}/items/{itemId}");
    }

    public async Task<IEnumerable<WishlistItemModel>> GetWishlistItemsAsync(int wishlistId)
    {
        return await _httpClient.GetFromJsonAsync<IEnumerable<WishlistItemModel>>($"{BaseUrl}/{wishlistId}/items");
    }

    public async Task<WishlistItemModel> UpdateWishlistItemAsync(int wishlistId, int itemId, WishlistItemModel item)
    {
        var response = await _httpClient.PutAsJsonAsync($"{BaseUrl}/{wishlistId}/items/{itemId}", item);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<WishlistItemModel>();
    }

    public async Task<bool> RemoveItemFromWishlistAsync(int wishlistId, int itemId)
    {
        var response = await _httpClient.DeleteAsync($"{BaseUrl}/{wishlistId}/items/{itemId}");
        if (response.IsSuccessStatusCode)
        {
            return true;
        }
        return false;
    }
    
    // Wishlist sharing and permissions
    public async Task<WishlistPermissionModel> ShareWishlistAsync(int wishlistId, string userId, string permissionType)
    {
        var shareRequest = new { userId, permissionType };
        var response = await _httpClient.PostAsJsonAsync($"{BaseUrl}/{wishlistId}/permissions", shareRequest);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<WishlistPermissionModel>();
    }
    
    public async Task<string> CreateSharingLinkAsync(int wishlistId, string permissionType, TimeSpan? expiration = null)
    {
        var linkRequest = new { permissionType, expiration };
        var response = await _httpClient.PostAsJsonAsync($"{BaseUrl}/{wishlistId}/share-link", linkRequest);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }
    
    public async Task<bool> AcceptSharingLinkAsync(string token, string userId)
    {
        var request = new { userId };
        var response = await _httpClient.PostAsJsonAsync($"{BaseUrl}/accept-link/{token}", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<bool>();
    }
    
    public async Task<IEnumerable<WishlistPermissionModel>> GetWishlistPermissionsAsync(int wishlistId)
    {
        return await _httpClient.GetFromJsonAsync<IEnumerable<WishlistPermissionModel>>($"{BaseUrl}/{wishlistId}/permissions");
    }
    
    public async Task<bool> RemoveWishlistPermissionAsync(int wishlistId, string userId)
    {
        var response = await _httpClient.DeleteAsync($"{BaseUrl}/{wishlistId}/permissions/{userId}");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<bool>();
    }
    
    public async Task<IEnumerable<WishlistModel>> GetSharedWithMeWishlistsAsync(string userId)
    {
        return await _httpClient.GetFromJsonAsync<IEnumerable<WishlistModel>>($"{BaseUrl}/shared-with-me");
    }
    
    public async Task<bool> CanUserAccessWishlistAsync(int wishlistId, string userId)
    {
        return await _httpClient.GetFromJsonAsync<bool>($"{BaseUrl}/{wishlistId}/can-access/{userId}");
    }
    
    public async Task<bool> CanUserEditWishlistAsync(int wishlistId, string userId)
    {
        return await _httpClient.GetFromJsonAsync<bool>($"{BaseUrl}/{wishlistId}/can-edit/{userId}");
    }
    
    // Item comments
    public async Task<ItemCommentModel> AddCommentToItemAsync(int wishlistId, int itemId, string userId, string text)
    {
        var commentRequest = new { userId, text };
        var response = await _httpClient.PostAsJsonAsync($"{BaseUrl}/{wishlistId}/items/{itemId}/comments", commentRequest);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ItemCommentModel>();
    }
    
    public async Task<IEnumerable<ItemCommentModel>> GetItemCommentsAsync(int wishlistId, int itemId)
    {
        return await _httpClient.GetFromJsonAsync<IEnumerable<ItemCommentModel>>($"{BaseUrl}/{wishlistId}/items/{itemId}/comments");
    }
    
    public async Task<bool> RemoveItemCommentAsync(int commentId, string userId)
    {
        var response = await _httpClient.DeleteAsync($"{BaseUrl}/comments/{commentId}?userId={userId}");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<bool>();
    }
    
    // Item reservations
    public async Task<bool> ReserveItemAsync(int wishlistId, int itemId, string userId, bool isAnonymous = false)
    {
        var reservationRequest = new { userId, isAnonymous };
        var response = await _httpClient.PostAsJsonAsync($"{BaseUrl}/{wishlistId}/items/{itemId}/reserve", reservationRequest);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<bool>();
    }
    
    public async Task<bool> CancelReservationAsync(int wishlistId, int itemId, string userId)
    {
        var response = await _httpClient.DeleteAsync($"{BaseUrl}/{wishlistId}/items/{itemId}/reservation?userId={userId}");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<bool>();
    }
    
    public async Task<ItemReservationModel?> GetItemReservationAsync(int wishlistId, int itemId)
    {
        var response = await _httpClient.GetAsync($"{BaseUrl}/{wishlistId}/items/{itemId}/reservation");
        
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }
        
        return await response.Content.ReadFromJsonAsync<ItemReservationModel>();
    }
    
    public async Task<bool> IsItemReservedAsync(int itemId)
    {
        return await _httpClient.GetFromJsonAsync<bool>($"{BaseUrl}/items/{itemId}/is-reserved");
    }
}