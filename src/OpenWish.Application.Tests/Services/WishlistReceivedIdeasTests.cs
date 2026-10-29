using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using OpenWish.Application.Models;
using OpenWish.Application.Services;
using OpenWish.Data;
using OpenWish.Data.Entities;
using OpenWish.Shared.Models;
using OpenWish.Shared.RequestModels;
using OpenWish.Shared.Services;
using Xunit;

namespace OpenWish.Application.Tests.Services;

public class WishlistReceivedIdeasTests
{
    private readonly IMapper _mapper = new MapperConfiguration(
        configuration => configuration.AddProfile<OpenWishProfile>(),
        NullLoggerFactory.Instance).CreateMapper();

    [Fact]
    public async Task MarkReceived_IsOwnerOnly()
    {
        var (factory, list, items) = await SeedAsync();
        var service = CreateService(factory);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.SetWishlistItemReceivedByPublicIdAsync(
            list.PublicId, items[0].PublicId, Received(), "shopper"));
        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.SetWishlistItemReceivedByPublicIdAsync(
            list.PublicId, "missing", Received(), "owner"));

        await using var context = factory.CreateDbContext();
        Assert.Null((await context.WishlistItems.FindAsync(items[0].Id))!.ReceivedOn);
    }

    [Fact]
    public async Task MarkReceived_OnEventListWaitsForTheEventDate()
    {
        var (factory, list, items) = await SeedAsync(eventDate: DateTimeOffset.UtcNow.AddDays(3));
        var service = CreateService(factory);

        var error = await Assert.ThrowsAsync<ArgumentException>(() => service.SetWishlistItemReceivedByPublicIdAsync(
            list.PublicId, items[0].PublicId, Received(), "owner"));
        Assert.Contains("after the event", error.Message);

        var (pastFactory, pastList, pastItems) = await SeedAsync(eventDate: DateTimeOffset.UtcNow.AddDays(-1));
        var saved = await CreateService(pastFactory).SetWishlistItemReceivedByPublicIdAsync(
            pastList.PublicId, pastItems[0].PublicId, Received(), "owner");
        Assert.NotNull(saved.ReceivedOn);
    }

    [Fact]
    public async Task MarkReceived_OnPlainListIsAllowedAnytime()
    {
        var (factory, list, items) = await SeedAsync();
        var saved = await CreateService(factory).SetWishlistItemReceivedByPublicIdAsync(
            list.PublicId, items[0].PublicId, Received(), "owner");

        Assert.NotNull(saved.ReceivedOn);
    }

    [Fact]
    public async Task ReceivedIdeas_LeaveTheActiveListForOwnerAndShoppersButStayWithTheOwner()
    {
        var (factory, list, items) = await SeedAsync();
        var service = CreateService(factory);
        await service.SetWishlistItemReceivedByPublicIdAsync(list.PublicId, items[0].PublicId, Received(), "owner");

        var ownerView = await service.GetWishlistByPublicIdAsync(list.PublicId, "owner");
        Assert.Equal(["Second"], ownerView.Items.Select(i => i.Name));
        Assert.Equal(["Scarf"], ownerView.ReceivedItems.Select(i => i.Name));

        var shopperView = await service.GetWishlistByPublicIdAsync(list.PublicId, "shopper");
        Assert.Equal(["Second"], shopperView.Items.Select(i => i.Name));
        Assert.Empty(shopperView.ReceivedItems);

        var anonymousView = await service.GetWishlistByPublicIdAsync(list.PublicId);
        Assert.Equal(["Second"], anonymousView.Items.Select(i => i.Name));
        Assert.Empty(anonymousView.ReceivedItems);

        Assert.Equal(["Second"], (await service.GetWishlistItemsByPublicIdAsync(list.PublicId, "owner")).Select(i => i.Name));
        Assert.Equal(["Second"], (await service.GetWishlistItemsByPublicIdAsync(list.PublicId, "shopper")).Select(i => i.Name));
        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.GetWishlistItemByPublicIdAsync(list.PublicId, items[0].Id, "shopper"));
        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.ReserveItemAsync(list.Id, items[0].Id, "shopper"));
        Assert.Equal(["Second"], (await service.GetUserWishlistsAsync("owner")).Single().Items.Select(i => i.Name));
    }

    [Fact]
    public async Task MoveBack_RestoresTheIdeaToTheActiveList()
    {
        var (factory, list, items) = await SeedAsync();
        var service = CreateService(factory);
        await service.SetWishlistItemReceivedByPublicIdAsync(list.PublicId, items[0].PublicId, Received(), "owner");
        await service.SetWishlistItemReceivedByPublicIdAsync(
            list.PublicId, items[0].PublicId, new SetWishlistItemReceivedRequest { IsReceived = false }, "owner");

        var ownerView = await service.GetWishlistByPublicIdAsync(list.PublicId, "owner");
        Assert.Equal(2, ownerView.Items.Count);
        Assert.Empty(ownerView.ReceivedItems);
    }

    [Fact]
    public async Task ThankYou_IsDeliveredOnlyToTheReserverNamingItemAndOwner()
    {
        var (factory, list, items) = await SeedAsync();
        await ReserveAsync(factory, items[0], "shopper");

        await CreateService(factory).SetWishlistItemReceivedByPublicIdAsync(
            list.PublicId, items[0].PublicId, Received("Loved the scarf!"), "owner");

        await using var context = factory.CreateDbContext();
        var notification = await context.Notifications.SingleAsync();
        Assert.Equal("shopper", notification.UserId);
        Assert.Contains("Scarf", notification.Message);
        Assert.Contains("owner", notification.Message);
        Assert.Contains("Loved the scarf!", notification.Message);
    }

    [Fact]
    public async Task ThankYou_WithoutAReservationOrMessageSendsNothingAndLooksIdentical()
    {
        var (factory, list, items) = await SeedAsync();
        await ReserveAsync(factory, items[1], "shopper");
        var service = CreateService(factory);

        var unreserved = await service.SetWishlistItemReceivedByPublicIdAsync(
            list.PublicId, items[0].PublicId, Received("Thanks!"), "owner");
        var reservedNoMessage = await service.SetWishlistItemReceivedByPublicIdAsync(
            list.PublicId, items[1].PublicId, Received(), "owner");

        await using var context = factory.CreateDbContext();
        Assert.Empty(context.Notifications);
        Assert.NotNull(unreserved.ReceivedOn);
        Assert.NotNull(reservedNoMessage.ReceivedOn);
        Assert.Empty(unreserved.Reservations);
        Assert.Empty(reservedNoMessage.Reservations);
    }

    [Fact]
    public async Task ThankYou_RejectsMessagesOverThreeHundredCharacters()
    {
        var (factory, list, items) = await SeedAsync();

        await Assert.ThrowsAsync<ArgumentException>(() => CreateService(factory).SetWishlistItemReceivedByPublicIdAsync(
            list.PublicId, items[0].PublicId, Received(new string('x', 301)), "owner"));
    }

    [Fact]
    public async Task GiftPlan_DropsReceivedGiftsUnlessTheShopperHadBoughtThemThenTheyAreHistory()
    {
        var (factory, list, items) = await SeedAsync();
        await ReserveAsync(factory, items[0], "shopper", purchased: false);
        await ReserveAsync(factory, items[1], "shopper", purchased: true);
        var service = CreateService(factory);
        var plan = new GiftPlanService(factory);
        Assert.Equal(2, (await plan.GetGiftPlanAsync("shopper")).ItemCount);

        await service.SetWishlistItemReceivedByPublicIdAsync(list.PublicId, items[0].PublicId, Received(), "owner");
        await service.SetWishlistItemReceivedByPublicIdAsync(list.PublicId, items[1].PublicId, Received(), "owner");

        var result = await plan.GetGiftPlanAsync("shopper");
        Assert.Equal(0, result.ItemCount);
        Assert.Equal(["Second"], result.History.SelectMany(r => r.Items).Select(i => i.ItemName));
    }

    private static SetWishlistItemReceivedRequest Received(string? thankYou = null) =>
        new() { IsReceived = true, ThankYouMessage = thankYou };

    private static async Task ReserveAsync(TestDbContextFactory factory, WishlistItem item, string userId, bool purchased = false)
    {
        await using var context = factory.CreateDbContext();
        context.ItemReservations.Add(new ItemReservation
        {
            WishlistItemId = item.Id,
            UserId = userId,
            ReservationDate = DateTimeOffset.UtcNow,
            PurchasedOn = purchased ? DateTimeOffset.UtcNow : null
        });
        await context.SaveChangesAsync();
    }

    private static async Task<(TestDbContextFactory Factory, Wishlist List, List<WishlistItem> Items)> SeedAsync(
        DateTimeOffset? eventDate = null)
    {
        var factory = new TestDbContextFactory(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
        var owner = new ApplicationUser { Id = "owner", UserName = "owner" };
        var shopper = new ApplicationUser { Id = "shopper", UserName = "shopper" };
        var list = new Wishlist { Name = "Birthday", OwnerId = owner.Id };
        if (eventDate is { } date)
        {
            list.Event = new Event { Name = "Party", Date = date, CreatedBy = owner };
        }

        var items = new List<WishlistItem>
        {
            new() { Name = "Scarf", Wishlist = list, OrderIndex = 0 },
            new() { Name = "Second", Wishlist = list, OrderIndex = 1 }
        };
        await using var context = factory.CreateDbContext();
        context.Users.AddRange(owner, shopper);
        context.Friends.Add(new Friend { UserId = "shopper", FriendUserId = "owner" });
        context.WishlistItems.AddRange(items);
        await context.SaveChangesAsync();
        return (factory, list, items);
    }

    private WishlistService CreateService(TestDbContextFactory factory) =>
        new(factory, _mapper, new NoOpActivityService(), NullLogger<WishlistService>.Instance);

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

        public Task<IEnumerable<ActivityLogModel>> GetUserActivityFeedAsync(string userId, int count = 20, int skip = 0) =>
            Task.FromResult<IEnumerable<ActivityLogModel>>([]);

        public Task<IEnumerable<ActivityLogModel>> GetFriendsActivityFeedAsync(string userId, int count = 20, int skip = 0) =>
            Task.FromResult<IEnumerable<ActivityLogModel>>([]);

        public Task<IEnumerable<ActivityLogModel>> GetWishlistActivityAsync(
            int wishlistId,
            string requestingUserId,
            int count = 20,
            int skip = 0) =>
            Task.FromResult<IEnumerable<ActivityLogModel>>([]);
    }
}