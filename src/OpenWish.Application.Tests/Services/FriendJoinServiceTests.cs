using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenWish.Application.Models.Configuration;
using OpenWish.Application.Services;
using OpenWish.Data;
using OpenWish.Data.Entities;
using OpenWish.Shared.Models;
using OpenWish.Shared.Services;
using Xunit;

namespace OpenWish.Application.Tests.Services;

public class FriendJoinServiceTests
{
    [Fact]
    public async Task CreateJoinCode_ReturnsUnambiguousCodeLinkAndQr()
    {
        var (service, _, _) = await CreateAsync();

        var model = await service.CreateJoinCodeAsync("alice", "https://wishes.example/");

        Assert.Equal(8, model.Code.Length);
        Assert.DoesNotContain(model.Code, ch => "01OIL".Contains(ch));
        Assert.Equal($"https://wishes.example/friends/join/{model.Code}", model.JoinUrl);
        Assert.StartsWith("<svg", model.QrSvg.TrimStart());
        Assert.InRange(model.ExpiresOn, DateTimeOffset.UtcNow.AddMinutes(14), DateTimeOffset.UtcNow.AddMinutes(16));
    }

    [Fact]
    public async Task CreateJoinCode_StoresOnlyAHash()
    {
        var (service, db, _) = await CreateAsync();

        var model = await service.CreateJoinCodeAsync("alice");

        await using var context = new ApplicationDbContext(db);
        var stored = Assert.Single(context.FriendJoinCodes);
        Assert.DoesNotContain(model.Code, stored.CodeHash);
        Assert.NotEqual(model.Code, stored.CodeHash);
    }

    [Fact]
    public async Task CreateJoinCode_RegeneratingInvalidatesPreviousCode()
    {
        var (service, _, _) = await CreateAsync();
        var first = await service.CreateJoinCodeAsync("alice");
        var second = await service.CreateJoinCodeAsync("alice");

        Assert.Equal(FriendJoinStatus.Invalid, (await service.PreviewJoinCodeAsync("bob", first.Code)).Status);
        Assert.Equal(FriendJoinStatus.Valid, (await service.PreviewJoinCodeAsync("bob", second.Code)).Status);
    }

    [Fact]
    public async Task CancelJoinCode_InvalidatesCode()
    {
        var (service, _, _) = await CreateAsync();
        var code = await service.CreateJoinCodeAsync("alice");

        Assert.True(await service.CancelJoinCodeAsync("alice"));
        Assert.False(await service.CancelJoinCodeAsync("alice"));
        Assert.Equal(FriendJoinStatus.Invalid, (await service.PreviewJoinCodeAsync("bob", code.Code)).Status);
    }

    [Fact]
    public async Task Preview_ShowsOnlyDisplayNameAndAcceptsLooseFormatting()
    {
        var (service, _, _) = await CreateAsync();
        var code = await service.CreateJoinCodeAsync("alice");

        var preview = await service.PreviewJoinCodeAsync("bob", $" {code.Code[..4].ToLowerInvariant()}-{code.Code[4..]} ");

        Assert.Equal(FriendJoinStatus.Valid, preview.Status);
        Assert.Equal("Alice", preview.DisplayName);
        Assert.DoesNotContain("example.com", System.Text.Json.JsonSerializer.Serialize(preview));
    }

    [Fact]
    public async Task Preview_FallsBackWhenUserNameIsAnEmail()
    {
        var (service, _, _) = await CreateAsync();
        var code = await service.CreateJoinCodeAsync("carol");

        var preview = await service.PreviewJoinCodeAsync("bob", code.Code);

        Assert.Equal("OpenWish member", preview.DisplayName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("ABC")]
    [InlineData("AAAAAAAA")]
    [InlineData("0000OOOO")]
    public async Task Preview_RejectsUnknownOrMalformedCodes(string code)
    {
        var (service, _, _) = await CreateAsync();

        Assert.Equal(FriendJoinStatus.Invalid, (await service.PreviewJoinCodeAsync("bob", code)).Status);
        Assert.Equal(FriendJoinStatus.Invalid, (await service.RedeemJoinCodeAsync("bob", code)).Status);
    }

    [Fact]
    public async Task Redeem_ExpiredCodeIsRejected()
    {
        var (service, db, _) = await CreateAsync();
        var code = await service.CreateJoinCodeAsync("alice");
        await using (var context = new ApplicationDbContext(db))
        {
            var entity = await context.FriendJoinCodes.SingleAsync();
            entity.ExpiresOn = DateTimeOffset.UtcNow.AddMinutes(-1);
            await context.SaveChangesAsync();
        }

        var result = await service.RedeemJoinCodeAsync("bob", code.Code);

        Assert.Equal(FriendJoinStatus.Invalid, result.Status);
        await using var verify = new ApplicationDbContext(db);
        Assert.Empty(verify.Friends);
    }

    [Fact]
    public async Task Redeem_CreatesMutualFriendshipNotifiesOwnerAndIsSingleUse()
    {
        var (service, db, notifications) = await CreateAsync();
        var code = await service.CreateJoinCodeAsync("alice");

        var result = await service.RedeemJoinCodeAsync("bob", code.Code);

        Assert.Equal(FriendJoinStatus.Connected, result.Status);
        Assert.Equal("Alice", result.DisplayName);
        await using (var verify = new ApplicationDbContext(db))
        {
            Assert.Equal(2, await verify.Friends.CountAsync(f => !f.Deleted));
            Assert.True(await verify.Friends.AnyAsync(f => f.UserId == "alice" && f.FriendUserId == "bob" && !f.Deleted));
            Assert.True(await verify.Friends.AnyAsync(f => f.UserId == "bob" && f.FriendUserId == "alice" && !f.Deleted));
        }

        var (userId, message) = Assert.Single(notifications.Sent);
        Assert.Equal("alice", userId);
        Assert.Contains("Bob", message);

        Assert.Equal(FriendJoinStatus.Invalid, (await service.RedeemJoinCodeAsync("dave", code.Code)).Status);
    }

    [Fact]
    public async Task Redeem_ReactivatesSoftDeletedFriendshipWithoutDuplicates()
    {
        var (service, db, _) = await CreateAsync();
        await using (var context = new ApplicationDbContext(db))
        {
            context.Friends.AddRange(
                new Friend { UserId = "alice", FriendUserId = "bob", Deleted = true },
                new Friend { UserId = "bob", FriendUserId = "alice", Deleted = true });
            await context.SaveChangesAsync();
        }

        var code = await service.CreateJoinCodeAsync("alice");
        await service.RedeemJoinCodeAsync("bob", code.Code);

        await using var verify = new ApplicationDbContext(db);
        Assert.Equal(2, await verify.Friends.CountAsync());
        Assert.Equal(2, await verify.Friends.CountAsync(f => !f.Deleted));
    }

    [Fact]
    public async Task Redeem_ResolvesPendingRequestBetweenThem()
    {
        var (service, db, _) = await CreateAsync();
        await using (var context = new ApplicationDbContext(db))
        {
            context.FriendRequests.Add(new FriendRequest { RequesterId = "bob", ReceiverId = "alice", Status = "Pending" });
            await context.SaveChangesAsync();
        }

        var code = await service.CreateJoinCodeAsync("alice");
        await service.RedeemJoinCodeAsync("bob", code.Code);

        await using var verify = new ApplicationDbContext(db);
        Assert.Equal("Accepted", (await verify.FriendRequests.SingleAsync()).Status);
    }

    [Fact]
    public async Task OwnCodeAndAlreadyFriendsAreNotRedeemed()
    {
        var (service, db, notifications) = await CreateAsync();
        var code = await service.CreateJoinCodeAsync("alice");

        Assert.Equal(FriendJoinStatus.OwnCode, (await service.RedeemJoinCodeAsync("alice", code.Code)).Status);

        await using (var context = new ApplicationDbContext(db))
        {
            context.Friends.AddRange(
                new Friend { UserId = "alice", FriendUserId = "bob" },
                new Friend { UserId = "bob", FriendUserId = "alice" });
            await context.SaveChangesAsync();
        }

        Assert.Equal(FriendJoinStatus.AlreadyFriends, (await service.RedeemJoinCodeAsync("bob", code.Code)).Status);
        await using var verify = new ApplicationDbContext(db);
        Assert.Equal(2, await verify.Friends.CountAsync());
        Assert.Empty(notifications.Sent);
    }

    [Fact]
    public async Task CreateJoinCode_UnknownUserThrows()
    {
        var (service, _, _) = await CreateAsync();

        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.CreateJoinCodeAsync("ghost"));
    }

    private static async Task<(FriendJoinService Service, DbContextOptions<ApplicationDbContext> Options, RecordingNotifications Notifications)> CreateAsync()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using (var context = new ApplicationDbContext(options))
        {
            context.Users.AddRange(
                new ApplicationUser { Id = "alice", UserName = "Alice", Email = "alice@example.com" },
                new ApplicationUser { Id = "bob", UserName = "Bob", Email = "bob@example.com" },
                new ApplicationUser { Id = "dave", UserName = "Dave", Email = "dave@example.com" },
                new ApplicationUser { Id = "carol", UserName = "carol@example.com", Email = "carol@example.com" });
            await context.SaveChangesAsync();
        }

        var notifications = new RecordingNotifications();
        var service = new FriendJoinService(
            new TestDbContextFactory(options),
            notifications,
            Options.Create(new OpenWishSettings { BaseUri = "https://openwish.local" }));
        return (service, options, notifications);
    }

    private sealed class TestDbContextFactory(DbContextOptions<ApplicationDbContext> options) : IDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext() => new(options);
    }

    private sealed class RecordingNotifications : INotificationService
    {
        public List<(string UserId, string Message)> Sent { get; } = [];

        public Task<NotificationModel> CreateNotificationAsync(string userId, string message)
        {
            Sent.Add((userId, message));
            return Task.FromResult(new NotificationModel());
        }

        public Task<IEnumerable<NotificationModel>> GetUserNotificationsAsync(string userId, bool includeRead = false) => throw new NotImplementedException();
        public Task<int> GetUnreadNotificationCountAsync(string userId) => throw new NotImplementedException();
        public Task<NotificationModel> CreateNotificationAsync(string senderUserId, string targetUserId, string title, string message, string type, NotificationActionModel? action = null) => throw new NotImplementedException();
        public Task<bool> MarkNotificationAsReadAsync(string notificationPublicId, string userId) => throw new NotImplementedException();
        public Task<bool> MarkAllNotificationsAsReadAsync(string userId) => throw new NotImplementedException();
        public Task<bool> DeleteNotificationAsync(string notificationPublicId, string userId) => throw new NotImplementedException();
    }
}