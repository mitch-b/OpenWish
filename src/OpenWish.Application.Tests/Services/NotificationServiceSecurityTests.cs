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
        await using var context = CreateContext();
        var notification = new Notification
        {
            UserId = "owner",
            Message = "Owner's notification",
            Date = DateTimeOffset.UtcNow
        };
        context.Notifications.Add(notification);
        await context.SaveChangesAsync();

        var service = new NotificationService(context, _mapper);

        var result = await service.MarkNotificationAsReadAsync(notification.PublicId, "intruder");

        Assert.False(result);
        Assert.False((await context.Notifications.SingleAsync()).IsRead);
    }

    [Fact]
    public async Task MarkNotificationAsReadAsync_MarksOwnUnreadNotification()
    {
        await using var context = CreateContext();
        var notification = new Notification
        {
            UserId = "owner",
            Message = "Owner's notification",
            Date = DateTimeOffset.UtcNow
        };
        context.Notifications.Add(notification);
        await context.SaveChangesAsync();

        var service = new NotificationService(context, _mapper);

        var result = await service.MarkNotificationAsReadAsync(notification.PublicId, "owner");

        Assert.True(result);
        Assert.True((await context.Notifications.SingleAsync()).IsRead);
    }

    [Fact]
    public async Task MarkAllNotificationsAsReadAsync_ReturnsFalseWhenNothingIsUnread()
    {
        await using var context = CreateContext();
        context.Notifications.Add(new Notification
        {
            UserId = "owner",
            Message = "Already read",
            Date = DateTimeOffset.UtcNow,
            IsRead = true
        });
        await context.SaveChangesAsync();

        var service = new NotificationService(context, _mapper);

        var result = await service.MarkAllNotificationsAsReadAsync("owner");

        Assert.False(result);
    }

    [Fact]
    public async Task MarkAllNotificationsAsReadAsync_MarksOnlyTheCallingUsersUnreadNotifications()
    {
        await using var context = CreateContext();
        context.Notifications.AddRange(
            new Notification { UserId = "owner", Message = "First", Date = DateTimeOffset.UtcNow },
            new Notification { UserId = "owner", Message = "Second", Date = DateTimeOffset.UtcNow },
            new Notification { UserId = "someone-else", Message = "Not mine", Date = DateTimeOffset.UtcNow });
        await context.SaveChangesAsync();

        var service = new NotificationService(context, _mapper);

        var result = await service.MarkAllNotificationsAsReadAsync("owner");

        Assert.True(result);
        var notifications = await context.Notifications.ToListAsync();
        Assert.All(notifications.Where(n => n.UserId == "owner"), n => Assert.True(n.IsRead));
        Assert.False(notifications.Single(n => n.UserId == "someone-else").IsRead);
    }

    [Fact]
    public async Task DeleteNotificationAsync_RejectsAnotherUsersNotification()
    {
        await using var context = CreateContext();
        var notification = new Notification
        {
            UserId = "owner",
            Message = "Owner's notification",
            Date = DateTimeOffset.UtcNow
        };
        context.Notifications.Add(notification);
        await context.SaveChangesAsync();

        var service = new NotificationService(context, _mapper);

        var result = await service.DeleteNotificationAsync(notification.PublicId, "intruder");

        Assert.False(result);
        Assert.False((await context.Notifications.SingleAsync()).Deleted);
    }

    [Fact]
    public async Task DeleteNotificationAsync_SoftDeletesOwnNotification()
    {
        await using var context = CreateContext();
        var notification = new Notification
        {
            UserId = "owner",
            Message = "Owner's notification",
            Date = DateTimeOffset.UtcNow
        };
        context.Notifications.Add(notification);
        await context.SaveChangesAsync();

        var service = new NotificationService(context, _mapper);

        var result = await service.DeleteNotificationAsync(notification.PublicId, "owner");

        Assert.True(result);
        Assert.True((await context.Notifications.SingleAsync()).Deleted);
    }

    private static ApplicationDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
}