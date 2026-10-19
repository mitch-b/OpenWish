using Microsoft.EntityFrameworkCore;
using OpenWish.Application.Services;
using OpenWish.Data;
using OpenWish.Data.Entities;
using Xunit;

namespace OpenWish.Application.Tests.Services;

public class DashboardServiceTests
{
    [Fact]
    public async Task GetFirstRunChecklistAsync_ReportsProgressForANewAccount()
    {
        var factory = new TestContextFactory();
        await using (var seed = factory.CreateDbContext())
        {
            seed.Users.Add(new ApplicationUser { Id = "new", UserName = "new" });
            await seed.SaveChangesAsync();
        }

        var checklist = await new DashboardService(factory).GetFirstRunChecklistAsync("new");

        Assert.True(checklist.IsVisible);
        Assert.Equal(0, checklist.CompletedCount);
    }

    [Fact]
    public async Task GetFirstRunChecklistAsync_MarksStepsDoneFromOwnActivityOnly()
    {
        var factory = new TestContextFactory();
        await using (var seed = factory.CreateDbContext())
        {
            var me = new ApplicationUser { Id = "me", UserName = "me" };
            var other = new ApplicationUser { Id = "other", UserName = "other" };
            seed.Users.AddRange(me, other);
            var list = new Wishlist { Name = "Mine", Owner = me, OwnerId = "me" };
            list.Items = [new WishlistItem { Name = "Idea" }];
            var deleted = new Wishlist { Name = "Gone", Owner = me, OwnerId = "me", Deleted = true };
            seed.Wishlists.AddRange(list, deleted);
            seed.Wishlists.Add(new Wishlist { Name = "Theirs", Owner = other, OwnerId = "other" });
            await seed.SaveChangesAsync();
        }

        var service = new DashboardService(factory);
        var mine = await service.GetFirstRunChecklistAsync("me");
        var theirs = await service.GetFirstRunChecklistAsync("other");

        Assert.True(mine.HasWishlist);
        Assert.True(mine.HasIdea);
        Assert.False(mine.HasFriend);
        Assert.False(mine.HasEvent);
        Assert.True(theirs.HasWishlist);
        Assert.False(theirs.HasIdea);
    }

    [Fact]
    public async Task DismissFirstRunChecklistAsync_HidesTheChecklistPermanently()
    {
        var factory = new TestContextFactory();
        await using (var seed = factory.CreateDbContext())
        {
            seed.Users.Add(new ApplicationUser { Id = "me", UserName = "me" });
            await seed.SaveChangesAsync();
        }

        var service = new DashboardService(factory);
        await service.DismissFirstRunChecklistAsync("me");
        await service.DismissFirstRunChecklistAsync("me");

        var checklist = await service.GetFirstRunChecklistAsync("me");
        Assert.False(checklist.IsVisible);
        Assert.True(checklist.IsDismissed);
    }

    [Fact]
    public async Task DismissFirstRunChecklistAsync_RejectsUnknownUsers()
    {
        var service = new DashboardService(new TestContextFactory());

        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.DismissFirstRunChecklistAsync("missing"));
    }

    private sealed class TestContextFactory : IDbContextFactory<ApplicationDbContext>
    {
        private readonly string _name = Guid.NewGuid().ToString();

        public ApplicationDbContext CreateDbContext() =>
            new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(_name).Options);
    }
}