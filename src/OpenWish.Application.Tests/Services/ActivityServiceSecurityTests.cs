using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using OpenWish.Application.Models;
using OpenWish.Application.Services;
using OpenWish.Data;
using OpenWish.Data.Entities;
using Xunit;

namespace OpenWish.Application.Tests.Services;

public class ActivityServiceSecurityTests
{
    private readonly IMapper _mapper = new MapperConfiguration(
        configuration => configuration.AddProfile<OpenWishProfile>(),
        NullLoggerFactory.Instance).CreateMapper();

    [Fact]
    public async Task GetWishlistActivityAsync_HidesOwnerHiddenItemsAndAnonymousReservations()
    {
        var factory = CreateFactory();
        var (wishlistId, actorId) = await SeedActivityDataAsync(factory);
        var service = new ActivityService(factory, _mapper);

        var ownerActivities = await service.GetWishlistActivityAsync(wishlistId, "owner");
        var actorActivities = await service.GetWishlistActivityAsync(wishlistId, actorId);

        Assert.Empty(ownerActivities);
        Assert.Equal(3, actorActivities.Count());
    }

    [Fact]
    public async Task GetWishlistActivityAsync_ExcludesRemovedItemsBeforePagination()
    {
        var factory = CreateFactory();
        var (wishlistId, actorId) = await SeedActivityDataAsync(factory);
        await using (var context = factory.CreateDbContext())
        {
            var removedItem = await context.WishlistItems.SingleAsync(item => item.Name == "Surprise");
            removedItem.Deleted = true;
            var removedActivity = await context.ActivityLogs.SingleAsync(activity => activity.WishlistItemId == removedItem.Id);
            removedActivity.CreatedOn = DateTimeOffset.UtcNow.AddMinutes(1);
            await context.SaveChangesAsync();
        }

        var service = new ActivityService(factory, _mapper);
        var activities = await service.GetWishlistActivityAsync(wishlistId, actorId, count: 2);

        Assert.Equal(2, activities.Count());
        Assert.DoesNotContain(activities, activity => activity.Description == "Added surprise");
        Assert.Empty(await service.GetWishlistActivityAsync(wishlistId, "owner"));
    }

    [Fact]
    public async Task GetFriendsActivityFeedAsync_HidesAnonymousReservationActor()
    {
        var factory = CreateFactory();
        await SeedActivityDataAsync(factory);
        await using (var context = factory.CreateDbContext())
        {
            var wishlist = await context.Wishlists.SingleAsync();
            context.Friends.Add(new Friend { UserId = "viewer", FriendUserId = "actor" });
            context.WishlistPermissions.Add(new WishlistPermission
            {
                WishlistId = wishlist.Id,
                UserId = "viewer",
                PermissionType = "View"
            });
            await context.SaveChangesAsync();
        }
        var service = new ActivityService(factory, _mapper);

        var activities = await service.GetFriendsActivityFeedAsync("viewer");

        Assert.Equal(2, activities.Count());
        Assert.Contains(activities, activity => activity.Description == "Reserved visible gift");
        Assert.DoesNotContain(activities, activity => activity.Description == "Reserved gift");
    }

    [Fact]
    public async Task GetFriendsActivityFeed_HidesAllReservationsFromWishlistOwner()
    {
        var factory = CreateFactory();
        await SeedActivityDataAsync(factory);
        await using (var context = factory.CreateDbContext())
        {
            context.Friends.Add(new Friend { UserId = "owner", FriendUserId = "actor" });
            await context.SaveChangesAsync();
        }
        var service = new ActivityService(factory, _mapper);

        var activities = await service.GetFriendsActivityFeedAsync("owner");

        Assert.Empty(activities);
    }

    [Fact]
    public async Task GetUserActivityFeedAsync_ExcludesSoftDeletedActivity()
    {
        var factory = CreateFactory();
        await using (var context = factory.CreateDbContext())
        {
            context.Users.AddRange(
                new ApplicationUser { Id = "owner", UserName = "owner" },
                new ApplicationUser { Id = "other", UserName = "other" });
            context.ActivityLogs.AddRange(
                new ActivityLog { UserId = "owner", ActivityType = "ItemAdded", Description = "Visible" },
                new ActivityLog { UserId = "owner", ActivityType = "ItemAdded", Description = "Removed", Deleted = true },
                new ActivityLog { UserId = "other", ActivityType = "ItemAdded", Description = "Other" });
            await context.SaveChangesAsync();
        }

        var activities = await new ActivityService(factory, _mapper).GetUserActivityFeedAsync("owner");

        Assert.Equal("Visible", Assert.Single(activities).Description);
    }

    [Theory]
    [InlineData(false, false, 2)]
    [InlineData(true, false, 0)]
    [InlineData(false, true, 0)]
    public async Task GetFriendsActivityFeedAsync_ExcludesRemovedWishlistOrItem(
        bool wishlistDeleted, bool itemDeleted, int expectedCount)
    {
        var factory = CreateFactory();
        await SeedActivityDataAsync(factory);
        await using (var context = factory.CreateDbContext())
        {
            context.Friends.Add(new Friend { UserId = "viewer", FriendUserId = "actor" });
            var wishlist = await context.Wishlists.SingleAsync();
            context.WishlistPermissions.Add(new WishlistPermission
            {
                WishlistId = wishlist.Id,
                UserId = "viewer",
                PermissionType = "View"
            });
            wishlist.Deleted = wishlistDeleted;
            if (itemDeleted)
            {
                foreach (var item in context.WishlistItems)
                {
                    item.Deleted = true;
                }
            }
            await context.SaveChangesAsync();
        }

        var activities = await new ActivityService(factory, _mapper).GetFriendsActivityFeedAsync("viewer");

        Assert.Equal(expectedCount, activities.Count());
    }

    private static async Task<(int WishlistId, string ActorId)> SeedActivityDataAsync(TestDbContextFactory factory)
    {
        const string actorId = "actor";
        await using var context = factory.CreateDbContext();
        var owner = new ApplicationUser { Id = "owner", UserName = "owner" };
        var actor = new ApplicationUser { Id = actorId, UserName = actorId };
        var wishlist = new Wishlist
        {
            Name = "Wishlist",
            Owner = owner,
            OwnerId = owner.Id
        };
        var hiddenItem = new WishlistItem
        {
            Name = "Surprise",
            Wishlist = wishlist,
            IsHiddenFromOwner = true
        };
        var reservedItem = new WishlistItem
        {
            Name = "Reserved gift",
            Wishlist = wishlist
        };
        var visibleReservedItem = new WishlistItem
        {
            Name = "Visible reserved gift",
            Wishlist = wishlist
        };
        context.AddRange(owner, actor, wishlist, hiddenItem, reservedItem, visibleReservedItem);
        context.ActivityLogs.AddRange(
            new ActivityLog
            {
                User = actor,
                UserId = actorId,
                ActivityType = "ItemAdded",
                Description = "Added surprise",
                Wishlist = wishlist,
                WishlistItem = hiddenItem
            },
            new ActivityLog
            {
                User = actor,
                UserId = actorId,
                ActivityType = "ItemReserved",
                Description = "Reserved gift",
                Wishlist = wishlist,
                WishlistItem = reservedItem
            },
            new ActivityLog
            {
                User = actor,
                UserId = actorId,
                ActivityType = "ItemReserved",
                Description = "Reserved visible gift",
                Wishlist = wishlist,
                WishlistItem = visibleReservedItem
            });
        context.ItemReservations.AddRange(
            new ItemReservation
            {
                User = actor,
                UserId = actorId,
                WishlistItem = reservedItem,
                IsAnonymous = true
            },
            new ItemReservation
            {
                User = actor,
                UserId = actorId,
                WishlistItem = visibleReservedItem,
                IsAnonymous = false
            });
        await context.SaveChangesAsync();
        return (wishlist.Id, actorId);
    }

    private static TestDbContextFactory CreateFactory() =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private sealed class TestDbContextFactory(DbContextOptions<ApplicationDbContext> options)
        : IDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext() => new(options);
    }
}