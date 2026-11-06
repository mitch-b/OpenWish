namespace OpenWish.Data.Entities;

public class IntegrationConnectionWishlist
{
    public int IntegrationConnectionId { get; set; }
    public IntegrationConnection? IntegrationConnection { get; set; }
    public int WishlistId { get; set; }
    public Wishlist? Wishlist { get; set; }
}