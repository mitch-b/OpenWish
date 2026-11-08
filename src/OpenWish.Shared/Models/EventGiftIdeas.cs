namespace OpenWish.Shared.Models;

public static class EventGiftIdeas
{
    public static int Count(WishlistModel wishlist) =>
        wishlist.Items.Count(item => !item.Deleted && !item.IsPrivate && item.ReceivedOn is null);
}