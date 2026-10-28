using System.Net.Http.Json;
using OpenWish.Shared.Models;
using OpenWish.Shared.RequestModels;
using OpenWish.Shared.Services;

namespace OpenWish.Web.Client.Services;

public class GiftPlanHttpClientService(HttpClient httpClient) : IGiftPlanService
{
    private const string BaseUrl = "api/gift-plan";

    public async Task<GiftPlanModel> GetGiftPlanAsync(string userId) =>
        await httpClient.GetFromJsonAsync<GiftPlanModel>(BaseUrl) ?? new GiftPlanModel();

    public async Task SetGiftPurchasedAsync(string userId, string wishlistPublicId, string itemPublicId, bool isPurchased)
    {
        var response = await httpClient.PutAsJsonAsync(
            $"{BaseUrl}/{Uri.EscapeDataString(wishlistPublicId)}/items/{Uri.EscapeDataString(itemPublicId)}/purchased",
            new UpdateGiftPurchasedRequest { IsPurchased = isPurchased });
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            throw new KeyNotFoundException("You have not reserved that gift.");
        }

        response.EnsureSuccessStatusCode();
    }
}