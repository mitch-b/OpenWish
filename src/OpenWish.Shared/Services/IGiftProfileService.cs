using OpenWish.Shared.Models;

namespace OpenWish.Shared.Services;

public interface IGiftProfileService
{
    Task<GiftProfileModel> GetMyProfileAsync(string userId);

    /// <summary>Saves the caller's notes. Throws <see cref="ArgumentException"/> when a note is too long.</summary>
    Task SetMyProfileAsync(string userId, GiftProfileModel profile);

    /// <summary>Gets the notes of a wishlist's owner, or null when the viewer is the owner, is not a friend, or cannot open the list.</summary>
    Task<GiftProfileModel?> GetProfileForWishlistAsync(string wishlistPublicId, string viewerId);
}