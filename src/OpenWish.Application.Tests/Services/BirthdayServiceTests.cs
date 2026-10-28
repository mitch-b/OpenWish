using Microsoft.EntityFrameworkCore;
using OpenWish.Application.Services;
using OpenWish.Data;
using OpenWish.Data.Entities;
using Xunit;

namespace OpenWish.Application.Tests.Services;

public class BirthdayServiceTests
{
    private static readonly DateTimeOffset _today = new(2026, 10, 28, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task GetUpcomingBirthdaysAsync_ReturnsOnlyFriendsInsideTheWindowSoonestFirst()
    {
        var factory = new TestFactory();
        await using (var seed = factory.CreateDbContext())
        {
            seed.Users.AddRange(
                new ApplicationUser { Id = "me", UserName = "me" },
                new ApplicationUser { Id = "soon", UserName = "soon", BirthdayMonth = 11, BirthdayDay = 2 },
                new ApplicationUser { Id = "today", UserName = "today", BirthdayMonth = 10, BirthdayDay = 28 },
                new ApplicationUser { Id = "far", UserName = "far", BirthdayMonth = 3, BirthdayDay = 1 },
                new ApplicationUser { Id = "stranger", UserName = "stranger", BirthdayMonth = 10, BirthdayDay = 30 },
                new ApplicationUser { Id = "unset", UserName = "unset" },
                new ApplicationUser { Id = "emailed", UserName = "person@example.com", BirthdayMonth = 11, BirthdayDay = 20 });
            seed.Friends.AddRange(
                new Friend { UserId = "me", FriendUserId = "soon" },
                new Friend { UserId = "today", FriendUserId = "me" },
                new Friend { UserId = "me", FriendUserId = "far" },
                new Friend { UserId = "me", FriendUserId = "unset" },
                new Friend { UserId = "me", FriendUserId = "emailed" });
            seed.Wishlists.Add(new Wishlist { Name = "Soon list", OwnerId = "soon", PublicId = "soon-list" });
            await seed.SaveChangesAsync();
        }

        var results = await Service(factory).GetUpcomingBirthdaysAsync("me");

        Assert.Equal(["today", "soon", "A friend"], results.Select(r => r.UserName));
        Assert.Equal([0, 5, 23], results.Select(r => r.DaysUntil));
        Assert.Equal("soon-list", results[1].WishlistPublicId);
        Assert.Null(results[0].WishlistPublicId);
    }

    [Fact]
    public async Task GetUpcomingBirthdaysAsync_IgnoresRemovedFriendships()
    {
        var factory = new TestFactory();
        await using (var seed = factory.CreateDbContext())
        {
            seed.Users.AddRange(
                new ApplicationUser { Id = "me", UserName = "me" },
                new ApplicationUser { Id = "gone", UserName = "gone", BirthdayMonth = 10, BirthdayDay = 29 });
            seed.Friends.Add(new Friend { UserId = "me", FriendUserId = "gone", Deleted = true });
            await seed.SaveChangesAsync();
        }

        Assert.Empty(await Service(factory).GetUpcomingBirthdaysAsync("me"));
    }

    [Fact]
    public async Task SetBirthdayAsync_SavesClearsAndRejectsInvalidDates()
    {
        var factory = new TestFactory();
        await using (var seed = factory.CreateDbContext())
        {
            seed.Users.Add(new ApplicationUser { Id = "me", UserName = "me" });
            await seed.SaveChangesAsync();
        }

        var service = Service(factory);
        await service.SetBirthdayAsync("me", 2, 29);
        Assert.Equal((2, 29), ((await service.GetBirthdayAsync("me")).Month, (await service.GetBirthdayAsync("me")).Day));

        await Assert.ThrowsAsync<ArgumentException>(() => service.SetBirthdayAsync("me", 2, 30));
        await Assert.ThrowsAsync<ArgumentException>(() => service.SetBirthdayAsync("me", 5, null));

        await service.SetBirthdayAsync("me", null, null);
        var cleared = await service.GetBirthdayAsync("me");
        Assert.Null(cleared.Month);
        Assert.Null(cleared.Day);
    }

    private static BirthdayService Service(TestFactory factory) =>
        new(factory, new FixedTimeProvider(_today));

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed class TestFactory : IDbContextFactory<ApplicationDbContext>
    {
        private readonly string _name = Guid.NewGuid().ToString();

        public ApplicationDbContext CreateDbContext() =>
            new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(_name).Options);
    }
}