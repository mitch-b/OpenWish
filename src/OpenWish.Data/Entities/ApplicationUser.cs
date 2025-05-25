using Microsoft.AspNetCore.Identity;

namespace OpenWish.Data.Entities;

public class ApplicationUser : IdentityUser
{
    public ICollection<Event> Events { get; set; } = [];
    public ICollection<Wishlist> Wishlists { get; set; } = [];
    public ICollection<Notification> Notifications { get; set; } = [];
    public ICollection<PublicWishlist> PublicWishlists { get; set; } = [];
    public ICollection<EventUser> EventUsers { get; set; } = [];
}