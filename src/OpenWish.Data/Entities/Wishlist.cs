namespace OpenWish.Data.Entities;

public class Wishlist : BaseEntity
{
    public string Name { get; set; }
    public string OwnerId { get; set; } // Nullable if shared directly to event
    public ApplicationUser Owner { get; set; }
    public int? EventId { get; set; } // Nullable if a personal wishlist
    public Event Event { get; set; }
    public ICollection<WishlistItem> Items { get; set; } // Items in the wishlist
    public ICollection<WillPurchase> WillPurchases { get; set; } // Who has committed to buying items
    public bool IsCollaborative { get; set; } // Indicates if multiple users can add items
    public bool IsPrivate { get; set; } // Indicates if the wishlist is private (only owner can see)
    public ICollection<WishlistComment> Comments { get; set; } // Comments on the wishlist
    public ICollection<WishlistReaction> Reactions { get; set; } // Reactions to the wishlist
}