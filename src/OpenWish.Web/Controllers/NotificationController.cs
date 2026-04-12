using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenWish.Shared.Models;
using OpenWish.Shared.Services;
using OpenWish.Web.Services;

namespace OpenWish.Web.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize]
public class NotificationController : ControllerBase
{
    private readonly INotificationService _notificationService;
    private readonly ApiUserContextService _userContextService;

    public NotificationController(INotificationService notificationService, ApiUserContextService userContextService)
    {
        _notificationService = notificationService;
        _userContextService = userContextService;
    }

    [HttpGet("user")]
    public async Task<IActionResult> GetUserNotifications([FromQuery] bool includeRead = false)
    {
        var userId = await _userContextService.GetUserIdAsync();
        if (userId is null) return Unauthorized();
        var notifications = await _notificationService.GetUserNotificationsAsync(userId, includeRead);
        return Ok(notifications);
    }

    [HttpGet("count")]
    public async Task<IActionResult> GetUnreadNotificationCount()
    {
        var userId = await _userContextService.GetUserIdAsync();
        if (userId is null) return Unauthorized();
        var count = await _notificationService.GetUnreadNotificationCountAsync(userId);
        return Ok(count);
    }

    [HttpPost]
    public async Task<IActionResult> CreateNotification([FromBody] string message)
    {
        var userId = await _userContextService.GetUserIdAsync();
        if (userId is null) return Unauthorized();
        var notification = await _notificationService.CreateNotificationAsync(userId, message);
        return Ok(notification);
    }

    [HttpPost("user/{targetUserId}/detailed")]
    public async Task<IActionResult> CreateDetailedNotification(string targetUserId, [FromBody] DetailedNotificationRequest request)
    {
        var senderUserId = await _userContextService.GetUserIdAsync();
        if (senderUserId is null) return Unauthorized();
        var notification = await _notificationService.CreateNotificationAsync(
            senderUserId,
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

    [HttpPut("read-all")]
    public async Task<IActionResult> MarkAllAsRead()
    {
        var userId = await _userContextService.GetUserIdAsync();
        if (userId is null) return Unauthorized();
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
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public NotificationActionModel? Action { get; set; }
}