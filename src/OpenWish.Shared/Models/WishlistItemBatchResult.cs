namespace OpenWish.Shared.Models;

public sealed class WishlistItemBatchResult
{
    public int AffectedCount { get; init; }
    public string? TargetWishlistName { get; init; }
}