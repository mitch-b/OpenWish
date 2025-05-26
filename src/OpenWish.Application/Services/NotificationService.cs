using AutoMapper;
using Microsoft.EntityFrameworkCore;
using OpenWish.Data;
using OpenWish.Data.Entities;
using OpenWish.Shared.Models;
using OpenWish.Shared.Services;

namespace OpenWish.Application.Services;

public class NotificationService(ApplicationDbContext context, IMapper mapper) : INotificationService
{
    private readonly ApplicationDbContext _context = context;
    private readonly IMapper _mapper = mapper;

    public async Task<IEnumerable<NotificationModel>> GetUserNotificationsAsync(string userId, bool includeRead = false)
    {
        var query = _context.Notifications
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
        return await _context.Notifications
            .CountAsync(n => n.UserId == userId && !n.IsRead && !n.Deleted);
    }

    public async Task<NotificationModel> CreateNotificationAsync(string userId, string message)
    {
        var notification = new Notification
        {
            UserId = userId,
            Message = message,
            Date = DateTimeOffset.UtcNow,
            IsRead = false,
            CreatedOn = DateTimeOffset.UtcNow,
            UpdatedOn = DateTimeOffset.UtcNow
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();

        return _mapper.Map<NotificationModel>(notification);
    }

    public async Task<bool> MarkNotificationAsReadAsync(int notificationId)
    {
        var notification = await _context.Notifications.FindAsync(notificationId);

        if (notification == null || notification.Deleted)
        {
            return false;
        }

        notification.IsRead = true;
        notification.UpdatedOn = DateTimeOffset.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> MarkAllNotificationsAsReadAsync(string userId)
    {
        var notifications = await _context.Notifications
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

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteNotificationAsync(int notificationId)
    {
        var notification = await _context.Notifications.FindAsync(notificationId);

        if (notification == null)
        {
            return false;
        }

        notification.Deleted = true;
        notification.UpdatedOn = DateTimeOffset.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }
}