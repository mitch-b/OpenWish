namespace OpenWish.Data.Entities;

public class WillPurchase : BaseEntity
{
    public int WishlistItemId { get; set; }
    public WishlistItem WishlistItem { get; set; }

    public string PurchaserId { get; set; }
    public ApplicationUser Purchaser { get; set; }
}