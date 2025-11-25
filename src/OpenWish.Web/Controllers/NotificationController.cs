using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenWish.Shared.Models;
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

    [HttpPost("user/{targetUserId}/detailed")]
    public async Task<IActionResult> CreateDetailedNotification(string targetUserId, [FromBody] DetailedNotificationRequest request)
    {
        var notification = await _notificationService.CreateNotificationAsync(
            request.SenderUserId,
            targetUserId,
            request.Title,
            request.Message,
            request.Type,
            request.Action);

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

public class DetailedNotificationRequest
{
    public string SenderUserId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public NotificationActionModel? Action { get; set; }
}