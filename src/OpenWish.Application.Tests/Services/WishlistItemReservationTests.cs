using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using OpenWish.Application.Models;
using OpenWish.Application.Services;
using OpenWish.Data;
using OpenWish.Data.Entities;
using OpenWish.Shared.Models;
using OpenWish.Shared.Services;
using Xunit;

namespace OpenWish.Application.Tests.Services;

public class WishlistItemReservationTests
{
    private readonly IMapper _mapper = new MapperConfiguration(
        configuration => configuration.AddProfile<OpenWishProfile>(),
        NullLoggerFactory.Instance).CreateMapper();

    [Fact]
    public async Task ReserveItemByPublicIdAsync_CreatesReservation()
    {
        var factory = CreateFactory();
        string wishlistPublicId;
        int itemId;

        await using (var context = factory.CreateDbContext())
        {
            var owner = new ApplicationUser { Id = "owner", UserName = "owner" };
            var reserver = new ApplicationUser { Id = "reserver", UserName = "reserver" };
            context.Users.AddRange(owner, reserver);

            var wishlist = new Wishlist { Name = "Wishlist", OwnerId = "owner", IsPrivate = false };
            context.Wishlists.Add(wishlist);

            var item = new WishlistItem { Name = "Gift", Wishlist = wishlist };
            context.WishlistItems.Add(item);

            await context.SaveChangesAsync();
            wishlistPublicId = wishlist.PublicId;
            itemId = item.Id;
        }

        var service = CreateService(factory);
        var result = await service.ReserveItemByPublicIdAsync(wishlistPublicId, itemId, "reserver");

        Assert.True(result);

        await using var verifyContext = factory.CreateDbContext();
        var reservation = await verifyContext.ItemReservations
            .FirstAsync(r => r.WishlistItemId == itemId && r.UserId == "reserver");
        Assert.NotNull(reservation);
    }

    [Fact]
    public async Task ReserveItemByPublicIdAsync_AllowsOwnerReservation()
    {
        var factory = CreateFactory();
        string wishlistPublicId;
        int itemId;

        await using (var context = factory.CreateDbContext())
        {
            var owner = new ApplicationUser { Id = "owner", UserName = "owner" };
            context.Users.Add(owner);

            var wishlist = new Wishlist { Name = "Wishlist", OwnerId = "owner", IsPrivate = false };
            context.Wishlists.Add(wishlist);

            var item = new WishlistItem { Name = "Gift", Wishlist = wishlist };
            context.WishlistItems.Add(item);

            await context.SaveChangesAsync();
            wishlistPublicId = wishlist.PublicId;
            itemId = item.Id;
        }

        var service = CreateService(factory);
        var result = await service.ReserveItemByPublicIdAsync(wishlistPublicId, itemId, "owner");

        Assert.True(result);
    }

    [Fact]
    public async Task ReserveItemByPublicIdAsync_PreventsDoubleReservation()
    {
        var factory = CreateFactory();
        string wishlistPublicId;
        int itemId;

        await using (var context = factory.CreateDbContext())
        {
            var owner = new ApplicationUser { Id = "owner", UserName = "owner" };
            var reserver = new ApplicationUser { Id = "reserver", UserName = "reserver" };
            context.Users.AddRange(owner, reserver);

            var wishlist = new Wishlist { Name = "Wishlist", OwnerId = "owner", IsPrivate = false };
            context.Wishlists.Add(wishlist);

            var item = new WishlistItem { Name = "Gift", Wishlist = wishlist };
            context.WishlistItems.Add(item);

            await context.SaveChangesAsync();
            wishlistPublicId = wishlist.PublicId;
            itemId = item.Id;
        }

        var service = CreateService(factory);
        var firstReservation = await service.ReserveItemByPublicIdAsync(wishlistPublicId, itemId, "reserver");
        var secondReservation = await service.ReserveItemByPublicIdAsync(wishlistPublicId, itemId, "other");

        Assert.True(firstReservation);
        Assert.False(secondReservation);
    }

    [Fact]
    public async Task CancelReservationByPublicIdAsync_SoftDeletesReservation()
    {
        var factory = CreateFactory();
        string wishlistPublicId;
        int itemId;
        int reservationId;

        await using (var context = factory.CreateDbContext())
        {
            var owner = new ApplicationUser { Id = "owner", UserName = "owner" };
            var reserver = new ApplicationUser { Id = "reserver", UserName = "reserver" };
            context.Users.AddRange(owner, reserver);

            var wishlist = new Wishlist { Name = "Wishlist", OwnerId = "owner", IsPrivate = false };
            context.Wishlists.Add(wishlist);

            var item = new WishlistItem { Name = "Gift", Wishlist = wishlist };
            context.WishlistItems.Add(item);

            var reservation = new ItemReservation
            {
                WishlistItem = item,
                UserId = "reserver",
                IsAnonymous = false
            };
            context.ItemReservations.Add(reservation);

            await context.SaveChangesAsync();
            wishlistPublicId = wishlist.PublicId;
            itemId = item.Id;
            reservationId = reservation.Id;
        }

        var service = CreateService(factory);
        var result = await service.CancelReservationByPublicIdAsync(wishlistPublicId, itemId, "reserver");

        Assert.True(result);

        await using var verifyContext = factory.CreateDbContext();
        var deleted = await verifyContext.ItemReservations.IgnoreQueryFilters().FirstAsync(r => r.Id == reservationId);
        Assert.True(deleted.Deleted);
    }

    [Fact]
    public async Task CancelReservationByPublicIdAsync_RejectsUnauthorizedUser()
    {
        var factory = CreateFactory();
        string wishlistPublicId;
        int itemId;

        await using (var context = factory.CreateDbContext())
        {
            var owner = new ApplicationUser { Id = "owner", UserName = "owner" };
            var reserver = new ApplicationUser { Id = "reserver", UserName = "reserver" };
            context.Users.AddRange(owner, reserver);

            var wishlist = new Wishlist { Name = "Wishlist", OwnerId = "owner", IsPrivate = false };
            context.Wishlists.Add(wishlist);

            var item = new WishlistItem { Name = "Gift", Wishlist = wishlist };
            context.WishlistItems.Add(item);

            var reservation = new ItemReservation
            {
                WishlistItem = item,
                UserId = "reserver",
                IsAnonymous = false
            };
            context.ItemReservations.Add(reservation);

            await context.SaveChangesAsync();
            wishlistPublicId = wishlist.PublicId;
            itemId = item.Id;
        }

        var service = CreateService(factory);
        var result = await service.CancelReservationByPublicIdAsync(wishlistPublicId, itemId, "otheruser");

        Assert.False(result);
    }

    [Fact]
    public async Task ReserveItemByPublicIdAsync_AnonymousReservation()
    {
        var factory = CreateFactory();
        string wishlistPublicId;
        int itemId;

        await using (var context = factory.CreateDbContext())
        {
            var owner = new ApplicationUser { Id = "owner", UserName = "owner" };
            var reserver = new ApplicationUser { Id = "reserver", UserName = "reserver" };
            context.Users.AddRange(owner, reserver);

            var wishlist = new Wishlist { Name = "Wishlist", OwnerId = "owner", IsPrivate = false };
            context.Wishlists.Add(wishlist);

            var item = new WishlistItem { Name = "Gift", Wishlist = wishlist };
            context.WishlistItems.Add(item);

            await context.SaveChangesAsync();
            wishlistPublicId = wishlist.PublicId;
            itemId = item.Id;
        }

        var service = CreateService(factory);
        var result = await service.ReserveItemByPublicIdAsync(wishlistPublicId, itemId, "reserver", isAnonymous: true);

        Assert.True(result);

        await using var verifyContext = factory.CreateDbContext();
        var reservation = await verifyContext.ItemReservations
            .FirstAsync(r => r.WishlistItemId == itemId && r.UserId == "reserver");
        Assert.True(reservation.IsAnonymous);
    }

    private static TestDbContextFactory CreateFactory() =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static WishlistService CreateService(TestDbContextFactory factory) =>
        new(factory, new MapperConfiguration(
            configuration => configuration.AddProfile<OpenWishProfile>(),
            NullLoggerFactory.Instance).CreateMapper(),
            new NoOpActivityService(),
            NullLogger<WishlistService>.Instance);

    private sealed class TestDbContextFactory(DbContextOptions<ApplicationDbContext> options)
        : IDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext() => new(options);
    }

    private sealed class NoOpActivityService : IActivityService
    {
        public Task<ActivityLogModel> LogActivityAsync(
            string userId,
            string activityType,
            string description,
            int? wishlistId = null,
            int? wishlistItemId = null) =>
            Task.FromResult(new ActivityLogModel());

        public Task<IEnumerable<ActivityLogModel>> GetUserActivityFeedAsync(
            string userId,
            int count = 20,
            int skip = 0) =>
            Task.FromResult<IEnumerable<ActivityLogModel>>([]);

        public Task<IEnumerable<ActivityLogModel>> GetFriendsActivityFeedAsync(
            string userId,
            int count = 20,
            int skip = 0) =>
            Task.FromResult<IEnumerable<ActivityLogModel>>([]);

        public Task<IEnumerable<ActivityLogModel>> GetWishlistActivityAsync(
            int wishlistId,
            string requestingUserId,
            int count = 20,
            int skip = 0) =>
            Task.FromResult<IEnumerable<ActivityLogModel>>([]);
    }
}