namespace OpenWish.Shared.Models;

/// <summary>Decides which ideas an owner should be asked "still want this?" about.</summary>
public static class WishlistFreshness
{
    public const int StaleAfterDays = 180;

    public static bool IsStale(WishlistItemModel item, DateTimeOffset now) =>
        !item.IsHiddenFromOwner && item.UpdatedOn <= now.AddDays(-StaleAfterDays);

    /// <summary>Ideas not touched for six months, oldest first.</summary>
    public static IReadOnlyList<WishlistItemModel> GetStaleItems(IEnumerable<WishlistItemModel> items, DateTimeOffset now) =>
        items.Where(item => IsStale(item, now)).OrderBy(item => item.UpdatedOn).ToList();
}