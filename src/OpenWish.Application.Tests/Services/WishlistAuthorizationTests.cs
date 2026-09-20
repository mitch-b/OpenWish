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

public class WishlistAuthorizationTests
{
    private readonly IMapper _mapper = new MapperConfiguration(
        configuration => configuration.AddProfile<OpenWishProfile>(),
        NullLoggerFactory.Instance).CreateMapper();

    [Theory]
    [InlineData("View", false)]
    [InlineData("Edit", true)]
    [InlineData("Admin", true)]
    public async Task CanUserEditWishlistAsync_EnforcesPermissionLevel(string permissionType, bool expected)
    {
        var factory = CreateFactory();
        await using (var context = factory.CreateDbContext())
        {
            var wishlist = CreateWishlist();
            context.Wishlists.Add(wishlist);
            context.WishlistPermissions.Add(new WishlistPermission
            {
                Wishlist = wishlist,
                UserId = "collaborator",
                PermissionType = permissionType
            });
            await context.SaveChangesAsync();
        }

        var service = new WishlistService(
            factory,
            _mapper,
            new NoOpActivityService(),
            NullLogger<WishlistService>.Instance);

        var canEdit = await service.CanUserEditWishlistAsync(1, "collaborator");

        Assert.Equal(expected, canEdit);
    }

    [Fact]
    public async Task UpdateWishlistByPublicIdAsync_RejectsNonOwner()
    {
        var factory = CreateFactory();
        var wishlist = CreateWishlist();
        await using (var context = factory.CreateDbContext())
        {
            context.Wishlists.Add(wishlist);
            await context.SaveChangesAsync();
        }

        var service = new WishlistService(
            factory,
            _mapper,
            new NoOpActivityService(),
            NullLogger<WishlistService>.Instance);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.UpdateWishlistByPublicIdAsync(
                wishlist.PublicId,
                new WishlistModel { Name = "Exposed", IsPrivate = false },
                "collaborator"));

        await using var verificationContext = factory.CreateDbContext();
        var unchanged = await verificationContext.Wishlists.SingleAsync();
        Assert.Equal("Private wishlist", unchanged.Name);
        Assert.True(unchanged.IsPrivate);
    }

    [Fact]
    public async Task AddItemToWishlistByPublicIdAsync_ReusesMatchingRequestKey()
    {
        var factory = CreateFactory();
        var wishlist = CreateWishlist();
        await using (var context = factory.CreateDbContext())
        {
            context.Wishlists.Add(wishlist);
            await context.SaveChangesAsync();
        }

        var service = CreateService(factory);
        var item = new WishlistItemModel
        {
            PublicId = Guid.NewGuid().ToString(),
            Name = "Tea infuser",
            Description = "Fine mesh",
            IsPrivate = true
        };

        var firstResult = await service.AddItemToWishlistByPublicIdAsync(wishlist.PublicId, item);
        var retryResult = await service.AddItemToWishlistByPublicIdAsync(wishlist.PublicId, item);

        Assert.Equal(firstResult.Id, retryResult.Id);
        await using var verificationContext = factory.CreateDbContext();
        Assert.Single(await verificationContext.WishlistItems.ToListAsync());
    }

    [Fact]
    public async Task AddItemToWishlistByPublicIdAsync_RetryReturnsItemAfterPostCommitFailure()
    {
        var factory = CreateFactory();
        var wishlist = CreateWishlist();
        await using (var context = factory.CreateDbContext())
        {
            context.Wishlists.Add(wishlist);
            await context.SaveChangesAsync();
        }

        var item = new WishlistItemModel
        {
            PublicId = Guid.NewGuid().ToString(),
            Name = "Tea infuser"
        };
        var failingService = new WishlistService(
            factory,
            _mapper,
            new ThrowingActivityService(),
            NullLogger<WishlistService>.Instance);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            failingService.AddItemToWishlistByPublicIdAsync(wishlist.PublicId, item));

        var retryResult = await CreateService(factory)
            .AddItemToWishlistByPublicIdAsync(wishlist.PublicId, item);

        Assert.Equal(item.PublicId, retryResult.PublicId);
        await using var verificationContext = factory.CreateDbContext();
        Assert.Single(await verificationContext.WishlistItems.ToListAsync());
    }

    [Fact]
    public async Task AddItemToWishlistByPublicIdAsync_RejectsRequestKeyReuseWithDifferentItem()
    {
        var factory = CreateFactory();
        var wishlist = CreateWishlist();
        await using (var context = factory.CreateDbContext())
        {
            context.Wishlists.Add(wishlist);
            await context.SaveChangesAsync();
        }

        var service = CreateService(factory);
        var requestKey = Guid.NewGuid().ToString();
        await service.AddItemToWishlistByPublicIdAsync(
            wishlist.PublicId,
            new WishlistItemModel { PublicId = requestKey, Name = "Tea infuser" });

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            service.AddItemToWishlistByPublicIdAsync(
                wishlist.PublicId,
                new WishlistItemModel { PublicId = requestKey, Name = "Different item" }));

        Assert.Equal("The item request key is already in use.", exception.Message);
    }

    [Fact]
    public async Task AddItemToWishlistByPublicIdAsync_RejectsRequestKeyReuseWithDifferentImage()
    {
        var factory = CreateFactory();
        var wishlist = CreateWishlist();
        await using (var context = factory.CreateDbContext())
        {
            context.Wishlists.Add(wishlist);
            await context.SaveChangesAsync();
        }

        var service = CreateService(factory);
        var requestKey = Guid.NewGuid().ToString();
        await service.AddItemToWishlistByPublicIdAsync(
            wishlist.PublicId,
            new WishlistItemModel
            {
                PublicId = requestKey,
                Name = "Tea infuser",
                Image = "https://example.com/first.jpg"
            });

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.AddItemToWishlistByPublicIdAsync(
                wishlist.PublicId,
                new WishlistItemModel
                {
                    PublicId = requestKey,
                    Name = "Tea infuser",
                    Image = "https://example.com/second.jpg"
                }));
    }

    [Fact]
    public async Task AddItemToWishlistByPublicIdAsync_RetryUsesOriginalRequestAfterItemUpdate()
    {
        var factory = CreateFactory();
        var wishlist = CreateWishlist();
        await using (var context = factory.CreateDbContext())
        {
            context.Wishlists.Add(wishlist);
            await context.SaveChangesAsync();
        }

        var service = CreateService(factory);
        var originalRequest = new WishlistItemModel
        {
            PublicId = Guid.NewGuid().ToString(),
            Name = "Tea infuser"
        };
        var createdItem = await service.AddItemToWishlistByPublicIdAsync(wishlist.PublicId, originalRequest);
        await service.UpdateWishlistItemAsync(
            wishlist.Id,
            createdItem.Id,
            new WishlistItemModel { Id = createdItem.Id, Name = "Updated tea infuser" });

        var retryResult = await service.AddItemToWishlistByPublicIdAsync(wishlist.PublicId, originalRequest);

        Assert.Equal(createdItem.Id, retryResult.Id);
        Assert.Equal("Tea infuser", retryResult.Name);
        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.AddItemToWishlistByPublicIdAsync(
                wishlist.PublicId,
                new WishlistItemModel
                {
                    PublicId = originalRequest.PublicId,
                    Name = "Updated tea infuser"
                }));
    }

    [Fact]
    public async Task AddItemToWishlistByPublicIdAsync_RejectsRetryAfterItemDeletion()
    {
        var factory = CreateFactory();
        var wishlist = CreateWishlist();
        await using (var context = factory.CreateDbContext())
        {
            context.Wishlists.Add(wishlist);
            await context.SaveChangesAsync();
        }

        var service = CreateService(factory);
        var request = new WishlistItemModel
        {
            PublicId = Guid.NewGuid().ToString(),
            Name = "Tea infuser"
        };
        var createdItem = await service.AddItemToWishlistByPublicIdAsync(wishlist.PublicId, request);
        await using (var context = factory.CreateDbContext())
        {
            var item = await context.WishlistItems.SingleAsync(existingItem => existingItem.Id == createdItem.Id);
            item.Deleted = true;
            await context.SaveChangesAsync();
        }

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            service.AddItemToWishlistByPublicIdAsync(wishlist.PublicId, request));

        Assert.Equal("The item request key is already in use.", exception.Message);
    }

    [Fact]
    public async Task AddItemToWishlistByPublicIdAsync_RejectsUnsupportedPricePrecision()
    {
        var factory = CreateFactory();
        var wishlist = CreateWishlist();
        await using (var context = factory.CreateDbContext())
        {
            context.Wishlists.Add(wishlist);
            await context.SaveChangesAsync();
        }

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            CreateService(factory).AddItemToWishlistByPublicIdAsync(
                wishlist.PublicId,
                new WishlistItemModel { Name = "Tea infuser", Price = 12.345m }));

        Assert.Contains("two decimal places", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task UpdateWishlistItemAsync_PreservesPublicId()
    {
        var factory = CreateFactory();
        var wishlist = CreateWishlist();
        var item = new WishlistItem { Name = "Tea infuser", Wishlist = wishlist };
        await using (var context = factory.CreateDbContext())
        {
            context.WishlistItems.Add(item);
            await context.SaveChangesAsync();
        }

        var originalPublicId = item.PublicId;
        var result = await CreateService(factory).UpdateWishlistItemAsync(
            wishlist.Id,
            item.Id,
            new WishlistItemModel
            {
                Id = item.Id,
                PublicId = Guid.NewGuid().ToString(),
                Name = "Updated tea infuser"
            });

        Assert.Equal(originalPublicId, result.PublicId);
        await using var verificationContext = factory.CreateDbContext();
        Assert.Equal(originalPublicId, (await verificationContext.WishlistItems.SingleAsync()).PublicId);
    }

    [Fact]
    public async Task RemoveItemFromWishlistAsync_ReconcilesAlreadyDeletedItem()
    {
        var factory = CreateFactory();
        var wishlist = CreateWishlist();
        var item = new WishlistItem
        {
            Name = "Tea infuser",
            Wishlist = wishlist,
            Deleted = true
        };
        await using (var context = factory.CreateDbContext())
        {
            context.WishlistItems.Add(item);
            await context.SaveChangesAsync();
        }

        var result = await CreateService(factory).RemoveItemFromWishlistAsync(wishlist.Id, item.Id);

        Assert.True(result);
        await using var verificationContext = factory.CreateDbContext();
        Assert.True((await verificationContext.WishlistItems.SingleAsync()).Deleted);
    }

    private static TestDbContextFactory CreateFactory() =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static Wishlist CreateWishlist() => new()
    {
        Name = "Private wishlist",
        OwnerId = "owner",
        IsPrivate = true,
        IsCollaborative = true
    };

    private WishlistService CreateService(TestDbContextFactory factory) =>
        new(
            factory,
            _mapper,
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

    private sealed class ThrowingActivityService : IActivityService
    {
        public Task<ActivityLogModel> LogActivityAsync(
            string userId,
            string activityType,
            string description,
            int? wishlistId = null,
            int? wishlistItemId = null) =>
            throw new InvalidOperationException("Simulated post-commit failure.");

        public Task<IEnumerable<ActivityLogModel>> GetUserActivityFeedAsync(
            string userId,
            int count = 20,
            int skip = 0) =>
            throw new NotSupportedException();

        public Task<IEnumerable<ActivityLogModel>> GetFriendsActivityFeedAsync(
            string userId,
            int count = 20,
            int skip = 0) =>
            throw new NotSupportedException();

        public Task<IEnumerable<ActivityLogModel>> GetWishlistActivityAsync(
            int wishlistId,
            string requestingUserId,
            int count = 20,
            int skip = 0) =>
            throw new NotSupportedException();
    }
}