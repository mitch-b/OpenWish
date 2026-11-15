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

public class WishlistArchivedIdeasTests
{
    private readonly IMapper _mapper = new MapperConfiguration(
        configuration => configuration.AddProfile<OpenWishProfile>(),
        NullLoggerFactory.Instance).CreateMapper();

    [Fact]
    public async Task Archive_IsOwnerOnlyAndRejectsMissingIdeas()
    {
        var (factory, list, items) = await SeedAsync();
        var service = CreateService(factory);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.SetWishlistItemArchivedByPublicIdAsync(
            list.PublicId, items[0].PublicId, true, "shopper"));
        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.SetWishlistItemArchivedByPublicIdAsync(
            list.PublicId, "missing", true, "owner"));

        await using var context = factory.CreateDbContext();
        Assert.Null((await context.WishlistItems.FindAsync(items[0].Id))!.ArchivedOn);
    }

    [Fact]
    public async Task ArchivedIdeas_LeaveTheActiveListButStayWithTheOwner()
    {
        var (factory, list, items) = await SeedAsync();
        var service = CreateService(factory);
        var saved = await service.SetWishlistItemArchivedByPublicIdAsync(list.PublicId, items[0].PublicId, true, "owner");
        Assert.NotNull(saved.ArchivedOn);

        var ownerView = await service.GetWishlistByPublicIdAsync(list.PublicId, "owner");
        Assert.Equal(["Second"], ownerView.Items.Select(i => i.Name));
        Assert.Equal(["Scarf"], ownerView.ArchivedItems.Select(i => i.Name));

        var shopperView = await service.GetWishlistByPublicIdAsync(list.PublicId, "shopper");
        Assert.Equal(["Second"], shopperView.Items.Select(i => i.Name));
        Assert.Empty(shopperView.ArchivedItems);

        var anonymousView = await service.GetWishlistByPublicIdAsync(list.PublicId);
        Assert.Equal(["Second"], anonymousView.Items.Select(i => i.Name));
        Assert.Empty(anonymousView.ArchivedItems);

        Assert.Equal(["Second"], (await service.GetWishlistItemsByPublicIdAsync(list.PublicId, "owner")).Select(i => i.Name));
        Assert.Equal(["Second"], (await service.GetWishlistItemsByPublicIdAsync(list.PublicId, "shopper")).Select(i => i.Name));
        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.GetWishlistItemByPublicIdAsync(list.PublicId, items[0].Id, "shopper"));
        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.ReserveItemAsync(list.Id, items[0].Id, "shopper"));
        Assert.Equal(["Second"], (await service.GetUserWishlistsAsync("owner")).Single().Items.Select(i => i.Name));
    }

    [Fact]
    public async Task Restore_ReturnsTheIdeaToTheActiveList()
    {
        var (factory, list, items) = await SeedAsync();
        var service = CreateService(factory);
        await service.SetWishlistItemArchivedByPublicIdAsync(list.PublicId, items[0].PublicId, true, "owner");

        var restored = await service.SetWishlistItemArchivedByPublicIdAsync(list.PublicId, items[0].PublicId, false, "owner");

        Assert.Null(restored.ArchivedOn);
        var shopperView = await service.GetWishlistByPublicIdAsync(list.PublicId, "shopper");
        Assert.Equal(["Scarf", "Second"], shopperView.Items.Select(i => i.Name).Order());
        Assert.Empty((await service.GetWishlistByPublicIdAsync(list.PublicId, "owner")).ArchivedItems);
    }

    [Fact]
    public async Task Archive_RejectsIdeasAlreadyReceivedAndReceivingArchivedIdeas()
    {
        var (factory, list, items) = await SeedAsync();
        var service = CreateService(factory);
        await service.SetWishlistItemReceivedByPublicIdAsync(
            list.PublicId, items[0].PublicId, new SetWishlistItemReceivedRequest { IsReceived = true }, "owner");
        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.SetWishlistItemArchivedByPublicIdAsync(
            list.PublicId, items[0].PublicId, true, "owner"));

        await service.SetWishlistItemArchivedByPublicIdAsync(list.PublicId, items[1].PublicId, true, "owner");
        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.SetWishlistItemReceivedByPublicIdAsync(
            list.PublicId, items[1].PublicId, new SetWishlistItemReceivedRequest { IsReceived = true }, "owner"));
    }

    [Fact]
    public async Task Archive_KeepsOrderingValidForTheRemainingIdeas()
    {
        var (factory, list, items) = await SeedAsync();
        var service = CreateService(factory);
        await service.SetWishlistItemArchivedByPublicIdAsync(list.PublicId, items[0].PublicId, true, "owner");

        await service.ReorderWishlistItemsByPublicIdAsync(
            list.PublicId, new ReorderWishlistItemsRequest { OrderedItemPublicIds = [items[1].PublicId] });
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