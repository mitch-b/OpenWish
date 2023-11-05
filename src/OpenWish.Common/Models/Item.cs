using System;

namespace OpenWish.Common.Models;

public class Item
{
    [Key]
    public Guid ItemId { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public decimal Price { get; set; }
    public string StoreUrl { get; set; }
    public bool IsClaimed { get; set; }

    public Guid WishlistId { get; set; }
    public Wishlist Wishlist { get; set; }
    public List<Comment> Comments { get; set; }
}

