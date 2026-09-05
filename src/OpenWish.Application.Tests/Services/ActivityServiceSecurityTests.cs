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

        var ownerActivities = await service.GetWishlistActivityAsync(wishlistId, requestingUserId: "owner");
        var actorActivities = await service.GetWishlistActivityAsync(wishlistId, requestingUserId: actorId);

        Assert.Empty(ownerActivities);
        Assert.Equal(2, actorActivities.Count());
    }

    [Fact]
    public async Task GetFriendsActivityFeedAsync_HidesAnonymousReservationActor()
    {
        var factory = CreateFactory();
        await SeedActivityDataAsync(factory);
        await using (var context = factory.CreateDbContext())
        {
            context.Friends.Add(new Friend { UserId = "viewer", FriendUserId = "actor" });
            await context.SaveChangesAsync();
        }
        var service = new ActivityService(factory, _mapper);

        var activities = await service.GetFriendsActivityFeedAsync("viewer");

        Assert.Empty(activities);
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
        context.AddRange(owner, actor, wishlist, hiddenItem, reservedItem);
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
            });
        context.ItemReservations.Add(new ItemReservation
        {
            User = actor,
            UserId = actorId,
            WishlistItem = reservedItem,
            IsAnonymous = true
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