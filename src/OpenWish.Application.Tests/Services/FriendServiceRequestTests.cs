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

    private static FriendService CreateFriendService(ServiceProvider provider)
    {
        var mapper = new MapperConfiguration(
            configuration => configuration.AddProfile<OpenWishProfile>(),
            NullLoggerFactory.Instance).CreateMapper();
        var options = Options.Create(new OpenWishSettings { BaseUri = "https://openwish.local" });

        return new FriendService(
            provider.GetRequiredService<IServiceScopeFactory>(),
            mapper,
            new NoOpNotificationService(),
            new NoOpAppEmailSender(),
            options,
            NullLogger<FriendService>.Instance);
    }

    private sealed class NoOpNotificationService : INotificationService
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
            Task.FromResult(new NotificationModel());

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
        public Task SendConfirmationLinkAsync(string toEmail, string confirmationLink) => Task.CompletedTask;
        public Task SendPasswordResetCodeAsync(string toEmail, string resetCode) => Task.CompletedTask;
        public Task SendPasswordResetLinkAsync(string toEmail, string resetLink) => Task.CompletedTask;
        public Task SendFriendInviteEmailAsync(string toEmail, string inviterName, string inviteLink) => Task.CompletedTask;
        public Task SendEventInviteEmailAsync(string toEmail, string inviterName, string eventName, string inviteLink) => Task.CompletedTask;
        public Task SendGiftExchangeDrawnEmailAsync(string toEmail, string eventName, string recipientName, string eventLink) => Task.CompletedTask;
        public Task SendGiftExchangeResetEmailAsync(string toEmail, string eventName, string eventLink) => Task.CompletedTask;
        public Task SendEmailAsync(string toEmail, string subject, string message) => Task.CompletedTask;
    }
}