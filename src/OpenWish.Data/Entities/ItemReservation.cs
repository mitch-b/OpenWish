namespace OpenWish.Data.Entities;

public class ItemReservation : BaseEntity
{
    public int WishlistItemId { get; set; }
    public WishlistItem WishlistItem { get; set; }
    
    public string UserId { get; set; }
    public ApplicationUser User { get; set; }
    
    public DateTimeOffset ReservationDate { get; set; }
    public bool IsAnonymous { get; set; }
}