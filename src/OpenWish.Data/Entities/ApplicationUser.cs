using Microsoft.AspNetCore.Identity;

namespace OpenWish.Data.Entities;

public class ApplicationUser : IdentityUser
{
    public ICollection<Event> Events { get; set; } = [];
    public ICollection<Wishlist> Wishlists { get; set; } = [];
    public ICollection<Notification> Notifications { get; set; } = [];
    public ICollection<PublicWishlist> PublicWishlists { get; set; } = [];
    public ICollection<EventUser> EventUsers { get; set; } = [];
    
    // Social features
    public ICollection<Friend> Friends { get; set; } = [];
    public ICollection<FriendRequest> SentFriendRequests { get; set; } = [];
    public ICollection<FriendRequest> ReceivedFriendRequests { get; set; } = [];
    public ICollection<WishlistPermission> WishlistPermissions { get; set; } = [];
    public ICollection<ActivityLog> Activities { get; set; } = [];
}