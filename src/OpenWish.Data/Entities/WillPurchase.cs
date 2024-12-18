namespace OpenWish.Data.Entities;

public class WillPurchase
{
    public int WillPurchaseId { get; set; }
    public int WishlistItemId { get; set; }
    public WishlistItem WishlistItem { get; set; }

    public int PurchaserId { get; set; }
    public User Purchaser { get; set; }
}