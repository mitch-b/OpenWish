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

public class WishlistItemCommentTests
{
    private readonly IMapper _mapper = new MapperConfiguration(
        configuration => configuration.AddProfile<OpenWishProfile>(),
        NullLoggerFactory.Instance).CreateMapper();

    [Fact]
    public async Task AddCommentToItemByPublicIdAsync_CreatesComment()
    {
        var factory = CreateFactory();
        string wishlistPublicId;
        int itemId;

        await using (var context = factory.CreateDbContext())
        {
            var owner = new ApplicationUser { Id = "owner", UserName = "owner" };
            context.Users.Add(owner);

            var wishlist = new Wishlist { Name = "Wishlist", OwnerId = "owner" };
            context.Wishlists.Add(wishlist);

            var item = new WishlistItem { Name = "Gift", Wishlist = wishlist };
            context.WishlistItems.Add(item);

            await context.SaveChangesAsync();
            wishlistPublicId = wishlist.PublicId;
            itemId = item.Id;
        }

        var service = CreateService(factory);
        var comment = await service.AddCommentToItemByPublicIdAsync(
            wishlistPublicId,
            itemId,
            "commenter",
            "Great gift!");

        Assert.NotNull(comment);
        Assert.Equal("Great gift!", comment.Text);
        Assert.Equal("commenter", comment.UserId);
    }

    [Fact]
    public async Task GetItemCommentsByPublicIdAsync_ReturnsAllComments()
    {
        var factory = CreateFactory();
        string wishlistPublicId;
        int itemId;

        await using (var context = factory.CreateDbContext())
        {
            var owner = new ApplicationUser { Id = "owner", UserName = "owner" };
            var commenter1 = new ApplicationUser { Id = "user1", UserName = "user1" };
            var commenter2 = new ApplicationUser { Id = "user2", UserName = "user2" };
            context.Users.AddRange(owner, commenter1, commenter2);

            var wishlist = new Wishlist { Name = "Wishlist", OwnerId = "owner" };
            context.Wishlists.Add(wishlist);

            var item = new WishlistItem { Name = "Gift", Wishlist = wishlist };
            context.WishlistItems.Add(item);

            context.ItemComments.AddRange(
                new ItemComment { WishlistItem = item, UserId = "user1", Text = "Comment 1", CreatedOn = DateTimeOffset.UtcNow },
                new ItemComment { WishlistItem = item, UserId = "user2", Text = "Comment 2", CreatedOn = DateTimeOffset.UtcNow.AddMinutes(-1) });

            await context.SaveChangesAsync();
            wishlistPublicId = wishlist.PublicId;
            itemId = item.Id;
        }

        var service = CreateService(factory);
        var comments = await service.GetItemCommentsByPublicIdAsync(wishlistPublicId, itemId);

        Assert.Equal(2, comments.Count());
    }

    [Fact]
    public async Task RemoveItemCommentAsync_SoftDeletesComment()
    {
        var factory = CreateFactory();
        int commentId;

        await using (var context = factory.CreateDbContext())
        {
            var owner = new ApplicationUser { Id = "owner", UserName = "owner" };
            var commenter = new ApplicationUser { Id = "commenter", UserName = "commenter" };
            context.Users.AddRange(owner, commenter);

            var wishlist = new Wishlist { Name = "Wishlist", OwnerId = "owner" };
            context.Wishlists.Add(wishlist);

            var item = new WishlistItem { Name = "Gift", Wishlist = wishlist };
            context.WishlistItems.Add(item);

            var comment = new ItemComment
            {
                WishlistItem = item,
                UserId = "commenter",
                Text = "Comment to remove",
                CreatedOn = DateTimeOffset.UtcNow
            };
            context.ItemComments.Add(comment);

            await context.SaveChangesAsync();
            commentId = comment.Id;
        }

        var service = CreateService(factory);
        var result = await service.RemoveItemCommentAsync(commentId, "commenter");

        Assert.True(result);

        await using var verifyContext = factory.CreateDbContext();
        var deleted = await verifyContext.ItemComments.IgnoreQueryFilters().FirstAsync(c => c.Id == commentId);
        Assert.True(deleted.Deleted);
    }

    [Fact]
    public async Task RemoveItemCommentAsync_RejectsAlreadyDeletedComment()
    {
        var factory = CreateFactory();
        int commentId;

        await using (var context = factory.CreateDbContext())
        {
            var owner = new ApplicationUser { Id = "owner", UserName = "owner" };
            var commenter = new ApplicationUser { Id = "commenter", UserName = "commenter" };
            context.Users.AddRange(owner, commenter);

            var wishlist = new Wishlist { Name = "Wishlist", OwnerId = "owner" };
            context.Wishlists.Add(wishlist);

            var item = new WishlistItem { Name = "Gift", Wishlist = wishlist };
            context.WishlistItems.Add(item);

            var comment = new ItemComment
            {
                WishlistItem = item,
                UserId = "commenter",
                Text = "Comment",
                CreatedOn = DateTimeOffset.UtcNow,
                Deleted = true
            };
            context.ItemComments.Add(comment);

            await context.SaveChangesAsync();
            commentId = comment.Id;
        }

        var service = CreateService(factory);
        var result = await service.RemoveItemCommentAsync(commentId, "commenter");

        Assert.False(result);
    }

    [Fact]
    public async Task RemoveItemCommentAsync_PermitsWishlistOwnerDeletion()
    {
        var factory = CreateFactory();
        int commentId;

        await using (var context = factory.CreateDbContext())
        {
            var owner = new ApplicationUser { Id = "owner", UserName = "owner" };
            var commenter = new ApplicationUser { Id = "commenter", UserName = "commenter" };
            context.Users.AddRange(owner, commenter);

            var wishlist = new Wishlist { Name = "Wishlist", OwnerId = "owner" };
            context.Wishlists.Add(wishlist);

            var item = new WishlistItem { Name = "Gift", Wishlist = wishlist };
            context.WishlistItems.Add(item);

            var comment = new ItemComment
            {
                WishlistItem = item,
                UserId = "commenter",
                Text = "Comment",
                CreatedOn = DateTimeOffset.UtcNow
            };
            context.ItemComments.Add(comment);

            await context.SaveChangesAsync();
            commentId = comment.Id;
        }

        var service = CreateService(factory);
        var result = await service.RemoveItemCommentAsync(commentId, "owner");

        Assert.True(result);

        await using var verifyContext = factory.CreateDbContext();
        var deleted = await verifyContext.ItemComments.IgnoreQueryFilters().FirstAsync(c => c.Id == commentId);
        Assert.True(deleted.Deleted);
    }

    [Fact]
    public async Task CommentPublicIdOverloads_AddListAndRemoveOnlyForAuthorizedUsers()
    {
        var factory = CreateFactory();
        string wishlistPublicId;
        string itemPublicId;

        await using (var context = factory.CreateDbContext())
        {
            context.Users.AddRange(
                new ApplicationUser { Id = "owner", UserName = "owner" },
                new ApplicationUser { Id = "commenter", UserName = "commenter" },
                new ApplicationUser { Id = "stranger", UserName = "stranger" });

            var wishlist = new Wishlist { Name = "Wishlist", OwnerId = "owner" };
            var item = new WishlistItem { Name = "Gift", Wishlist = wishlist };
            context.WishlistItems.Add(item);
            context.WishlistPermissions.Add(new WishlistPermission { Wishlist = wishlist, UserId = "commenter", PermissionType = "View" });

            await context.SaveChangesAsync();
            wishlistPublicId = wishlist.PublicId;
            itemPublicId = item.PublicId;
        }

        var service = CreateService(factory);
        var added = await service.AddCommentToItemByPublicIdAsync(wishlistPublicId, itemPublicId, "commenter", "Great pick");
        var comment = Assert.Single(await service.GetItemCommentsByPublicIdAsync(wishlistPublicId, itemPublicId, "commenter"));
        Assert.Equal(added.PublicId, comment.PublicId);

        Assert.False(await service.RemoveItemCommentByPublicIdAsync(comment.PublicId, "stranger"));
        Assert.False(await service.RemoveItemCommentByPublicIdAsync("missing", "commenter"));
        Assert.True(await service.RemoveItemCommentByPublicIdAsync(comment.PublicId, "commenter"));
        Assert.Empty(await service.GetItemCommentsByPublicIdAsync(wishlistPublicId, itemPublicId, "commenter"));
        Assert.False(await service.RemoveItemCommentByPublicIdAsync(comment.PublicId, "commenter"));
    }

    [Fact]
    public async Task ItemPublicIdOperations_TreatItemsTheViewerCannotSeeAsMissing()
    {
        var factory = CreateFactory();
        string wishlistPublicId;
        string privatePublicId;
        string surprisePublicId;
        string visiblePublicId;

        await using (var context = factory.CreateDbContext())
        {
            context.Users.AddRange(
                new ApplicationUser { Id = "owner", UserName = "owner" },
                new ApplicationUser { Id = "shopper", UserName = "shopper" },
                new ApplicationUser { Id = "stranger", UserName = "stranger" });
            var wishlist = new Wishlist { Name = "Wishlist", OwnerId = "owner" };
            var privateItem = new WishlistItem { Name = "Private", Wishlist = wishlist, IsPrivate = true };
            var surprise = new WishlistItem { Name = "Surprise", Wishlist = wishlist, IsHiddenFromOwner = true };
            var visible = new WishlistItem { Name = "Visible", Wishlist = wishlist };
            context.WishlistItems.AddRange(privateItem, surprise, visible);
            context.WishlistPermissions.Add(new WishlistPermission { Wishlist = wishlist, UserId = "shopper", PermissionType = "View" });
            await context.SaveChangesAsync();
            wishlistPublicId = wishlist.PublicId;
            privatePublicId = privateItem.PublicId;
            visiblePublicId = visible.PublicId;
            surprisePublicId = surprise.PublicId;
        }

        var service = CreateService(factory);
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            service.GetItemCommentsByPublicIdAsync(wishlistPublicId, privatePublicId, "shopper"));
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            service.AddCommentToItemByPublicIdAsync(wishlistPublicId, privatePublicId, "shopper", "Hi"));
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            service.ReserveItemByPublicIdAsync(wishlistPublicId, privatePublicId, "shopper"));
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            service.GetItemReservationByPublicIdAsync(wishlistPublicId, privatePublicId, "shopper"));
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            service.GetItemCommentsByPublicIdAsync(wishlistPublicId, surprisePublicId, "owner"));

        Assert.Empty(await service.GetItemCommentsByPublicIdAsync(wishlistPublicId, privatePublicId, "owner"));
        Assert.True(await service.ReserveItemByPublicIdAsync(wishlistPublicId, surprisePublicId, "shopper"));

        // A caller without list access (for example after access is revoked) can't reach any item.
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            service.GetItemCommentsByPublicIdAsync(wishlistPublicId, visiblePublicId, "stranger"));
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            service.AddCommentToItemByPublicIdAsync(wishlistPublicId, visiblePublicId, "stranger", "Hi"));
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            service.ReserveItemByPublicIdAsync(wishlistPublicId, visiblePublicId, "stranger"));
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            service.GetItemReservationByPublicIdAsync(wishlistPublicId, visiblePublicId, "stranger"));
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