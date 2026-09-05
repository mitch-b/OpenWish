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

        var service = new WishlistService(factory, _mapper, new NoOpActivityService());

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

        var service = new WishlistService(factory, _mapper, new NoOpActivityService());

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