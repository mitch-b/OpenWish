using OpenWish.Shared.Models;

namespace OpenWish.Shared.Services;

/// <summary>
/// Named groups of the caller's friends (such as Family or Book Club). A circle belongs to one person, is never
/// visible to its members, and members must already be the owner's friends.
/// </summary>
public interface ICircleService
{
    Task<IReadOnlyList<CircleModel>> GetCirclesAsync(string userId);

    /// <summary>Creates a circle. Throws <see cref="ArgumentException"/> for a bad or duplicate name.</summary>
    Task<CircleModel> CreateCircleAsync(string userId, string name);

    /// <summary>Renames a circle the caller owns. Throws <see cref="KeyNotFoundException"/> for anyone else's circle.</summary>
    Task<CircleModel> RenameCircleAsync(string userId, string circlePublicId, string name);

    /// <summary>Deletes a circle and withdraws the access it granted to wishlists.</summary>
    Task DeleteCircleAsync(string userId, string circlePublicId);

    /// <summary>
    /// Adds a friend to a circle; they immediately gain access to every wishlist shared with it. Throws
    /// <see cref="InvalidOperationException"/> when that person is not the caller's friend.
    /// </summary>
    Task<CircleModel> AddMemberAsync(string userId, string circlePublicId, string memberUserId);

    /// <summary>Removes someone from a circle and withdraws the access the circle gave them.</summary>
    Task<CircleModel> RemoveMemberAsync(string userId, string circlePublicId, string memberUserId);

    /// <summary>
    /// Shares one of the caller's wishlists with everyone in the circle, now and later. Throws
    /// <see cref="KeyNotFoundException"/> unless the caller owns the wishlist, and
    /// <see cref="InvalidOperationException"/> for a private wishlist.
    /// </summary>
    Task<CircleModel> ShareWishlistAsync(string userId, string circlePublicId, string wishlistPublicId);

    /// <summary>Stops sharing a wishlist with a circle and withdraws the access it granted.</summary>
    Task<CircleModel> UnshareWishlistAsync(string userId, string circlePublicId, string wishlistPublicId);
}