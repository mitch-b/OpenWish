using OpenWish.Shared.Models;

namespace OpenWish.Shared.Services;

public interface INotificationService
{
    Task<IEnumerable<NotificationModel>> GetUserNotificationsAsync(string userId, bool includeRead = false);
    Task<int> GetUnreadNotificationCountAsync(string userId);
    Task<NotificationModel> CreateNotificationAsync(string userId, string message);
    Task<NotificationModel> CreateNotificationAsync(string senderUserId, string targetUserId, string title, string message, string type);
    Task<bool> MarkNotificationAsReadAsync(int notificationId);
    Task<bool> MarkAllNotificationsAsReadAsync(string userId);
    Task<bool> DeleteNotificationAsync(int notificationId);
}