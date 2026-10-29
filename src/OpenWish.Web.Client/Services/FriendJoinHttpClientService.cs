using System.Net.Http.Json;
using OpenWish.Shared.Models;
using OpenWish.Shared.Services;

namespace OpenWish.Web.Client.Services;

public class FriendJoinHttpClientService(HttpClient httpClient) : IFriendJoinService
{
    private const string BaseUrl = "api/friends/join";

    public async Task<FriendJoinCodeModel> CreateJoinCodeAsync(string userId, string? origin = null)
    {
        var response = await httpClient.PostAsync($"{BaseUrl}/code", null);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<FriendJoinCodeModel>()
            ?? throw new InvalidOperationException("The connection code could not be created.");
    }

    public async Task<bool> CancelJoinCodeAsync(string userId)
    {
        var response = await httpClient.DeleteAsync($"{BaseUrl}/code");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<bool>();
    }

    public async Task<FriendJoinPreviewModel> PreviewJoinCodeAsync(string userId, string code) =>
        await PostCodeAsync("preview", code);

    public async Task<FriendJoinPreviewModel> RedeemJoinCodeAsync(string userId, string code) =>
        await PostCodeAsync("redeem", code);

    private async Task<FriendJoinPreviewModel> PostCodeAsync(string action, string code)
    {
        var response = await httpClient.PostAsJsonAsync($"{BaseUrl}/{action}", new FriendJoinRequestModel { Code = code });
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<FriendJoinPreviewModel>()
            ?? new FriendJoinPreviewModel { Status = FriendJoinStatus.Invalid };
    }
}