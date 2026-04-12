using AutoMapper;
using Microsoft.EntityFrameworkCore;
using OpenWish.Application.Models;
using OpenWish.Data;
using OpenWish.Data.Entities;
using OpenWish.Shared.Models;
using OpenWish.Shared.Services;

namespace OpenWish.Application.Services;

public class NotificationService(IDbContextFactory<ApplicationDbContext> contextFactory, IMapper mapper) : INotificationService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory = contextFactory;
    private readonly IMapper _mapper = mapper;

    public async Task<IEnumerable<NotificationModel>> GetUserNotificationsAsync(string userId, bool includeRead = false)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var query = context.Notifications
            .Include(n => n.User)
            .Where(n => n.UserId == userId && !n.Deleted);

        if (!includeRead)
        {
            query = query.Where(n => !n.IsRead);
        }

        var notifications = await query
            .OrderByDescending(n => n.Date)
            .ToListAsync();

        return _mapper.Map<IEnumerable<NotificationModel>>(notifications);
    }

    public async Task<int> GetUnreadNotificationCountAsync(string userId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Notifications
            .CountAsync(n => n.UserId == userId && !n.IsRead && !n.Deleted);
    }

    public async Task<NotificationModel> CreateNotificationAsync(string userId, string message)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var notification = new Notification
        {
            UserId = userId,
            Message = message,
            Date = DateTimeOffset.UtcNow,
            IsRead = false,
            CreatedOn = DateTimeOffset.UtcNow,
            UpdatedOn = DateTimeOffset.UtcNow
        };

        context.Notifications.Add(notification);
        await context.SaveChangesAsync();

        return _mapper.Map<NotificationModel>(notification);
    }

    public async Task<NotificationModel> CreateNotificationAsync(
        string senderUserId,
        string targetUserId,
        string title,
        string message,
        string type,
        NotificationActionModel? action = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var notification = new Notification
        {
            UserId = targetUserId,
            SenderUserId = senderUserId,
            Title = title,
            Message = message,
            Type = type,
            Date = DateTimeOffset.UtcNow,
            IsRead = false,
            ActionData = NotificationActionMapper.Serialize(action),
            CreatedOn = DateTimeOffset.UtcNow,
            UpdatedOn = DateTimeOffset.UtcNow
        };

        context.Notifications.Add(notification);
        await context.SaveChangesAsync();

        return _mapper.Map<NotificationModel>(notification);
    }

    public async Task<bool> MarkNotificationAsReadAsync(int notificationId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var notification = await context.Notifications.FindAsync(notificationId);

        if (notification == null || notification.Deleted)
        {
            return false;
        }

        notification.IsRead = true;
        notification.UpdatedOn = DateTimeOffset.UtcNow;

        await context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> MarkAllNotificationsAsReadAsync(string userId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var notifications = await context.Notifications
            .Where(n => n.UserId == userId && !n.IsRead && !n.Deleted)
            .ToListAsync();

        if (!notifications.Any())
        {
            return false;
        }

        foreach (var notification in notifications)
        {
            notification.IsRead = true;
            notification.UpdatedOn = DateTimeOffset.UtcNow;
        }

        await context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteNotificationAsync(int notificationId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var notification = await context.Notifications.FindAsync(notificationId);

        if (notification == null)
        {
            return false;
        }

        notification.Deleted = true;
        notification.UpdatedOn = DateTimeOffset.UtcNow;

        await context.SaveChangesAsync();
        return true;
    }
}