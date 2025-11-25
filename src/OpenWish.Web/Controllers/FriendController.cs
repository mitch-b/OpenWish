using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenWish.Shared.Models;
using OpenWish.Shared.Services;

namespace OpenWish.Web.Controllers;

[ApiController]
[Route("api/friends")]
[Authorize]
public class FriendController : ControllerBase
{
    private readonly IFriendService _friendService;
    private readonly IUserContextService _userContextService;

    public FriendController(IFriendService friendService, IUserContextService userContextService)
    {
        _friendService = friendService;
        _userContextService = userContextService;
    }

    [HttpGet("user/{userId}")]
    public async Task<IActionResult> GetFriends(string userId)
    {
        var friends = await _friendService.GetFriendsAsync(userId);
        return Ok(friends);
    }

    [HttpGet("check/{userId}/{otherUserId}")]
    public async Task<IActionResult> CheckFriendship(string userId, string otherUserId)
    {
        var areFriends = await _friendService.AreFriendsAsync(userId, otherUserId);
        return Ok(areFriends);
    }

    [HttpDelete("{userId}/{friendId}")]
    public async Task<IActionResult> RemoveFriend(string userId, string friendId)
    {
        var result = await _friendService.RemoveFriendAsync(userId, friendId);
        return Ok(result);
    }

    [HttpPost("request/{requesterId}/{receiverId}")]
    public async Task<IActionResult> SendFriendRequest(string requesterId, string receiverId)
    {
        var request = await _friendService.SendFriendRequestAsync(requesterId, receiverId);
        return Ok(request);
    }

    [HttpGet("requests/received/{userId}")]
    public async Task<IActionResult> GetReceivedRequests(string userId)
    {
        var requests = await _friendService.GetReceivedFriendRequestsAsync(userId);
        return Ok(requests);
    }

    [HttpGet("requests/sent/{userId}")]
    public async Task<IActionResult> GetSentRequests(string userId)
    {
        var requests = await _friendService.GetSentFriendRequestsAsync(userId);
        return Ok(requests);
    }

    [HttpPost("request/{requestId}/accept/{userId}")]
    public async Task<IActionResult> AcceptRequest(int requestId, string userId)
    {
        var result = await _friendService.AcceptFriendRequestAsync(requestId, userId);
        return Ok(result);
    }

    [HttpPost("request/{requestId}/reject/{userId}")]
    public async Task<IActionResult> RejectRequest(int requestId, string userId)
    {
        var result = await _friendService.RejectFriendRequestAsync(requestId, userId);
        return Ok(result);
    }

    [HttpPost("request/{requestId}/cancel/{userId}")]
    public async Task<IActionResult> CancelRequest(int requestId, string userId)
    {
        var result = await _friendService.CancelFriendRequestAsync(requestId, userId);
        return result ? Ok(true) : NotFound();
    }

    [HttpPost("request/{requestId}/resend/{userId}")]
    public async Task<IActionResult> ResendRequest(int requestId, string userId)
    {
        try
        {
            var updatedRequest = await _friendService.ResendFriendRequestAsync(requestId, userId);
            return Ok(updatedRequest);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPost("invite/{senderUserId}")]
    public async Task<IActionResult> SendFriendInviteByEmail(string senderUserId, [FromQuery] string email)
    {
        try
        {
            var result = await _friendService.SendFriendInviteByEmailAsync(senderUserId, email);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("invite/{senderUserId}/batch")]
    public async Task<IActionResult> SendFriendInvitesByEmail(string senderUserId, [FromBody] List<string> emails)
    {
        try
        {
            var result = await _friendService.SendFriendInvitesByEmailAsync(senderUserId, emails);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("invite/complete/{newUserId}/{inviterUserId}")]
    public async Task<IActionResult> CreateFriendshipFromInvite(string newUserId, string inviterUserId)
    {
        try
        {
            var result = await _friendService.CreateFriendshipFromInviteAsync(newUserId, inviterUserId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    // Search by username functionality removed for security/privacy reasons
}