namespace OpenWish.Shared.Models;

public static class FriendWishlistFilter
{
    public static IReadOnlyList<WishlistModel> Apply(IEnumerable<WishlistModel> wishlists, string? search, string? ownerName)
    {
        var term = search?.Trim();
        var owner = ownerName?.Trim();

        return wishlists
            .Where(wishlist => string.IsNullOrEmpty(owner) ||
                string.Equals(wishlist.Owner?.UserName, owner, StringComparison.OrdinalIgnoreCase))
            .Where(wishlist => string.IsNullOrEmpty(term) ||
                wishlist.Name.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                (wishlist.Owner?.UserName?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false))
            .ToList();
    }

    public static IReadOnlyList<ApplicationUserModel> OrderFriends(IEnumerable<ApplicationUserModel> friends) =>
        friends
            .OrderBy(friend => friend.UserName ?? friend.Email ?? string.Empty, StringComparer.OrdinalIgnoreCase)
            .ToList();
}