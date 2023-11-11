using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace OpenWish.Entities;

public class Wishlist
{
    [Key]
    public Guid WishlistId { get; set; }
    public string Name { get; set; }
    public bool IsHidden { get; set; }
    public List<Item> Items { get; set; }
}

