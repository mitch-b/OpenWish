using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenWish.Shared.Services;

namespace OpenWish.Web.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize]
public class NotificationController : ControllerBase
{
    private readonly INotificationService _notificationService;

    public NotificationController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    [HttpGet("user/{userId}")]
    public async Task<IActionResult> GetUserNotifications(string userId, [FromQuery] bool includeRead = false)
    {
        var notifications = await _notificationService.GetUserNotificationsAsync(userId, includeRead);
        return Ok(notifications);
    }

    [HttpGet("user/{userId}/count")]
    public async Task<IActionResult> GetUnreadNotificationCount(string userId)
    {
        var count = await _notificationService.GetUnreadNotificationCountAsync(userId);
        return Ok(count);
    }

    [HttpPost("user/{userId}")]
    public async Task<IActionResult> CreateNotification(string userId, [FromBody] string message)
    {
        var notification = await _notificationService.CreateNotificationAsync(userId, message);
        return Ok(notification);
    }

    [HttpPut("{notificationId}/read")]
    public async Task<IActionResult> MarkAsRead(int notificationId)
    {
        var result = await _notificationService.MarkNotificationAsReadAsync(notificationId);
        return Ok(result);
    }

    [HttpPut("user/{userId}/read-all")]
    public async Task<IActionResult> MarkAllAsRead(string userId)
    {
        var result = await _notificationService.MarkAllNotificationsAsReadAsync(userId);
        return Ok(result);
    }

    [HttpDelete("{notificationId}")]
    public async Task<IActionResult> DeleteNotification(int notificationId)
    {
        var result = await _notificationService.DeleteNotificationAsync(notificationId);
        return Ok(result);
    }
}