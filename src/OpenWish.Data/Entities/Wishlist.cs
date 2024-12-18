namespace OpenWish.Data.Entities;

public class Wishlist : BaseEntity
{
    public string Name { get; set; }
    public int? OwnerId { get; set; } // Nullable if shared directly to event
    public User Owner { get; set; }
    public int? EventId { get; set; } // Nullable if a personal wishlist
    public Event Event { get; set; }
    public ICollection<WishlistItem> Items { get; set; } // Items in the wishlist
    public ICollection<WillPurchase> WillPurchases { get; set; } // Who has committed to buying items
    public bool IsCollaborative { get; set; } // Indicates if multiple users can add items
    public ICollection<WishlistComment> Comments { get; set; } // Comments on the wishlist
    public ICollection<WishlistReaction> Reactions { get; set; } // Reactions to the wishlist
}