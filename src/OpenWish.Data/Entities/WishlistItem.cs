namespace OpenWish.Data.Entities;

public class WishlistItem : BaseEntity
{
    public string Name { get; set; }
    public string? Url { get; set; }
    public string? Image { get; set; }
    public string? Description { get; set; }
    public decimal? Price { get; set; }
    public string? WhereToBuy { get; set; }
    public int WishlistId { get; set; }
    public Wishlist Wishlist { get; set; }
    public bool IsPrivate { get; set; } // Indicates if the item is private
    public int? Priority { get; set; } // Priority level (e.g., 1 = High, 2 = Medium, 3 = Low)
    public ICollection<Comment> Comments { get; set; } // Comments on the item
    public ICollection<ItemReaction> Reactions { get; set; } // Reactions to the item
    public int? OrderIndex { get; set; } // Determines the order of items in the wishlist
    public bool IsHiddenFromOwner { get; set; } // Indicates if collaborative item is hidden from the wishlist owner
}