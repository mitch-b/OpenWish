using System.Net.Http.Json;
using OpenWish.Shared.Models;
using OpenWish.Shared.Services;

namespace OpenWish.Web.Client.Services;

public class GiftPlanHttpClientService(HttpClient httpClient) : IGiftPlanService
{
    private const string BaseUrl = "api/gift-plan";

    public async Task<GiftPlanModel> GetGiftPlanAsync(string userId) =>
        await httpClient.GetFromJsonAsync<GiftPlanModel>(BaseUrl) ?? new GiftPlanModel();
}