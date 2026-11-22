using System.Net;
using System.Net.Http.Json;
using OpenWish.Shared.Models;
using Xunit;

namespace OpenWish.Web.Tests.Integrations;

[Collection(IntegrationCollection.Name)]
public sealed class WishlistItemPublicIdRouteTests(OpenWishAppFactory factory)
{
    [Fact]
    public async Task ItemRoutes_AddressItemsByPublicIdBecauseInternalIdsAreNotSerialized()
    {
        using var owner = factory.CreateBrowser();
        await owner.SignInAsync("owner");
        var list = await owner.CreateWishlistAsync($"Public id routes {Guid.NewGuid():N}");
        var otherList = await owner.CreateWishlistAsync($"Other list {Guid.NewGuid():N}");
        await owner.AddOwnItemAsync(list.PublicId, new WishlistItemModel { Name = "Route idea" });
        var item = Assert.Single(await owner.GetItemsAsync(list.PublicId));
        Assert.Equal(0, item.Id);
        var path = $"api/wishlists/{list.PublicId}/items/by-public-id/{item.PublicId}";

        Assert.Equal(HttpStatusCode.NoContent, (await owner.Client.GetAsync($"{path}/reservation")).StatusCode);

        var commentResponse = await owner.Client.PostAsJsonAsync($"{path}/comments", new { Text = "Looks great" });
        commentResponse.EnsureSuccessStatusCode();
        var comments = await owner.Client.GetFromJsonAsync<List<ItemCommentModel>>($"{path}/comments", OpenWishBrowser.Json);
        var comment = Assert.Single(comments!);
        var removed = await owner.Client.DeleteAsync($"api/wishlists/comments/by-public-id/{comment.PublicId}");
        removed.EnsureSuccessStatusCode();
        Assert.True(await removed.Content.ReadFromJsonAsync<bool>());

        using var stranger = factory.CreateBrowser();
        await stranger.SignInAsync("friend");
        Assert.Equal(HttpStatusCode.Forbidden, (await stranger.Client.DeleteAsync(path)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await owner.Client.DeleteAsync(
            $"api/wishlists/{otherList.PublicId}/items/by-public-id/{item.PublicId}")).StatusCode);

        Assert.Equal(HttpStatusCode.NoContent, (await owner.Client.DeleteAsync(path)).StatusCode);
        Assert.Empty(await owner.GetItemsAsync(list.PublicId));
    }
}