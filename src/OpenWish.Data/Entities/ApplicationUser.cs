using Microsoft.AspNetCore.Identity;

namespace OpenWish.Data.Entities;

public class ApplicationUser : IdentityUser
{
    public ICollection<Event> Events { get; set; } = new List<Event>();
    public ICollection<Wishlist> Wishlists { get; set; } = new List<Wishlist>();
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    public ICollection<PublicWishlist> PublicWishlists { get; set; } = new List<PublicWishlist>();
}