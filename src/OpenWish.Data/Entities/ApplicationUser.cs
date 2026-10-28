using Microsoft.AspNetCore.Identity;

namespace OpenWish.Data.Entities;

public class ApplicationUser : IdentityUser
{
    public ICollection<Event> Events { get; set; } = [];
    public ICollection<Wishlist> Wishlists { get; set; } = [];
    public ICollection<Notification> Notifications { get; set; } = [];
    public ICollection<PublicWishlist> PublicWishlists { get; set; } = [];
    public ICollection<EventUser> EventUsers { get; set; } = [];

    public DateTimeOffset? ChecklistDismissedOn { get; set; }

    // Optional birthday (no year), visible only to friends
    public int? BirthdayMonth { get; set; }
    public int? BirthdayDay { get; set; }

    // Optional gift hints, visible only to friends
    public string? GiftSizes { get; set; }
    public string? GiftFavorites { get; set; }
    public string? GiftAvoid { get; set; }

    // Social features
    public ICollection<Friend> Friends { get; set; } = [];
    public ICollection<FriendRequest> SentFriendRequests { get; set; } = [];
    public ICollection<FriendRequest> ReceivedFriendRequests { get; set; } = [];
    public ICollection<WishlistPermission> WishlistPermissions { get; set; } = [];
    public ICollection<ActivityLog> Activities { get; set; } = [];
}