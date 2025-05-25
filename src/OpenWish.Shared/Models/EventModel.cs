namespace OpenWish.Shared.Models;

public class EventModel : BaseEntityModel
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string? Description { get; set; }
    public DateTimeOffset Date { get; set; }
    public ApplicationUserModel? CreatedBy { get; set; }
    public bool IsRecurring { get; set; } // Indicates if the event is recurring
    public decimal? Budget { get; set; } // Budget for the event
    public bool IsGiftExchange { get; set; } // Indicates if this event has Gift Exchange
    public IEnumerable<string> Tags { get; set; } = [];
    public DateTimeOffset CreatedOn { get; set; }
    public ICollection<EventUserModel> EventUsers { get; set; } // Users invited to the event
    // public ICollection<Wishlist> EventWishlists { get; set; } // Wishlists tied to the event
}