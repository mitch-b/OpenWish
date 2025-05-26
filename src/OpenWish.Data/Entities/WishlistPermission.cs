namespace OpenWish.Data.Entities;

public class WishlistPermission : BaseEntity
{
    public int WishlistId { get; set; }
    public Wishlist Wishlist { get; set; }
    
    public string UserId { get; set; }
    public ApplicationUser User { get; set; }
    
    public string PermissionType { get; set; } // "View", "Edit", "Admin"
    
    // For invitation links that haven't been claimed yet
    public string? InvitationToken { get; set; }
    public DateTimeOffset? ExpirationDate { get; set; }
}