using OpenWish.Shared.Models;

namespace OpenWish.Shared.Services;

/// <summary>Connects two people who are together by exchanging a short-lived code or QR link.</summary>
public interface IFriendJoinService
{
    /// <summary>Creates a new join code for the user, expiring any previous active code.</summary>
    /// <param name="origin">Public origin (scheme and host) used for the link; falls back to the configured base URI.</param>
    Task<FriendJoinCodeModel> CreateJoinCodeAsync(string userId, string? origin = null);

    /// <summary>Expires the user's active join code, if any.</summary>
    Task<bool> CancelJoinCodeAsync(string userId);

    /// <summary>Shows who owns a code (display name only) without changing anything.</summary>
    Task<FriendJoinPreviewModel> PreviewJoinCodeAsync(string userId, string code);

    /// <summary>Redeems a code, creating the friendship in both directions.</summary>
    Task<FriendJoinPreviewModel> RedeemJoinCodeAsync(string userId, string code);
}