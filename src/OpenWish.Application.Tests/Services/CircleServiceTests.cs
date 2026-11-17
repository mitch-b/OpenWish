using Microsoft.EntityFrameworkCore;
using OpenWish.Application.Services;
using OpenWish.Data;
using OpenWish.Data.Entities;
using Xunit;

namespace OpenWish.Application.Tests.Services;

public class CircleServiceTests
{
    [Fact]
    public async Task CreateAndRename_TrimNamesAndRejectDuplicatesAndStrangers()
    {
        var factory = await SeedAsync();
        var service = new CircleService(factory);

        var circle = await service.CreateCircleAsync("owner", "  Close   family ");
        Assert.Equal("Close family", circle.Name);
        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateCircleAsync("owner", "close FAMILY"));
        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateCircleAsync("owner", "   "));
        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateCircleAsync("owner", new string('x', 41)));

        var renamed = await service.RenameCircleAsync("owner", circle.PublicId, "Family");
        Assert.Equal("Family", renamed.Name);
        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.RenameCircleAsync("stranger", circle.PublicId, "Mine"));
        Assert.Empty(await service.GetCirclesAsync("stranger"));
    }

    [Fact]
    public async Task AddMember_RequiresAFriendAndIsIdempotent()
    {
        var factory = await SeedAsync();
        var service = new CircleService(factory);
        var circle = await service.CreateCircleAsync("owner", "Family");

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.AddMemberAsync("owner", circle.PublicId, "stranger"));
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.AddMemberAsync("owner", circle.PublicId, "owner"));
        await service.AddMemberAsync("owner", circle.PublicId, "ann");
        var again = await service.AddMemberAsync("owner", circle.PublicId, "ann");

        Assert.Equal(["ann"], again.Members.Select(m => m.UserName));
        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.AddMemberAsync("ann", circle.PublicId, "owner"));
    }

    [Fact]
    public async Task SharedWishlist_ReachesCurrentAndFutureMembersAndLeavesWithThem()
    {
        var factory = await SeedAsync();
        var service = new CircleService(factory);
        var circle = await service.CreateCircleAsync("owner", "Family");
        await service.AddMemberAsync("owner", circle.PublicId, "ann");

        var shared = await service.ShareWishlistAsync("owner", circle.PublicId, "list-1");
        Assert.Equal(["Birthday"], shared.Wishlists.Select(w => w.Name));
        Assert.True(await HasAccessAsync(factory, "list-1", "ann"));
        Assert.False(await HasAccessAsync(factory, "list-1", "ben"));

        await service.AddMemberAsync("owner", circle.PublicId, "ben");
        Assert.True(await HasAccessAsync(factory, "list-1", "ben"));
        Assert.False(await HasAccessAsync(factory, "list-1", "stranger"));

        await service.RemoveMemberAsync("owner", circle.PublicId, "ben");
        Assert.False(await HasAccessAsync(factory, "list-1", "ben"));
        Assert.True(await HasAccessAsync(factory, "list-1", "ann"));
    }

    [Fact]
    public async Task AccessFromAnotherCircleOrAPersonalShareSurvives()
    {
        var factory = await SeedAsync();
        var service = new CircleService(factory);
        var family = await service.CreateCircleAsync("owner", "Family");
        var club = await service.CreateCircleAsync("owner", "Book club");
        foreach (var circle in new[] { family, club })
        {
            await service.AddMemberAsync("owner", circle.PublicId, "ann");
            await service.ShareWishlistAsync("owner", circle.PublicId, "list-1");
        }

        await service.RemoveMemberAsync("owner", family.PublicId, "ann");
        Assert.True(await HasAccessAsync(factory, "list-1", "ann"));
        await service.DeleteCircleAsync("owner", club.PublicId);
        Assert.False(await HasAccessAsync(factory, "list-1", "ann"));

        await using (var seed = factory.CreateDbContext())
        {
            seed.WishlistPermissions.Add(new WishlistPermission { WishlistId = 1, UserId = "ben", PermissionType = "Edit" });
            await seed.SaveChangesAsync();
        }

        var third = await service.CreateCircleAsync("owner", "Third");
        await service.AddMemberAsync("owner", third.PublicId, "ben");
        await service.ShareWishlistAsync("owner", third.PublicId, "list-1");
        await service.DeleteCircleAsync("owner", third.PublicId);
        Assert.True(await HasAccessAsync(factory, "list-1", "ben"));
        await using var verify = factory.CreateDbContext();
        Assert.Equal("Edit", (await verify.WishlistPermissions.SingleAsync(p => p.UserId == "ben" && !p.Deleted)).PermissionType);
    }

    [Fact]
    public async Task UnshareWishlist_WithdrawsAccessAndOnlyOwnersMayShare()
    {
        var factory = await SeedAsync();
        var service = new CircleService(factory);
        var circle = await service.CreateCircleAsync("owner", "Family");
        await service.AddMemberAsync("owner", circle.PublicId, "ann");
        await service.ShareWishlistAsync("owner", circle.PublicId, "list-1");

        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.ShareWishlistAsync("owner", circle.PublicId, "other-list"));
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ShareWishlistAsync("owner", circle.PublicId, "private-list"));

        var after = await service.UnshareWishlistAsync("owner", circle.PublicId, "list-1");
        Assert.Empty(after.Wishlists);
        Assert.False(await HasAccessAsync(factory, "list-1", "ann"));
    }

    [Fact]
    public async Task EndingAFriendship_RemovesThemFromCirclesAndWithdrawsAccess()
    {
        var factory = await SeedAsync();
        var service = new CircleService(factory);
        var circle = await service.CreateCircleAsync("owner", "Family");
        await service.AddMemberAsync("owner", circle.PublicId, "ann");
        await service.ShareWishlistAsync("owner", circle.PublicId, "list-1");

        await using (var context = factory.CreateDbContext())
        {
            await CircleService.RemoveFromOwnerCirclesAsync(context, "owner", "ann");
        }

        Assert.False(await HasAccessAsync(factory, "list-1", "ann"));
        Assert.Empty((await service.GetCirclesAsync("owner"))[0].Members);
    }

    private static async Task<bool> HasAccessAsync(TestFactory factory, string wishlistPublicId, string userId)
    {
        await using var context = factory.CreateDbContext();
        return await context.WishlistPermissions.AnyAsync(p =>
            !p.Deleted && p.UserId == userId && p.Wishlist.PublicId == wishlistPublicId);
    }

    private static async Task<TestFactory> SeedAsync()
    {
        var factory = new TestFactory();
        await using var seed = factory.CreateDbContext();
        seed.Users.AddRange(
            new ApplicationUser { Id = "owner", UserName = "owner" },
            new ApplicationUser { Id = "ann", UserName = "ann" },
            new ApplicationUser { Id = "ben", UserName = "ben" },
            new ApplicationUser { Id = "stranger", UserName = "stranger" });
        seed.Friends.AddRange(
            new Friend { UserId = "owner", FriendUserId = "ann" },
            new Friend { UserId = "ann", FriendUserId = "owner" },
            new Friend { UserId = "owner", FriendUserId = "ben" },
            new Friend { UserId = "ben", FriendUserId = "owner" });
        seed.Wishlists.AddRange(
            new Wishlist { Id = 1, Name = "Birthday", OwnerId = "owner", PublicId = "list-1", IsFriendsOnly = true },
            new Wishlist { Id = 2, Name = "Secret", OwnerId = "owner", PublicId = "private-list", IsPrivate = true },
            new Wishlist { Id = 3, Name = "Theirs", OwnerId = "ann", PublicId = "other-list", IsFriendsOnly = true });
        await seed.SaveChangesAsync();
        return factory;
    }

    private sealed class TestFactory : IDbContextFactory<ApplicationDbContext>
    {
        private readonly string _name = Guid.NewGuid().ToString();

        public ApplicationDbContext CreateDbContext() =>
            new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(_name).Options);
    }
}