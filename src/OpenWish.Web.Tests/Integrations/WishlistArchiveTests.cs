using System.Net;
using System.Net.Http.Json;
using OpenWish.Shared.Models;
using Xunit;

namespace OpenWish.Web.Tests.Integrations;

[Collection(IntegrationCollection.Name)]
public sealed class WishlistArchiveTests(OpenWishAppFactory factory)
{
    [Fact]
    public async Task Archive_IsOwnerOnly_AndRestoresContributorAccess()
    {
        using var owner = factory.CreateBrowser();
        await owner.SignInAsync("owner");
        using var contributor = factory.CreateBrowser();
        await contributor.SignInAsync("friend");
        var contributorList = await contributor.CreateWishlistAsync($"Contributor identity {Guid.NewGuid():N}");
        var list = await owner.CreateWishlistAsync($"Archived list {Guid.NewGuid():N}");
        var path = $"api/wishlists/{list.PublicId}";
        var share = await owner.Client.PostAsJsonAsync($"{path}/permissions",
            new { UserId = contributorList.OwnerId, PermissionType = "Edit" });
        share.EnsureSuccessStatusCode();
        await owner.AddOwnItemAsync(list.PublicId, new WishlistItemModel { Name = "Holiday gift" });

        Assert.Equal(HttpStatusCode.OK, (await contributor.Client.GetAsync(path)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await contributor.Client.PutAsJsonAsync($"{path}/archived", true)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await owner.Client.PutAsJsonAsync($"{path}/archived", true)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await contributor.Client.GetAsync(path)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await contributor.Client.PostAsJsonAsync($"{path}/items",
            new WishlistItemModel { Name = "Inaccessible draft" })).StatusCode);

        var active = await owner.Client.GetFromJsonAsync<List<WishlistModel>>("api/wishlists", OpenWishBrowser.Json);
        var archived = await owner.Client.GetFromJsonAsync<List<WishlistModel>>("api/wishlists?archived=true", OpenWishBrowser.Json);
        Assert.DoesNotContain(active!, wishlist => wishlist.PublicId == list.PublicId);
        var archivedList = Assert.Single(archived!, wishlist => wishlist.PublicId == list.PublicId);
        Assert.True(archivedList.IsArchived);
        Assert.Equal("Holiday gift", Assert.Single(archivedList.Items).Name);

        Assert.Equal(HttpStatusCode.NoContent, (await owner.Client.PutAsJsonAsync($"{path}/archived", false)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await contributor.Client.GetAsync(path)).StatusCode);
        Assert.True((await contributor.Client.PostAsJsonAsync($"{path}/items",
            new WishlistItemModel { Name = "After restore" })).IsSuccessStatusCode);
    }
}