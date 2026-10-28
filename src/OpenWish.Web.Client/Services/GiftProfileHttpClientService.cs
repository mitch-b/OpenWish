using System.Net;
using System.Net.Http.Json;
using OpenWish.Shared.Models;
using OpenWish.Shared.Services;

namespace OpenWish.Web.Client.Services;

public class GiftProfileHttpClientService(HttpClient httpClient) : IGiftProfileService
{
    private const string BaseUrl = "api/gift-profile";

    public async Task<GiftProfileModel> GetMyProfileAsync(string userId) =>
        await httpClient.GetFromJsonAsync<GiftProfileModel>($"{BaseUrl}/me") ?? new GiftProfileModel();

    public async Task SetMyProfileAsync(string userId, GiftProfileModel profile)
    {
        var response = await httpClient.PutAsJsonAsync($"{BaseUrl}/me", profile);
        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            throw new ArgumentException(await response.Content.ReadAsStringAsync());
        }

        response.EnsureSuccessStatusCode();
    }

    public async Task<GiftProfileModel?> GetProfileForWishlistAsync(string wishlistPublicId, string viewerId)
    {
        var response = await httpClient.GetAsync($"{BaseUrl}/wishlists/{Uri.EscapeDataString(wishlistPublicId)}");
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<GiftProfileModel>();
    }
}