using OpenWish.Shared.Models;

namespace OpenWish.Shared.Services;

/// <summary>
/// Service for managing friendships, friend requests, and email-based friend invitations.
/// Supports in-app relationships and email-based invites for unregistered users.
/// </summary>
public interface IFriendService
{
    /// <summary>Lists all users who are friends with the specified user.</summary>
    /// <param name="userId">The user ID to fetch friends for.</param>
    /// <returns>Collection of user models representing confirmed friendships.</returns>
    Task<IEnumerable<ApplicationUserModel>> GetFriendsAsync(string userId);

    /// <summary>Checks if two users are confirmed friends.</summary>
    /// <param name="userId">The first user ID.</param>
    /// <param name="otherUserId">The second user ID.</param>
    /// <returns>True if both users are confirmed friends.</returns>
    Task<bool> AreFriendsAsync(string userId, string otherUserId);

    /// <summary>Removes a friend relationship between two users.</summary>
    /// <param name="userId">The user ID initiating the removal.</param>
    /// <param name="friendId">The user ID to remove from friends.</param>
    /// <returns>True if the friendship was successfully removed.</returns>
    Task<bool> RemoveFriendAsync(string userId, string friendId);

    /// <summary>Sends a friend request from one registered user to another.</summary>
    /// <param name="requesterId">The user ID sending the request.</param>
    /// <param name="receiverId">The user ID receiving the request.</param>
    /// <returns>The friend request model.</returns>
    Task<FriendRequestModel> SendFriendRequestAsync(string requesterId, string receiverId);

    /// <summary>Lists all pending friend requests received by the specified user.</summary>
    /// <param name="userId">The user ID to fetch received requests for.</param>
    /// <returns>Collection of friend request models awaiting response.</returns>
    Task<IEnumerable<FriendRequestModel>> GetReceivedFriendRequestsAsync(string userId);

    /// <summary>Lists all friend requests sent by the specified user.</summary>
    /// <param name="userId">The user ID to fetch sent requests for.</param>
    /// <returns>Collection of sent friend request models (pending or accepted).</returns>
    Task<IEnumerable<FriendRequestModel>> GetSentFriendRequestsAsync(string userId);

    /// <summary>Accepts a friend request by its ID, confirming the friendship.</summary>
    /// <param name="requestId">The internal friend request ID.</param>
    /// <param name="userId">The user ID accepting the request (must be the receiver).</param>
    /// <returns>True if the request was successfully accepted.</returns>
    Task<bool> AcceptFriendRequestAsync(int requestId, string userId);

    /// <summary>Rejects a friend request by its ID.</summary>
    /// <param name="requestId">The internal friend request ID.</param>
    /// <param name="userId">The user ID rejecting the request (must be the receiver).</param>
    /// <returns>True if the request was successfully rejected.</returns>
    Task<bool> RejectFriendRequestAsync(int requestId, string userId);

    /// <summary>Cancels a sent friend request before it is accepted or rejected.</summary>
    /// <param name="requestId">The internal friend request ID.</param>
    /// <param name="requesterId">The user ID canceling the request (must be the sender).</param>
    /// <returns>True if the request was successfully canceled.</returns>
    Task<bool> CancelFriendRequestAsync(int requestId, string requesterId);

    /// <summary>Resends a friend request, updating its timestamp for emphasis.</summary>
    /// <param name="requestId">The internal friend request ID.</param>
    /// <param name="requesterId">The user ID resending the request (must be the original sender).</param>
    /// <returns>The resent friend request model with updated timestamp.</returns>
    Task<FriendRequestModel> ResendFriendRequestAsync(int requestId, string requesterId);

    /// <summary>Sends a friend invitation to an email address (user may not yet be registered).</summary>
    /// <param name="senderUserId">The user ID sending the invite.</param>
    /// <param name="emailAddress">The email address to invite.</param>
    /// <returns>True if the invitation email was successfully sent.</returns>
    Task<bool> SendFriendInviteByEmailAsync(string senderUserId, string emailAddress);

    /// <summary>Sends friend invitations to multiple email addresses in bulk.</summary>
    /// <param name="senderUserId">The user ID sending the invites.</param>
    /// <param name="emailAddresses">The collection of email addresses to invite.</param>
    /// <returns>True if all invitations were successfully sent (or queued).</returns>
    Task<bool> SendFriendInvitesByEmailAsync(string senderUserId, IEnumerable<string> emailAddresses);

    /// <summary>Converts an email-based friend invite into a confirmed friendship when the invited user registers.</summary>
    /// <param name="newUserId">The newly registered user ID matching an invite email.</param>
    /// <param name="inviterUserId">The user ID who originally sent the invite.</param>
    /// <returns>True if a friendship was successfully established.</returns>
    Task<bool> CreateFriendshipFromInviteAsync(string newUserId, string inviterUserId);

    /// <summary>Lists all pending email invitations sent by a user that have not yet been accepted.</summary>
    /// <param name="userId">The user ID to fetch pending invites for.</param>
    /// <returns>Collection of pending friend invite models.</returns>
    Task<IEnumerable<PendingFriendInviteModel>> GetPendingFriendInvitesAsync(string userId);

    /// <summary>Cancels a pending email-based friend invitation.</summary>
    /// <param name="inviteId">The internal pending invite ID.</param>
    /// <param name="userId">The user ID canceling the invite (must be the original sender).</param>
    /// <returns>True if the invite was successfully canceled.</returns>
    Task<bool> CancelPendingFriendInviteAsync(int inviteId, string userId);

    /// <summary>Resends a pending email-based friend invitation to the original recipient address.</summary>
    /// <param name="inviteId">The internal pending invite ID.</param>
    /// <param name="userId">The user ID resending the invite (must be the original sender).</param>
    /// <returns>True if the invitation email was successfully resent.</returns>
    Task<bool> ResendPendingFriendInviteAsync(int inviteId, string userId);

    /// <summary>Searches for registered users by username or display name to facilitate adding friends.</summary>
    /// <param name="searchTerm">The search term (partial username or name).</param>
    /// <param name="currentUserId">The user ID performing the search (to exclude from results).</param>
    /// <param name="maxResults">Maximum number of results to return.</param>
    /// <returns>Collection of matching user models.</returns>
    Task<IEnumerable<ApplicationUserModel>> SearchUsersAsync(string searchTerm, string currentUserId, int maxResults = 10);
}