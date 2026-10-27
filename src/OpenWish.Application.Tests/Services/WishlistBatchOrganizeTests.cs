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

public class WishlistBatchOrganizeTests
{
    private readonly IMapper _mapper = new MapperConfiguration(
        configuration => configuration.AddProfile<OpenWishProfile>(),
        NullLoggerFactory.Instance).CreateMapper();

    [Fact]
    public async Task Transfer_MovesIdeasToTheEndOfAnotherOwnedList()
    {
        var (factory, source, target, items) = await SeedAsync();
        var result = await CreateService(factory).TransferWishlistItemsByPublicIdAsync(
            source.PublicId,
            new TransferWishlistItemsRequest { ItemPublicIds = [items[1].PublicId, items[0].PublicId], TargetWishlistPublicId = target.PublicId },
            "owner");

        Assert.Equal(2, result.AffectedCount);
        Assert.Equal(target.Name, result.TargetWishlistName);
        await using var context = factory.CreateDbContext();
        var moved = await context.WishlistItems.Where(i => i.WishlistId == target.Id).OrderBy(i => i.OrderIndex).ToListAsync();
        Assert.Equal(["Existing", "First", "Second"], moved.Select(i => i.Name));
        Assert.All(moved, i => Assert.False(i.IsPinned));
        Assert.Single(await context.WishlistItems.Where(i => i.WishlistId == source.Id && !i.Deleted).ToListAsync());
    }

    [Fact]
    public async Task Transfer_CopyLeavesOriginalsAndDuplicatesWithinTheSameList()
    {
        var (factory, source, target, items) = await SeedAsync();
        var service = CreateService(factory);
        await service.TransferWishlistItemsByPublicIdAsync(
            source.PublicId,
            new TransferWishlistItemsRequest { ItemPublicIds = [items[0].PublicId], TargetWishlistPublicId = target.PublicId, Copy = true },
            "owner");
        await service.TransferWishlistItemsByPublicIdAsync(
            source.PublicId,
            new TransferWishlistItemsRequest { ItemPublicIds = [items[0].PublicId], TargetWishlistPublicId = source.PublicId, Copy = true },
            "owner");

        await using var context = factory.CreateDbContext();
        Assert.Equal(4, await context.WishlistItems.CountAsync(i => i.WishlistId == source.Id));
        Assert.Contains(await context.WishlistItems.ToListAsync(), i => i.Name == "First (copy)" && i.WishlistId == source.Id);
        Assert.Contains(await context.WishlistItems.ToListAsync(), i => i.Name == "First" && i.WishlistId == target.Id);
    }

    [Fact]
    public async Task Transfer_RejectsNonOwnersForeignTargetsAndHiddenItems()
    {
        var (factory, source, target, items) = await SeedAsync();
        var service = CreateService(factory);
        await using (var context = factory.CreateDbContext())
        {
            context.Wishlists.Add(new Wishlist { Name = "Theirs", OwnerId = "other" });
            await context.SaveChangesAsync();
        }

        string theirsId()
        {
            using var context = factory.CreateDbContext();
            return context.Wishlists.Single(w => w.OwnerId == "other").PublicId;
        }

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.TransferWishlistItemsByPublicIdAsync(
            source.PublicId,
            new TransferWishlistItemsRequest { ItemPublicIds = [items[0].PublicId], TargetWishlistPublicId = target.PublicId },
            "other"));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.TransferWishlistItemsByPublicIdAsync(
            source.PublicId,
            new TransferWishlistItemsRequest { ItemPublicIds = [items[0].PublicId], TargetWishlistPublicId = theirsId() },
            "owner"));
        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.TransferWishlistItemsByPublicIdAsync(
            source.PublicId,
            new TransferWishlistItemsRequest { ItemPublicIds = [items[2].PublicId], TargetWishlistPublicId = target.PublicId },
            "owner"));
        await Assert.ThrowsAsync<ArgumentException>(() => service.TransferWishlistItemsByPublicIdAsync(
            source.PublicId,
            new TransferWishlistItemsRequest { ItemPublicIds = [items[0].PublicId], TargetWishlistPublicId = source.PublicId },
            "owner"));
        await Assert.ThrowsAsync<ArgumentException>(() => service.TransferWishlistItemsByPublicIdAsync(
            source.PublicId,
            new TransferWishlistItemsRequest { ItemPublicIds = [], TargetWishlistPublicId = target.PublicId },
            "owner"));

        await using var verification = factory.CreateDbContext();
        Assert.Equal(3, await verification.WishlistItems.CountAsync(i => i.WishlistId == source.Id));
    }

    [Fact]
    public async Task BulkUpdate_SetsPriorityAndMostWantedForSelectedIdeasOnly()
    {
        var (factory, source, _, items) = await SeedAsync();
        var service = CreateService(factory);
        await service.BulkUpdateWishlistItemsByPublicIdAsync(
            source.PublicId,
            new BulkUpdateWishlistItemsRequest { ItemPublicIds = [items[0].PublicId, items[1].PublicId], Action = BulkWishlistItemAction.SetPriority, Priority = 1 },
            "owner");
        await service.BulkUpdateWishlistItemsByPublicIdAsync(
            source.PublicId,
            new BulkUpdateWishlistItemsRequest { ItemPublicIds = [items[1].PublicId], Action = BulkWishlistItemAction.MarkMostWanted },
            "owner");

        await using var context = factory.CreateDbContext();
        var saved = await context.WishlistItems.Where(i => i.WishlistId == source.Id).ToDictionaryAsync(i => i.Name);
        Assert.Equal(1, saved["First"].Priority);
        Assert.True(saved["First"].IsPinned);
        Assert.True(saved["Second"].IsPinned);
        Assert.Null(saved["Surprise"].Priority);

        await service.BulkUpdateWishlistItemsByPublicIdAsync(
            source.PublicId,
            new BulkUpdateWishlistItemsRequest { ItemPublicIds = [items[1].PublicId], Action = BulkWishlistItemAction.ClearMostWanted },
            "owner");
        await using var after = factory.CreateDbContext();
        Assert.False((await after.WishlistItems.FindAsync(items[1].Id))!.IsPinned);
    }

    [Fact]
    public async Task BulkUpdate_RejectsNonOwnersBadPrioritiesAndHiddenItems()
    {
        var (factory, source, _, items) = await SeedAsync();
        var service = CreateService(factory);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.BulkUpdateWishlistItemsByPublicIdAsync(
            source.PublicId,
            new BulkUpdateWishlistItemsRequest { ItemPublicIds = [items[0].PublicId], Action = BulkWishlistItemAction.MarkMostWanted },
            "other"));
        await Assert.ThrowsAsync<ArgumentException>(() => service.BulkUpdateWishlistItemsByPublicIdAsync(
            source.PublicId,
            new BulkUpdateWishlistItemsRequest { ItemPublicIds = [items[0].PublicId], Action = BulkWishlistItemAction.SetPriority, Priority = 9 },
            "owner"));
        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.BulkUpdateWishlistItemsByPublicIdAsync(
            source.PublicId,
            new BulkUpdateWishlistItemsRequest { ItemPublicIds = [items[2].PublicId], Action = BulkWishlistItemAction.MarkMostWanted },
            "owner"));
    }

    private async Task<(TestDbContextFactory Factory, Wishlist Source, Wishlist Target, List<WishlistItem> Items)> SeedAsync()
    {
        var factory = new TestDbContextFactory(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
        var source = new Wishlist { Name = "Birthday", OwnerId = "owner" };
        var target = new Wishlist { Name = "Holiday", OwnerId = "owner" };
        var items = new List<WishlistItem>
        {
            new() { Name = "First", Wishlist = source, OrderIndex = 0, IsPinned = true, Price = 10 },
            new() { Name = "Second", Wishlist = source, OrderIndex = 1 },
            new() { Name = "Surprise", Wishlist = source, OrderIndex = 2, IsHiddenFromOwner = true },
            new() { Name = "Existing", Wishlist = target, OrderIndex = 0 }
        };
        await using var context = factory.CreateDbContext();
        context.WishlistItems.AddRange(items);
        await context.SaveChangesAsync();
        return (factory, source, target, items);
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