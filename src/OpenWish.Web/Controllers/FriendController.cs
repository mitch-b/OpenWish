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

    [HttpGet("user")]
    public async Task<IActionResult> GetFriends()
    {
        var userId = await _userContextService.GetUserIdAsync();
        if (userId is null) return Unauthorized();
        var friends = await _friendService.GetFriendsAsync(userId);
        return Ok(friends);
    }

    [HttpGet("check/{otherUserId}")]
    public async Task<IActionResult> CheckFriendship(string otherUserId)
    {
        var userId = await _userContextService.GetUserIdAsync();
        if (userId is null) return Unauthorized();
        var areFriends = await _friendService.AreFriendsAsync(userId, otherUserId);
        return Ok(areFriends);
    }

    [HttpDelete("{friendId}")]
    public async Task<IActionResult> RemoveFriend(string friendId)
    {
        var userId = await _userContextService.GetUserIdAsync();
        if (userId is null) return Unauthorized();
        var result = await _friendService.RemoveFriendAsync(userId, friendId);
        return Ok(result);
    }

    [HttpPost("request/{receiverId}")]
    public async Task<IActionResult> SendFriendRequest(string receiverId)
    {
        var requesterId = await _userContextService.GetUserIdAsync();
        if (requesterId is null) return Unauthorized();
        try
        {
            var request = await _friendService.SendFriendRequestAsync(requesterId, receiverId);
            return Ok(request);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("requests/received")]
    public async Task<IActionResult> GetReceivedRequests()
    {
        var userId = await _userContextService.GetUserIdAsync();
        if (userId is null) return Unauthorized();
        var requests = await _friendService.GetReceivedFriendRequestsAsync(userId);
        return Ok(requests);
    }

    [HttpGet("requests/sent")]
    public async Task<IActionResult> GetSentRequests()
    {
        var userId = await _userContextService.GetUserIdAsync();
        if (userId is null) return Unauthorized();
        var requests = await _friendService.GetSentFriendRequestsAsync(userId);
        return Ok(requests);
    }

    [HttpPost("request/{requestId}/accept")]
    public async Task<IActionResult> AcceptRequest(int requestId)
    {
        var userId = await _userContextService.GetUserIdAsync();
        if (userId is null) return Unauthorized();
        var result = await _friendService.AcceptFriendRequestAsync(requestId, userId);
        return Ok(result);
    }

    [HttpPost("request/{requestId}/reject")]
    public async Task<IActionResult> RejectRequest(int requestId)
    {
        var userId = await _userContextService.GetUserIdAsync();
        if (userId is null) return Unauthorized();
        var result = await _friendService.RejectFriendRequestAsync(requestId, userId);
        return Ok(result);
    }

    [HttpPost("request/{requestId}/cancel")]
    public async Task<IActionResult> CancelRequest(int requestId)
    {
        var userId = await _userContextService.GetUserIdAsync();
        if (userId is null) return Unauthorized();
        var result = await _friendService.CancelFriendRequestAsync(requestId, userId);
        return result ? Ok(true) : NotFound();
    }

    [HttpPost("request/{requestId}/resend")]
    public async Task<IActionResult> ResendRequest(int requestId)
    {
        var userId = await _userContextService.GetUserIdAsync();
        if (userId is null) return Unauthorized();
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

    [HttpPost("invite")]
    public async Task<IActionResult> SendFriendInviteByEmail([FromQuery] string email)
    {
        var senderUserId = await _userContextService.GetUserIdAsync();
        if (senderUserId is null) return Unauthorized();
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

    [HttpPost("invite/batch")]
    public async Task<IActionResult> SendFriendInvitesByEmail([FromBody] List<string> emails)
    {
        var senderUserId = await _userContextService.GetUserIdAsync();
        if (senderUserId is null) return Unauthorized();
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

    [HttpPost("invite/complete/{inviterUserId}")]
    public async Task<IActionResult> CreateFriendshipFromInvite(string inviterUserId)
    {
        var newUserId = await _userContextService.GetUserIdAsync();
        if (newUserId is null) return Unauthorized();
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

    [HttpGet("pending-invites")]
    public async Task<IActionResult> GetPendingFriendInvites()
    {
        var userId = await _userContextService.GetUserIdAsync();
        if (userId is null) return Unauthorized();
        var invites = await _friendService.GetPendingFriendInvitesAsync(userId);
        return Ok(invites);
    }

    [HttpPost("pending-invite/{inviteId}/cancel")]
    public async Task<IActionResult> CancelPendingFriendInvite(int inviteId)
    {
        var userId = await _userContextService.GetUserIdAsync();
        if (userId is null) return Unauthorized();
        var result = await _friendService.CancelPendingFriendInviteAsync(inviteId, userId);
        return result ? Ok(true) : NotFound();
    }

    [HttpPost("pending-invite/{inviteId}/resend")]
    public async Task<IActionResult> ResendPendingFriendInvite(int inviteId)
    {
        var userId = await _userContextService.GetUserIdAsync();
        if (userId is null) return Unauthorized();
        var result = await _friendService.ResendPendingFriendInviteAsync(inviteId, userId);
        return result ? Ok(true) : NotFound();
    }

    // Search by username functionality removed for security/privacy reasons
}