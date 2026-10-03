using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using OpenWish.Application.Models;
using OpenWish.Application.Services;
using OpenWish.Data;
using OpenWish.Data.Entities;
using Xunit;

namespace OpenWish.Application.Tests.Services;

public class NotificationServiceSecurityTests
{
    private readonly IMapper _mapper = new MapperConfiguration(
        configuration => configuration.AddProfile<OpenWishProfile>(),
        NullLoggerFactory.Instance).CreateMapper();

    [Fact]
    public async Task MarkNotificationAsReadAsync_RejectsAnotherUsersNotification()
    {
        var factory = CreateFactory();
        await using var context = factory.CreateDbContext();
        var notification = new Notification
        {
            UserId = "owner",
            Message = "Owner's notification",
            Date = DateTimeOffset.UtcNow
        };
        context.Notifications.Add(notification);
        await context.SaveChangesAsync();

        var service = new NotificationService(factory, _mapper);

        var result = await service.MarkNotificationAsReadAsync(notification.PublicId, "intruder");

        Assert.False(result);
        Assert.False((await context.Notifications.SingleAsync()).IsRead);
    }

    [Fact]
    public async Task MarkNotificationAsReadAsync_MarksOwnUnreadNotification()
    {
        var factory = CreateFactory();
        await using var context = factory.CreateDbContext();
        var notification = new Notification
        {
            UserId = "owner",
            Message = "Owner's notification",
            Date = DateTimeOffset.UtcNow
        };
        context.Notifications.Add(notification);
        await context.SaveChangesAsync();

        var service = new NotificationService(factory, _mapper);

        var result = await service.MarkNotificationAsReadAsync(notification.PublicId, "owner");

        Assert.True(result);
        await using var verificationContext = factory.CreateDbContext();
        Assert.True((await verificationContext.Notifications.SingleAsync()).IsRead);
    }

    [Fact]
    public async Task MarkAllNotificationsAsReadAsync_ReturnsFalseWhenNothingIsUnread()
    {
        var factory = CreateFactory();
        await using var context = factory.CreateDbContext();
        context.Notifications.Add(new Notification
        {
            UserId = "owner",
            Message = "Already read",
            Date = DateTimeOffset.UtcNow,
            IsRead = true
        });
        await context.SaveChangesAsync();

        var service = new NotificationService(factory, _mapper);

        var result = await service.MarkAllNotificationsAsReadAsync("owner");

        Assert.False(result);
    }

    [Fact]
    public async Task MarkAllNotificationsAsReadAsync_MarksOnlyTheCallingUsersUnreadNotifications()
    {
        var factory = CreateFactory();
        await using var context = factory.CreateDbContext();
        context.Notifications.AddRange(
            new Notification { UserId = "owner", Message = "First", Date = DateTimeOffset.UtcNow },
            new Notification { UserId = "owner", Message = "Second", Date = DateTimeOffset.UtcNow },
            new Notification { UserId = "someone-else", Message = "Not mine", Date = DateTimeOffset.UtcNow });
        await context.SaveChangesAsync();

        var service = new NotificationService(factory, _mapper);

        var result = await service.MarkAllNotificationsAsReadAsync("owner");

        Assert.True(result);
        await using var verificationContext = factory.CreateDbContext();
        var notifications = await verificationContext.Notifications.ToListAsync();
        Assert.All(notifications.Where(n => n.UserId == "owner"), n => Assert.True(n.IsRead));
        Assert.False(notifications.Single(n => n.UserId == "someone-else").IsRead);
    }

    [Fact]
    public async Task DeleteNotificationAsync_RejectsAnotherUsersNotification()
    {
        var factory = CreateFactory();
        await using var context = factory.CreateDbContext();
        var notification = new Notification
        {
            UserId = "owner",
            Message = "Owner's notification",
            Date = DateTimeOffset.UtcNow
        };
        context.Notifications.Add(notification);
        await context.SaveChangesAsync();

        var service = new NotificationService(factory, _mapper);

        var result = await service.DeleteNotificationAsync(notification.PublicId, "intruder");

        Assert.False(result);
        Assert.False((await context.Notifications.SingleAsync()).Deleted);
    }

    [Fact]
    public async Task DeleteNotificationAsync_SoftDeletesOwnNotification()
    {
        var factory = CreateFactory();
        await using var context = factory.CreateDbContext();
        var notification = new Notification
        {
            UserId = "owner",
            Message = "Owner's notification",
            Date = DateTimeOffset.UtcNow
        };
        context.Notifications.Add(notification);
        await context.SaveChangesAsync();

        var service = new NotificationService(factory, _mapper);

        var result = await service.DeleteNotificationAsync(notification.PublicId, "owner");

        Assert.True(result);
        await using var verificationContext = factory.CreateDbContext();
        Assert.True((await verificationContext.Notifications.SingleAsync()).Deleted);
    }

    [Fact]
    public async Task DeleteNotificationAsync_DoesNotDeleteAlreadyDeletedNotificationAgain()
    {
        var factory = CreateFactory();
        await using var context = factory.CreateDbContext();
        var notification = new Notification
        {
            UserId = "owner",
            Message = "Removed",
            Date = DateTimeOffset.UtcNow
        };
        context.Notifications.Add(notification);
        await context.SaveChangesAsync();
        var service = new NotificationService(factory, _mapper);

        Assert.True(await service.DeleteNotificationAsync(notification.PublicId, "owner"));
        await using var verificationContext = factory.CreateDbContext();
        var deletedAt = (await verificationContext.Notifications.SingleAsync()).UpdatedOn;
        Assert.False(await service.DeleteNotificationAsync(notification.PublicId, "owner"));
        await verificationContext.Entry(await verificationContext.Notifications.SingleAsync()).ReloadAsync();
        Assert.Equal(deletedAt, (await verificationContext.Notifications.SingleAsync()).UpdatedOn);
        Assert.False(await service.MarkNotificationAsReadAsync(notification.PublicId, "owner"));
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