using OpenWish.Shared.Models;

namespace OpenWish.Shared.Services;

public interface IFriendService
{
    // Friend management
    Task<IEnumerable<ApplicationUserModel>> GetFriendsAsync(string userId);
    Task<bool> AreFriendsAsync(string userId, string otherUserId);
    Task<bool> RemoveFriendAsync(string userId, string friendId);

    // Friend requests
    Task<FriendRequestModel> SendFriendRequestAsync(string requesterId, string receiverId);
    Task<IEnumerable<FriendRequestModel>> GetReceivedFriendRequestsAsync(string userId);
    Task<IEnumerable<FriendRequestModel>> GetSentFriendRequestsAsync(string userId);
    Task<bool> AcceptFriendRequestAsync(int requestId, string userId);
    Task<bool> RejectFriendRequestAsync(int requestId, string userId);

    // Email invites
    Task<bool> SendFriendInviteByEmailAsync(string senderUserId, string emailAddress);
    Task<bool> SendFriendInvitesByEmailAsync(string senderUserId, IEnumerable<string> emailAddresses);

    // User search for adding friends
    Task<IEnumerable<ApplicationUserModel>> SearchUsersAsync(string searchTerm, string currentUserId, int maxResults = 10);
}