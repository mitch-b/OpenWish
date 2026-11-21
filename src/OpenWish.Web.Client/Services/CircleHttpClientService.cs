using System.Net;
using System.Net.Http.Json;
using OpenWish.Shared.Models;
using OpenWish.Shared.Services;

namespace OpenWish.Web.Client.Services;

public class CircleHttpClientService(HttpClient httpClient) : ICircleService
{
    private const string BaseUrl = "api/circles";

    public async Task<IReadOnlyList<CircleModel>> GetCirclesAsync(string userId) =>
        await httpClient.GetFromJsonAsync<List<CircleModel>>(BaseUrl) ?? [];

    public async Task<CircleModel> CreateCircleAsync(string userId, string name) =>
        await ReadCircleAsync(await httpClient.PostAsJsonAsync(BaseUrl, new CircleNameRequest { Name = name }));

    public async Task<CircleModel> RenameCircleAsync(string userId, string circlePublicId, string name) =>
        await ReadCircleAsync(await httpClient.PutAsJsonAsync($"{BaseUrl}/{Uri.EscapeDataString(circlePublicId)}", new CircleNameRequest { Name = name }));

    public async Task DeleteCircleAsync(string userId, string circlePublicId) =>
        await EnsureSuccessAsync(await httpClient.DeleteAsync($"{BaseUrl}/{Uri.EscapeDataString(circlePublicId)}"));

    public async Task<CircleModel> AddMemberAsync(string userId, string circlePublicId, string memberUserId) =>
        await ReadCircleAsync(await httpClient.PostAsJsonAsync($"{BaseUrl}/{Uri.EscapeDataString(circlePublicId)}/members", new CircleMemberRequest { UserId = memberUserId }));

    public async Task<CircleModel> RemoveMemberAsync(string userId, string circlePublicId, string memberUserId) =>
        await ReadCircleAsync(await httpClient.DeleteAsync($"{BaseUrl}/{Uri.EscapeDataString(circlePublicId)}/members/{Uri.EscapeDataString(memberUserId)}"));

    public async Task<CircleModel> ShareWishlistAsync(string userId, string circlePublicId, string wishlistPublicId) =>
        await ReadCircleAsync(await httpClient.PostAsJsonAsync($"{BaseUrl}/{Uri.EscapeDataString(circlePublicId)}/wishlists", new CircleWishlistRequest { WishlistPublicId = wishlistPublicId }));

    public async Task<CircleModel> UnshareWishlistAsync(string userId, string circlePublicId, string wishlistPublicId) =>
        await ReadCircleAsync(await httpClient.DeleteAsync($"{BaseUrl}/{Uri.EscapeDataString(circlePublicId)}/wishlists/{Uri.EscapeDataString(wishlistPublicId)}"));

    private static async Task<CircleModel> ReadCircleAsync(HttpResponseMessage response)
    {
        await EnsureSuccessAsync(response);
        return await response.Content.ReadFromJsonAsync<CircleModel>()
            ?? throw new HttpRequestException("The circle could not be read.");
    }

    // The server's message is safe to show: it only describes the caller's own request.
    private static async Task EnsureSuccessAsync(HttpResponseMessage response)
    {
        switch (response.StatusCode)
        {
            case HttpStatusCode.NotFound:
                throw new KeyNotFoundException("That circle or wishlist no longer exists.");
            case HttpStatusCode.BadRequest:
                {
                    var message = await response.Content.ReadAsStringAsync();
                    throw new ArgumentException(message.StartsWith('{') ? "Check the circle name and try again." : message);
                }
            case HttpStatusCode.Conflict:
                throw new InvalidOperationException(await response.Content.ReadAsStringAsync());
            default:
                response.EnsureSuccessStatusCode();
                break;
        }
    }
}