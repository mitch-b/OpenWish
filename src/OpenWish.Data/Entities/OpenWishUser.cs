namespace OpenWish.Data.Entities;

public class OpenWishUser : BaseEntity
{
    public string Name { get; set; }
    public string Email { get; set; }
    public ICollection<Event> Events { get; set; } // Events the user is part of
    public ICollection<Wishlist> Wishlists { get; set; } // Personal wishlists
    public ICollection<Notification> Notifications { get; set; } // Notifications for the user
    public ICollection<PublicWishlist> PublicWishlists { get; set; } // Public wishlists shared outside events
}