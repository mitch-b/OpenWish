using System;

namespace OpenWish.Common.Models;

public class Wishlist
{
    [Key]
    public Guid WishlistId { get; set; }
    public string Name { get; set; }
    public bool IsHidden { get; set; }
    public int UserId { get; set; }
    public User User { get; set; }
    public List<Item> Items { get; set; }
}

