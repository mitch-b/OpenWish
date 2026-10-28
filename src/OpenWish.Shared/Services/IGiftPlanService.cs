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
}