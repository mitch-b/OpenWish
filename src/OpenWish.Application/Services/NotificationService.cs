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

        var models = _mapper.Map<List<NotificationModel>>(notifications);
        await ApplyInvitationStatusAsync(context, models, userId);
        return models;
    }

    // Answered invitations keep no live buttons, even after a reload or in another tab.
    private static async Task ApplyInvitationStatusAsync(ApplicationDbContext context, List<NotificationModel> models, string userId)
    {
        var invitations = new Dictionary<NotificationModel, int>();
        foreach (var model in models)
        {
            if (string.Equals(model.Action?.Type, "EventInvitation", StringComparison.OrdinalIgnoreCase) &&
                model.Action.Parameters.TryGetValue("eventUserId", out var raw) &&
                int.TryParse(raw, out var eventUserId))
            {
                invitations[model] = eventUserId;
            }
        }

        if (invitations.Count == 0)
        {
            return;
        }

        var ids = invitations.Values.Distinct().ToList();
        var statuses = await context.EventUsers
            .Where(eu => ids.Contains(eu.Id) && eu.UserId == userId && !eu.Deleted)
            .ToDictionaryAsync(eu => eu.Id, eu => eu.Status);

        foreach (var (model, eventUserId) in invitations)
        {
            if (statuses.TryGetValue(eventUserId, out var status) &&
                !string.Equals(status, "Pending", StringComparison.Ordinal))
            {
                model.Action!.Parameters["invitationStatus"] = status;
                model.Action.Options = [];
            }
        }
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

    public async Task<bool> MarkNotificationAsReadAsync(string notificationPublicId, string userId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var notification = await context.Notifications
            .FirstOrDefaultAsync(item => item.PublicId == notificationPublicId && item.UserId == userId && !item.Deleted);

        if (notification == null)
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

    public async Task<bool> DeleteNotificationAsync(string notificationPublicId, string userId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var notification = await context.Notifications
            .FirstOrDefaultAsync(item => item.PublicId == notificationPublicId && item.UserId == userId && !item.Deleted);

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