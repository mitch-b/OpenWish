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

    [HttpGet]
    public async Task<IActionResult> GetUserNotifications([FromQuery] bool includeRead = false)
    {
        var userId = await _userContextService.GetUserIdAsync();
        if (userId is null)
        {
            return Unauthorized();
        }

        var notifications = await _notificationService.GetUserNotificationsAsync(userId, includeRead);
        return Ok(notifications);
    }

    [HttpGet("count")]
    public async Task<IActionResult> GetUnreadNotificationCount()
    {
        var userId = await _userContextService.GetUserIdAsync();
        if (userId is null)
        {
            return Unauthorized();
        }

        var count = await _notificationService.GetUnreadNotificationCountAsync(userId);
        return Ok(count);
    }

    [HttpPut("{notificationPublicId}/read")]
    public async Task<IActionResult> MarkAsRead(string notificationPublicId)
    {
        var userId = await _userContextService.GetUserIdAsync();
        if (userId is null)
        {
            return Unauthorized();
        }

        var result = await _notificationService.MarkNotificationAsReadAsync(notificationPublicId, userId);
        return result ? Ok(true) : NotFound();
    }

    [HttpPut("read-all")]
    public async Task<IActionResult> MarkAllAsRead()
    {
        var userId = await _userContextService.GetUserIdAsync();
        if (userId is null)
        {
            return Unauthorized();
        }

        var result = await _notificationService.MarkAllNotificationsAsReadAsync(userId);
        return Ok(result);
    }

    [HttpDelete("{notificationPublicId}")]
    public async Task<IActionResult> DeleteNotification(string notificationPublicId)
    {
        var userId = await _userContextService.GetUserIdAsync();
        if (userId is null)
        {
            return Unauthorized();
        }

        var result = await _notificationService.DeleteNotificationAsync(notificationPublicId, userId);
        return result ? Ok(true) : NotFound();
    }
}