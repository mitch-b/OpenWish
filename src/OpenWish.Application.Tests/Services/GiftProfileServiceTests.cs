using Microsoft.EntityFrameworkCore;
using OpenWish.Application.Services;
using OpenWish.Data;
using OpenWish.Data.Entities;
using OpenWish.Shared.Models;
using Xunit;

namespace OpenWish.Application.Tests.Services;

public class GiftProfileServiceTests
{
    [Fact]
    public async Task GetProfileForWishlistAsync_ReturnsHintsToFriendsOnly()
    {
        var factory = new TestFactory();
        await using (var seed = factory.CreateDbContext())
        {
            seed.Users.AddRange(
                new ApplicationUser { Id = "owner", UserName = "owner", GiftSizes = "M", GiftAvoid = "Peanuts" },
                new ApplicationUser { Id = "friend", UserName = "friend" },
                new ApplicationUser { Id = "stranger", UserName = "stranger" });
            seed.Friends.Add(new Friend { UserId = "friend", FriendUserId = "owner" });
            seed.Wishlists.Add(new Wishlist { Name = "List", OwnerId = "owner", PublicId = "owner-list" });
            await seed.SaveChangesAsync();
        }

        var service = new GiftProfileService(factory);

        var profile = await service.GetProfileForWishlistAsync("owner-list", "friend");
        Assert.Equal("M", profile?.Sizes);
        Assert.Equal("Peanuts", profile?.Avoid);
        Assert.Null(await service.GetProfileForWishlistAsync("owner-list", "stranger"));
        Assert.Null(await service.GetProfileForWishlistAsync("owner-list", "owner"));
        Assert.Null(await service.GetProfileForWishlistAsync("missing", "friend"));
    }

    [Fact]
    public async Task GetProfileForWishlistAsync_ReturnsNothingWhenOwnerSharedNoHints()
    {
        var factory = new TestFactory();
        await using (var seed = factory.CreateDbContext())
        {
            seed.Users.AddRange(new ApplicationUser { Id = "owner", UserName = "owner" }, new ApplicationUser { Id = "friend", UserName = "friend" });
            seed.Friends.Add(new Friend { UserId = "owner", FriendUserId = "friend" });
            seed.Wishlists.Add(new Wishlist { Name = "List", OwnerId = "owner", PublicId = "owner-list" });
            await seed.SaveChangesAsync();
        }

        Assert.Null(await new GiftProfileService(factory).GetProfileForWishlistAsync("owner-list", "friend"));
    }

    [Fact]
    public async Task SetMyProfileAsync_TrimsClearsAndRejectsLongNotes()
    {
        var factory = new TestFactory();
        await using (var seed = factory.CreateDbContext())
        {
            seed.Users.Add(new ApplicationUser { Id = "me", UserName = "me" });
            await seed.SaveChangesAsync();
        }

        var service = new GiftProfileService(factory);
        await service.SetMyProfileAsync("me", new GiftProfileModel { Sizes = "  L  ", Favorites = "   " });
        var saved = await service.GetMyProfileAsync("me");
        Assert.Equal("L", saved.Sizes);
        Assert.Null(saved.Favorites);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.SetMyProfileAsync("me", new GiftProfileModel { Avoid = new string('x', GiftProfileModel.MaxLength + 1) }));
        Assert.Equal("L", (await service.GetMyProfileAsync("me")).Sizes);
    }

    private sealed class TestFactory : IDbContextFactory<ApplicationDbContext>
    {
        private readonly string _name = Guid.NewGuid().ToString();

        public ApplicationDbContext CreateDbContext() =>
            new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(_name).Options);
    }
}