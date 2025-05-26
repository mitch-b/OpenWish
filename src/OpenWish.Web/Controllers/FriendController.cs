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

    [HttpGet("search")]
    public async Task<IActionResult> SearchUsers([FromQuery] string term, [FromQuery] string userId, [FromQuery] int max = 10)
    {
        var users = await _friendService.SearchUsersAsync(term, userId, max);
        return Ok(users);
    }
}