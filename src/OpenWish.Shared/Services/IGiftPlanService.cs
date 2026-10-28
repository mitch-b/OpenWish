using OpenWish.Shared.Models;

namespace OpenWish.Shared.Services;

/// <summary>A shopper's private view of the gifts they have reserved for other people.</summary>
public interface IGiftPlanService
{
    /// <summary>
    /// Gets the caller's reservations grouped by recipient. Only the caller's own reservations are
    /// returned, and never for wishlists the caller can no longer open.
    /// </summary>
    Task<GiftPlanModel> GetGiftPlanAsync(string userId);

    /// <summary>
    /// Marks the caller's own reservation as bought, or back to still-to-buy. Throws
    /// <see cref="KeyNotFoundException"/> unless the caller holds an active reservation on a
    /// wishlist they can still open.
    /// </summary>
    Task SetGiftPurchasedAsync(string userId, string wishlistPublicId, string itemPublicId, bool isPurchased);
}