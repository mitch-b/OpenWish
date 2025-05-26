namespace OpenWish.Shared.Models;

public class WishlistPermissionModel : BaseEntityModel
{
    public int WishlistId { get; set; }
    public WishlistModel? Wishlist { get; set; }

    public string UserId { get; set; }
    public ApplicationUserModel? User { get; set; }

    public string PermissionType { get; set; } // "View", "Edit", "Admin"

    // For invitation links that haven't been claimed yet
    public string? InvitationToken { get; set; }
    public DateTimeOffset? ExpirationDate { get; set; }
}