namespace OpenWish.Data.Entities;

public class WillPurchase : BaseEntity
{
    public int WishlistItemId { get; set; }
    public WishlistItem WishlistItem { get; set; }

    public int PurchaserId { get; set; }
    public OpenWishUser Purchaser { get; set; }
}