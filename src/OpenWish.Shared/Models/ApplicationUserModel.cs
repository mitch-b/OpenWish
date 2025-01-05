namespace OpenWish.Shared.Models;

public record ApplicationUserModel
{
    public string Id { get; set; }
    public string UserName { get; set; }
    public string Email { get; set; }

    //public ICollection<Event> Events { get; set; } = new List<Event>();
    //public ICollection<Wishlist> Wishlists { get; set; } = new List<Wishlist>();
    //public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    //public ICollection<PublicWishlist> PublicWishlists { get; set; } = new List<PublicWishlist>();
}
