using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using OpenWish.Application.Models;
using OpenWish.Application.Models.Configuration;
using OpenWish.Application.Services;
using OpenWish.Data;
using OpenWish.Data.Entities;
using OpenWish.Shared.Models;
using OpenWish.Shared.Services;
using Xunit;

namespace OpenWish.Application.Tests.Services;

public class FriendServiceRequestTests
{
    [Fact]
    public async Task SendFriendRequestAsync_RejectsRequestToSelf()
    {
        var databaseName = Guid.NewGuid().ToString();
        await using (var context = CreateContext(databaseName))
        {
            context.Users.Add(new ApplicationUser { Id = "requester", UserName = "requester" });
            await context.SaveChangesAsync();
        }

        using var provider = BuildServiceProvider(databaseName);
        var service = CreateFriendService(provider);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.SendFriendRequestAsync("requester", "requester"));

        await using var verificationContext = CreateContext(databaseName);
        Assert.Empty(verificationContext.FriendRequests);
    }

    [Fact]
    public async Task SendFriendRequestAsync_RejectsUnknownReceiver()
    {
        var databaseName = Guid.NewGuid().ToString();
        await using (var context = CreateContext(databaseName))
        {
            context.Users.Add(new ApplicationUser { Id = "requester", UserName = "requester" });
            await context.SaveChangesAsync();
        }

        using var provider = BuildServiceProvider(databaseName);
        var service = CreateFriendService(provider);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            service.SendFriendRequestAsync("requester", "missing"));

        await using var verificationContext = CreateContext(databaseName);
        Assert.Empty(verificationContext.FriendRequests);
    }

    [Fact]
    public async Task SendFriendRequestAsync_ThrowsWhenUsersAreAlreadyFriends()
    {
        var databaseName = Guid.NewGuid().ToString();
        await using (var seedContext = CreateContext(databaseName))
        {
            seedContext.Users.AddRange(
                new ApplicationUser { Id = "requester", UserName = "requester" },
                new ApplicationUser { Id = "receiver", UserName = "receiver" });
            seedContext.Friends.Add(new Friend { UserId = "requester", FriendUserId = "receiver" });
            await seedContext.SaveChangesAsync();
        }

        using var provider = BuildServiceProvider(databaseName);
        var service = CreateFriendService(provider);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.SendFriendRequestAsync("requester", "receiver"));
    }

    [Fact]
    public async Task SendFriendRequestAsync_ThrowsWhenAPendingRequestAlreadyExists()
    {
        var databaseName = Guid.NewGuid().ToString();
        await using (var seedContext = CreateContext(databaseName))
        {
            seedContext.Users.AddRange(
                new ApplicationUser { Id = "requester", UserName = "requester" },
                new ApplicationUser { Id = "receiver", UserName = "receiver" });
            seedContext.FriendRequests.Add(new FriendRequest
            {
                RequesterId = "requester",
                ReceiverId = "receiver",
                Status = "Pending"
            });
            await seedContext.SaveChangesAsync();
        }

        using var provider = BuildServiceProvider(databaseName);
        var service = CreateFriendService(provider);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.SendFriendRequestAsync("requester", "receiver"));
    }

    [Fact]
    public async Task SendFriendRequestAsync_SucceedsForNewRequest()
    {
        var databaseName = Guid.NewGuid().ToString();
        await using (var seedContext = CreateContext(databaseName))
        {
            seedContext.Users.AddRange(
                new ApplicationUser { Id = "requester", UserName = "requester" },
                new ApplicationUser { Id = "receiver", UserName = "receiver" });
            await seedContext.SaveChangesAsync();
        }

        using var provider = BuildServiceProvider(databaseName);
        var service = CreateFriendService(provider);

        var request = await service.SendFriendRequestAsync("requester", "receiver");

        Assert.Equal("requester", request.RequesterId);
        Assert.Equal("receiver", request.ReceiverId);
        Assert.Equal("Pending", request.Status);
    }

    [Fact]
    public async Task SendFriendInviteByEmailAsync_MatchesExistingUserIgnoringEmailCase()
    {
        var databaseName = Guid.NewGuid().ToString();
        await using (var context = CreateContext(databaseName))
        {
            context.Users.AddRange(
                new ApplicationUser { Id = "sender", UserName = "sender" },
                new ApplicationUser
                {
                    Id = "receiver",
                    UserName = "receiver",
                    Email = "Receiver@Example.com",
                    NormalizedEmail = "RECEIVER@EXAMPLE.COM"
                });
            await context.SaveChangesAsync();
        }

        using var provider = BuildServiceProvider(databaseName);
        var emailSender = new NoOpAppEmailSender();
        var service = CreateFriendService(provider, emailSender);

        Assert.True(await service.SendFriendInviteByEmailAsync("sender", "receiver@example.com"));

        await using var verificationContext = CreateContext(databaseName);
        Assert.Equal("receiver", (await verificationContext.FriendRequests.SingleAsync()).ReceiverId);
        Assert.Empty(verificationContext.PendingFriendInvites);
        Assert.Equal(0, emailSender.InviteCount);
    }

    [Fact]
    public async Task SendFriendInviteByEmailAsync_RejectsExistingFriendWithoutSendingEmail()
    {
        var databaseName = Guid.NewGuid().ToString();
        await using (var context = CreateContext(databaseName))
        {
            context.Users.AddRange(
                new ApplicationUser { Id = "sender", UserName = "sender" },
                new ApplicationUser { Id = "receiver", UserName = "receiver", Email = "receiver@example.com", NormalizedEmail = "RECEIVER@EXAMPLE.COM" });
            context.Friends.Add(new Friend { UserId = "sender", FriendUserId = "receiver" });
            await context.SaveChangesAsync();
        }

        using var provider = BuildServiceProvider(databaseName);
        var emailSender = new NoOpAppEmailSender();
        var service = CreateFriendService(provider, emailSender);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.SendFriendInviteByEmailAsync("sender", "receiver@example.com"));

        await using var verificationContext = CreateContext(databaseName);
        Assert.Empty(verificationContext.PendingFriendInvites);
        Assert.Empty(verificationContext.FriendRequests);
        Assert.Equal(0, emailSender.InviteCount);
    }

    [Fact]
    public async Task SendFriendInviteByEmailAsync_ClosesObsoleteInviteWhenRecipientHasRegistered()
    {
        var databaseName = Guid.NewGuid().ToString();
        await using (var context = CreateContext(databaseName))
        {
            context.Users.AddRange(
                new ApplicationUser { Id = "sender", UserName = "sender" },
                new ApplicationUser { Id = "receiver", UserName = "receiver", Email = "receiver@example.com", NormalizedEmail = "RECEIVER@EXAMPLE.COM" });
            context.PendingFriendInvites.Add(new PendingFriendInvite
            {
                SenderUserId = "sender",
                Email = "Receiver@Example.com"
            });
            await context.SaveChangesAsync();
        }

        using var provider = BuildServiceProvider(databaseName);
        var emailSender = new NoOpAppEmailSender();
        var service = CreateFriendService(provider, emailSender);

        Assert.True(await service.SendFriendInviteByEmailAsync("sender", "receiver@example.com"));

        await using var verificationContext = CreateContext(databaseName);
        Assert.Equal("receiver", (await verificationContext.FriendRequests.SingleAsync()).ReceiverId);
        var invite = await verificationContext.PendingFriendInvites.SingleAsync();
        Assert.Equal("Cancelled", invite.Status);
        Assert.True(invite.Deleted);
        Assert.Empty(await service.GetPendingFriendInvitesAsync("sender"));
        Assert.Equal(0, emailSender.InviteCount);
    }

    [Fact]
    public async Task SendFriendInviteByEmailAsync_DoesNotRecordInviteWhenDeliveryFails()
    {
        var databaseName = Guid.NewGuid().ToString();
        await using (var context = CreateContext(databaseName))
        {
            context.Users.Add(new ApplicationUser { Id = "sender", UserName = "sender" });
            await context.SaveChangesAsync();
        }

        using var provider = BuildServiceProvider(databaseName);
        var service = CreateFriendService(provider, new NoOpAppEmailSender { FailInvites = true });

        await Assert.ThrowsAsync<HttpRequestException>(() =>
            service.SendFriendInviteByEmailAsync("sender", "receiver@example.com"));

        await using var verificationContext = CreateContext(databaseName);
        Assert.Empty(verificationContext.PendingFriendInvites);
    }

    [Fact]
    public async Task SendFriendInvitesByEmailAsync_HandlesNullEntriesAndContinuesWithValidAddresses()
    {
        var databaseName = Guid.NewGuid().ToString();
        await using (var context = CreateContext(databaseName))
        {
            context.Users.Add(new ApplicationUser { Id = "sender", UserName = "sender" });
            await context.SaveChangesAsync();
        }

        using var provider = BuildServiceProvider(databaseName);
        var emailSender = new NoOpAppEmailSender();
        var service = CreateFriendService(provider, emailSender);

        var succeeded = await service.SendFriendInvitesByEmailAsync(
            "sender", [" Valid@Example.com ", null!, "valid@example.com", "next@example.com"]);

        Assert.False(succeeded);
        await using var verificationContext = CreateContext(databaseName);
        Assert.Equal(2, await verificationContext.PendingFriendInvites.CountAsync());
        Assert.Equal(2, emailSender.InviteCount);
    }

    [Fact]
    public async Task SendFriendInvitesByEmailAsync_PropagatesDeliveryFailures()
    {
        var databaseName = Guid.NewGuid().ToString();
        await using (var context = CreateContext(databaseName))
        {
            context.Users.Add(new ApplicationUser { Id = "sender", UserName = "sender" });
            await context.SaveChangesAsync();
        }

        using var provider = BuildServiceProvider(databaseName);
        var service = CreateFriendService(provider, new NoOpAppEmailSender { FailInvites = true });

        await Assert.ThrowsAsync<HttpRequestException>(() =>
            service.SendFriendInvitesByEmailAsync("sender", ["first@example.com", "second@example.com"]));

        await using var verificationContext = CreateContext(databaseName);
        Assert.Empty(verificationContext.PendingFriendInvites);
    }

    [Fact]
    public async Task SendFriendInviteByEmailAsync_ReusesPendingInviteIgnoringEmailCase()
    {
        var databaseName = Guid.NewGuid().ToString();
        await using (var context = CreateContext(databaseName))
        {
            context.Users.Add(new ApplicationUser { Id = "sender", UserName = "sender" });
            context.PendingFriendInvites.Add(new PendingFriendInvite
            {
                SenderUserId = "sender",
                Email = "Receiver@Example.com"
            });
            await context.SaveChangesAsync();
        }

        using var provider = BuildServiceProvider(databaseName);
        var emailSender = new NoOpAppEmailSender();
        var service = CreateFriendService(provider, emailSender);

        Assert.True(await service.SendFriendInviteByEmailAsync("sender", "receiver@example.com"));

        await using var verificationContext = CreateContext(databaseName);
        Assert.Single(await verificationContext.PendingFriendInvites.ToListAsync());
        Assert.Equal(1, emailSender.InviteCount);
    }

    [Fact]
    public async Task SendFriendInviteByEmailAsync_TrimsAddressBeforeSavingAndSending()
    {
        var databaseName = Guid.NewGuid().ToString();
        await using (var context = CreateContext(databaseName))
        {
            context.Users.Add(new ApplicationUser { Id = "sender", UserName = "sender" });
            await context.SaveChangesAsync();
        }

        using var provider = BuildServiceProvider(databaseName);
        var emailSender = new NoOpAppEmailSender();
        var service = CreateFriendService(provider, emailSender);

        Assert.True(await service.SendFriendInviteByEmailAsync("sender", "  receiver@example.com  "));

        await using var verificationContext = CreateContext(databaseName);
        Assert.Equal("receiver@example.com", (await verificationContext.PendingFriendInvites.SingleAsync()).Email);
        Assert.Equal("receiver@example.com", emailSender.LastInviteAddress);
        Assert.Contains("receiver%40example.com", emailSender.LastInviteLink);
    }

    [Theory]
    [InlineData("Accepted")]
    [InlineData("Cancelled")]
    public async Task CancelPendingFriendInviteAsync_DoesNotChangeCompletedInvite(string status)
    {
        var databaseName = Guid.NewGuid().ToString();
        await using (var context = CreateContext(databaseName))
        {
            context.Users.Add(new ApplicationUser { Id = "sender", UserName = "sender" });
            context.PendingFriendInvites.Add(new PendingFriendInvite
            {
                SenderUserId = "sender",
                Email = "receiver@example.com",
                Status = status
            });
            await context.SaveChangesAsync();
        }

        using var provider = BuildServiceProvider(databaseName);
        var service = CreateFriendService(provider);
        await using var verificationContext = CreateContext(databaseName);
        var inviteId = (await verificationContext.PendingFriendInvites.SingleAsync()).Id;

        Assert.False(await service.CancelPendingFriendInviteAsync(inviteId, "sender"));
        var invite = await verificationContext.PendingFriendInvites.SingleAsync();
        Assert.Equal(status, invite.Status);
        Assert.False(invite.Deleted);
    }

    [Fact]
    public async Task CreateFriendshipFromInviteAsync_ConsumesInviteWhenAlreadyFriends()
    {
        var databaseName = Guid.NewGuid().ToString();
        await using (var context = CreateContext(databaseName))
        {
            context.Users.AddRange(
                new ApplicationUser { Id = "sender", UserName = "sender" },
                new ApplicationUser { Id = "new-user", UserName = "new-user", Email = "Receiver@Example.com" });
            context.Friends.Add(new Friend { UserId = "sender", FriendUserId = "new-user" });
            context.PendingFriendInvites.Add(new PendingFriendInvite
            {
                SenderUserId = "sender",
                Email = "receiver@example.com"
            });
            await context.SaveChangesAsync();
        }

        using var provider = BuildServiceProvider(databaseName);
        var service = CreateFriendService(provider);

        Assert.True(await service.CreateFriendshipFromInviteAsync("new-user", "sender"));

        await using var verificationContext = CreateContext(databaseName);
        Assert.Equal("Accepted", (await verificationContext.PendingFriendInvites.SingleAsync()).Status);
        Assert.Single(await verificationContext.Friends.ToListAsync());
    }

    [Fact]
    public async Task CreateFriendshipFromInviteAsync_SavesFriendshipBeforeNotifying()
    {
        var databaseName = Guid.NewGuid().ToString();
        await using (var context = CreateContext(databaseName))
        {
            context.Users.AddRange(
                new ApplicationUser { Id = "sender", UserName = "sender" },
                new ApplicationUser { Id = "new-user", UserName = "new-user", Email = "receiver@example.com" });
            context.PendingFriendInvites.Add(new PendingFriendInvite
            {
                SenderUserId = "sender",
                Email = "receiver@example.com"
            });
            await context.SaveChangesAsync();
        }

        using var provider = BuildServiceProvider(databaseName);
        var service = CreateFriendService(provider, notificationService: new NoOpNotificationService(failAcceptance: true));

        await Assert.ThrowsAsync<HttpRequestException>(() =>
            service.CreateFriendshipFromInviteAsync("new-user", "sender"));

        await using var verificationContext = CreateContext(databaseName);
        Assert.Equal("Accepted", (await verificationContext.PendingFriendInvites.SingleAsync()).Status);
        Assert.Equal(2, await verificationContext.Friends.CountAsync(f => !f.Deleted));
    }

    [Fact]
    public async Task ResendPendingFriendInviteAsync_DoesNotAdvanceDateWhenDeliveryFails()
    {
        var databaseName = Guid.NewGuid().ToString();
        var originalDate = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        await using (var context = CreateContext(databaseName))
        {
            context.Users.Add(new ApplicationUser { Id = "sender", UserName = "sender" });
            context.PendingFriendInvites.Add(new PendingFriendInvite
            {
                SenderUserId = "sender",
                Email = "receiver@example.com",
                InviteDate = originalDate,
                UpdatedOn = originalDate
            });
            await context.SaveChangesAsync();
        }

        using var provider = BuildServiceProvider(databaseName);
        var service = CreateFriendService(provider, new NoOpAppEmailSender { FailInvites = true });
        await using var verificationContext = CreateContext(databaseName);
        var inviteId = (await verificationContext.PendingFriendInvites.SingleAsync()).Id;

        await Assert.ThrowsAsync<HttpRequestException>(() =>
            service.ResendPendingFriendInviteAsync(inviteId, "sender"));

        var invite = await verificationContext.PendingFriendInvites.SingleAsync();
        Assert.Equal(originalDate, invite.InviteDate);
        Assert.Equal(originalDate, invite.UpdatedOn);
    }

    private static ApplicationDbContext CreateContext(string databaseName) =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options);

    private static ServiceProvider BuildServiceProvider(string databaseName)
    {
        var services = new ServiceCollection();
        services.AddDbContext<ApplicationDbContext>(options => options.UseInMemoryDatabase(databaseName));
        return services.BuildServiceProvider();
    }

    private static FriendService CreateFriendService(
        ServiceProvider provider,
        IAppEmailSender? emailSender = null,
        INotificationService? notificationService = null)
    {
        var mapper = new MapperConfiguration(
            configuration => configuration.AddProfile<OpenWishProfile>(),
            NullLoggerFactory.Instance).CreateMapper();
        var options = Options.Create(new OpenWishSettings { BaseUri = "https://openwish.local" });

        return new FriendService(
            provider.GetRequiredService<IServiceScopeFactory>(),
            mapper,
            notificationService ?? new NoOpNotificationService(),
            emailSender ?? new NoOpAppEmailSender(),
            options,
            NullLogger<FriendService>.Instance);
    }

    private sealed class NoOpNotificationService(bool failAcceptance = false) : INotificationService
    {
        public Task<NotificationModel> CreateNotificationAsync(string userId, string message) =>
            Task.FromResult(new NotificationModel());

        public Task<NotificationModel> CreateNotificationAsync(
            string senderUserId,
            string targetUserId,
            string title,
            string message,
            string type,
            NotificationActionModel? action = null) =>
            failAcceptance && title == "Friend Invitation Accepted"
                ? Task.FromException<NotificationModel>(new HttpRequestException("Notification unavailable"))
                : Task.FromResult(new NotificationModel());

        public Task<bool> DeleteNotificationAsync(string notificationPublicId, string userId) =>
            Task.FromResult(true);

        public Task<int> GetUnreadNotificationCountAsync(string userId) => Task.FromResult(0);

        public Task<IEnumerable<NotificationModel>> GetUserNotificationsAsync(string userId, bool includeRead = false) =>
            Task.FromResult<IEnumerable<NotificationModel>>([]);

        public Task<bool> MarkAllNotificationsAsReadAsync(string userId) => Task.FromResult(true);

        public Task<bool> MarkNotificationAsReadAsync(string notificationPublicId, string userId) =>
            Task.FromResult(true);
    }

    private sealed class NoOpAppEmailSender : IAppEmailSender
    {
        public bool FailInvites { get; init; }
        public int InviteCount { get; private set; }
        public string? LastInviteAddress { get; private set; }
        public string? LastInviteLink { get; private set; }

        public Task SendConfirmationLinkAsync(string toEmail, string confirmationLink) => Task.CompletedTask;
        public Task SendPasswordResetCodeAsync(string toEmail, string resetCode) => Task.CompletedTask;
        public Task SendPasswordResetLinkAsync(string toEmail, string resetLink) => Task.CompletedTask;
        public Task SendFriendInviteEmailAsync(string toEmail, string inviterName, string inviteLink)
        {
            if (FailInvites)
            {
                throw new HttpRequestException("Email unavailable");
            }
            InviteCount++;
            LastInviteAddress = toEmail;
            LastInviteLink = inviteLink;
            return Task.CompletedTask;
        }
        public Task SendEventInviteEmailAsync(string toEmail, string inviterName, string eventName, string inviteLink) => Task.CompletedTask;
        public Task SendGiftExchangeDrawnEmailAsync(string toEmail, string eventName, string recipientName, string eventLink) => Task.CompletedTask;
        public Task SendGiftExchangeResetEmailAsync(string toEmail, string eventName, string eventLink) => Task.CompletedTask;
        public Task SendEmailAsync(string toEmail, string subject, string message) => Task.CompletedTask;
    }
}