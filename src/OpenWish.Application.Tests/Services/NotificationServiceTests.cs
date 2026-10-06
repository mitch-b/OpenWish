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

public class NotificationServiceTests
{
    private readonly IMapper _mapper = new MapperConfiguration(
        configuration => configuration.AddProfile<OpenWishProfile>(),
        NullLoggerFactory.Instance).CreateMapper();

    [Fact]
    public async Task GetUserNotificationsAsync_ExcludesDeletedNotifications()
    {
        var factory = CreateFactory();
        await using (var context = factory.CreateDbContext())
        {
            var user = new ApplicationUser { Id = "user1", UserName = "user1" };
            context.Users.Add(user);
            context.Notifications.AddRange(
                new Notification
                {
                    UserId = "user1",
                    Title = "Active",
                    Message = "Should appear",
                    Date = DateTimeOffset.UtcNow,
                    IsRead = false
                },
                new Notification
                {
                    UserId = "user1",
                    Title = "Deleted",
                    Message = "Should not appear",
                    Date = DateTimeOffset.UtcNow,
                    IsRead = false,
                    Deleted = true
                });
            await context.SaveChangesAsync();
        }

        var service = new NotificationService(factory, _mapper);
        var notifications = await service.GetUserNotificationsAsync("user1");

        Assert.Single(notifications);
        Assert.Equal("Active", Assert.Single(notifications).Title);
    }

    [Fact]
    public async Task GetUserNotificationsAsync_FiltersByReadStatus()
    {
        var factory = CreateFactory();
        await using (var context = factory.CreateDbContext())
        {
            var user = new ApplicationUser { Id = "user1", UserName = "user1" };
            context.Users.Add(user);
            context.Notifications.AddRange(
                new Notification
                {
                    UserId = "user1",
                    Title = "Unread",
                    Message = "Not read",
                    Date = DateTimeOffset.UtcNow,
                    IsRead = false
                },
                new Notification
                {
                    UserId = "user1",
                    Title = "Read",
                    Message = "Already read",
                    Date = DateTimeOffset.UtcNow,
                    IsRead = true
                });
            await context.SaveChangesAsync();
        }

        var service = new NotificationService(factory, _mapper);
        var unreadNotifications = await service.GetUserNotificationsAsync("user1", includeRead: false);
        var allNotifications = await service.GetUserNotificationsAsync("user1", includeRead: true);

        Assert.Single(unreadNotifications);
        Assert.Equal(2, allNotifications.Count());
    }

    [Fact]
    public async Task GetUserNotificationsAsync_SortsByDateDescending()
    {
        var factory = CreateFactory();
        var now = DateTimeOffset.UtcNow;
        await using (var context = factory.CreateDbContext())
        {
            var user = new ApplicationUser { Id = "user1", UserName = "user1" };
            context.Users.Add(user);
            context.Notifications.AddRange(
                new Notification
                {
                    UserId = "user1",
                    Title = "Oldest",
                    Message = "First",
                    Date = now.AddHours(-2),
                    IsRead = false
                },
                new Notification
                {
                    UserId = "user1",
                    Title = "Newest",
                    Message = "Last",
                    Date = now,
                    IsRead = false
                },
                new Notification
                {
                    UserId = "user1",
                    Title = "Middle",
                    Message = "Second",
                    Date = now.AddHours(-1),
                    IsRead = false
                });
            await context.SaveChangesAsync();
        }

        var service = new NotificationService(factory, _mapper);
        var notifications = await service.GetUserNotificationsAsync("user1");

        var notificationList = notifications.ToList();
        Assert.Equal("Newest", notificationList[0].Title);
        Assert.Equal("Middle", notificationList[1].Title);
        Assert.Equal("Oldest", notificationList[2].Title);
    }

    [Fact]
    public async Task GetUnreadNotificationCountAsync_CountsOnlyUnread()
    {
        var factory = CreateFactory();
        await using (var context = factory.CreateDbContext())
        {
            var user = new ApplicationUser { Id = "user1", UserName = "user1" };
            context.Users.Add(user);
            context.Notifications.AddRange(
                new Notification
                {
                    UserId = "user1",
                    Title = "Unread 1",
                    Message = "Not read",
                    Date = DateTimeOffset.UtcNow,
                    IsRead = false
                },
                new Notification
                {
                    UserId = "user1",
                    Title = "Unread 2",
                    Message = "Not read",
                    Date = DateTimeOffset.UtcNow,
                    IsRead = false
                },
                new Notification
                {
                    UserId = "user1",
                    Title = "Read",
                    Message = "Read",
                    Date = DateTimeOffset.UtcNow,
                    IsRead = true
                },
                new Notification
                {
                    UserId = "user1",
                    Title = "Deleted",
                    Message = "Deleted",
                    Date = DateTimeOffset.UtcNow,
                    IsRead = false,
                    Deleted = true
                });
            await context.SaveChangesAsync();
        }

        var service = new NotificationService(factory, _mapper);
        var count = await service.GetUnreadNotificationCountAsync("user1");

        Assert.Equal(2, count);
    }

    [Fact]
    public async Task CreateNotificationAsync_WithMessage_CreatesNotification()
    {
        var factory = CreateFactory();
        var service = new NotificationService(factory, _mapper);

        var result = await service.CreateNotificationAsync("user1", "Test message");

        Assert.NotNull(result);
        Assert.Equal("user1", result.UserId);
        Assert.Equal("Test message", result.Message);
        Assert.False(result.IsRead);

        await using var context = factory.CreateDbContext();
        var saved = await context.Notifications.SingleAsync();
        Assert.Equal("Test message", saved.Message);
    }

    [Fact]
    public async Task CreateNotificationAsync_WithAction_SerializesAction()
    {
        var factory = CreateFactory();
        var service = new NotificationService(factory, _mapper);
        var action = new NotificationActionModel
        {
            NavigateTo = "/wishlists/123"
        };

        var result = await service.CreateNotificationAsync(
            "sender1",
            "user1",
            "Title",
            "Message",
            "WishlistShared",
            action);

        Assert.NotNull(result);
        Assert.Equal("sender1", result.SenderUserId);
        Assert.Equal("WishlistShared", result.Type);

        await using var context = factory.CreateDbContext();
        var saved = await context.Notifications.SingleAsync();
        Assert.NotNull(saved.ActionData);
    }

    [Fact]
    public async Task MarkNotificationAsReadAsync_UpdatesNotificationState()
    {
        var factory = CreateFactory();
        string notificationPublicId;
        await using (var context = factory.CreateDbContext())
        {
            var notification = new Notification
            {
                UserId = "user1",
                Title = "Test",
                Message = "Test message",
                Date = DateTimeOffset.UtcNow,
                IsRead = false
            };
            context.Notifications.Add(notification);
            await context.SaveChangesAsync();
            notificationPublicId = notification.PublicId;
        }

        var service = new NotificationService(factory, _mapper);
        var result = await service.MarkNotificationAsReadAsync(notificationPublicId, "user1");

        Assert.True(result);
        await using var verifyContext = factory.CreateDbContext();
        var updated = await verifyContext.Notifications.SingleAsync();
        Assert.True(updated.IsRead);
    }

    [Fact]
    public async Task MarkNotificationAsReadAsync_RejectsUnauthorizedUser()
    {
        var factory = CreateFactory();
        string notificationPublicId;
        await using (var context = factory.CreateDbContext())
        {
            var notification = new Notification
            {
                UserId = "user1",
                Title = "Test",
                Message = "Test message",
                Date = DateTimeOffset.UtcNow,
                IsRead = false
            };
            context.Notifications.Add(notification);
            await context.SaveChangesAsync();
            notificationPublicId = notification.PublicId;
        }

        var service = new NotificationService(factory, _mapper);
        var result = await service.MarkNotificationAsReadAsync(notificationPublicId, "user2");

        Assert.False(result);
    }

    [Fact]
    public async Task MarkNotificationAsReadAsync_RejectsDeletedNotification()
    {
        var factory = CreateFactory();
        string notificationPublicId;
        await using (var context = factory.CreateDbContext())
        {
            var notification = new Notification
            {
                UserId = "user1",
                Title = "Test",
                Message = "Test message",
                Date = DateTimeOffset.UtcNow,
                IsRead = false,
                Deleted = true
            };
            context.Notifications.Add(notification);
            await context.SaveChangesAsync();
            notificationPublicId = notification.PublicId;
        }

        var service = new NotificationService(factory, _mapper);
        var result = await service.MarkNotificationAsReadAsync(notificationPublicId, "user1");

        Assert.False(result);
    }

    [Fact]
    public async Task MarkAllNotificationsAsReadAsync_UpdatesAllUnread()
    {
        var factory = CreateFactory();
        await using (var context = factory.CreateDbContext())
        {
            context.Notifications.AddRange(
                new Notification
                {
                    UserId = "user1",
                    Title = "Unread 1",
                    Message = "Not read",
                    Date = DateTimeOffset.UtcNow,
                    IsRead = false
                },
                new Notification
                {
                    UserId = "user1",
                    Title = "Unread 2",
                    Message = "Not read",
                    Date = DateTimeOffset.UtcNow,
                    IsRead = false
                },
                new Notification
                {
                    UserId = "user1",
                    Title = "Already read",
                    Message = "Read",
                    Date = DateTimeOffset.UtcNow,
                    IsRead = true
                });
            await context.SaveChangesAsync();
        }

        var service = new NotificationService(factory, _mapper);
        var result = await service.MarkAllNotificationsAsReadAsync("user1");

        Assert.True(result);
        await using var verifyContext = factory.CreateDbContext();
        var notifications = await verifyContext.Notifications.ToListAsync();
        Assert.All(notifications, n => Assert.True(n.IsRead));
    }

    [Fact]
    public async Task MarkAllNotificationsAsReadAsync_ReturnsFalseWhenNoneUnread()
    {
        var factory = CreateFactory();
        var service = new NotificationService(factory, _mapper);

        var result = await service.MarkAllNotificationsAsReadAsync("user1");

        Assert.False(result);
    }

    [Fact]
    public async Task DeleteNotificationAsync_SoftDeletesNotification()
    {
        var factory = CreateFactory();
        string notificationPublicId;
        await using (var context = factory.CreateDbContext())
        {
            var notification = new Notification
            {
                UserId = "user1",
                Title = "Test",
                Message = "Test message",
                Date = DateTimeOffset.UtcNow,
                IsRead = false
            };
            context.Notifications.Add(notification);
            await context.SaveChangesAsync();
            notificationPublicId = notification.PublicId;
        }

        var service = new NotificationService(factory, _mapper);
        var result = await service.DeleteNotificationAsync(notificationPublicId, "user1");

        Assert.True(result);
        await using var verifyContext = factory.CreateDbContext();
        var deleted = await verifyContext.Notifications.IgnoreQueryFilters().SingleAsync();
        Assert.True(deleted.Deleted);
    }

    [Fact]
    public async Task DeleteNotificationAsync_RejectsUnauthorizedUser()
    {
        var factory = CreateFactory();
        string notificationPublicId;
        await using (var context = factory.CreateDbContext())
        {
            var notification = new Notification
            {
                UserId = "user1",
                Title = "Test",
                Message = "Test message",
                Date = DateTimeOffset.UtcNow,
                IsRead = false
            };
            context.Notifications.Add(notification);
            await context.SaveChangesAsync();
            notificationPublicId = notification.PublicId;
        }

        var service = new NotificationService(factory, _mapper);
        var result = await service.DeleteNotificationAsync(notificationPublicId, "user2");

        Assert.False(result);
    }

    [Fact]
    public async Task DeleteNotificationAsync_RejectsAlreadyDeletedNotification()
    {
        var factory = CreateFactory();
        string notificationPublicId;
        await using (var context = factory.CreateDbContext())
        {
            var notification = new Notification
            {
                UserId = "user1",
                Title = "Test",
                Message = "Test message",
                Date = DateTimeOffset.UtcNow,
                IsRead = false,
                Deleted = true
            };
            context.Notifications.Add(notification);
            await context.SaveChangesAsync();
            notificationPublicId = notification.PublicId;
        }

        var service = new NotificationService(factory, _mapper);
        var result = await service.DeleteNotificationAsync(notificationPublicId, "user1");

        Assert.False(result);
    }

    private static TestDbContextFactory CreateFactory() =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private sealed class TestDbContextFactory(DbContextOptions<ApplicationDbContext> options)
        : IDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext() => new(options);
    }
}