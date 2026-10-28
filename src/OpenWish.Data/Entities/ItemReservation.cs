namespace OpenWish.Data.Entities;

public class ItemReservation : BaseEntity
{
    public int WishlistItemId { get; set; }
    public WishlistItem? WishlistItem { get; set; }

    public required string UserId { get; set; }
    public ApplicationUser? User { get; set; }

    public DateTimeOffset ReservationDate { get; set; }
    public bool IsAnonymous { get; set; }

    /// <summary>Set when the reserver has bought the gift. Never shown to the list owner or other shoppers.</summary>
    public DateTimeOffset? PurchasedOn { get; set; }
}