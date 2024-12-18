namespace OpenWish.Data.Entities;

public class Event
{
    public int EventId { get; set; }
    public string Name { get; set; }
    public DateTime Date { get; set; }
    public string Description { get; set; }
    public int CreatedByUserId { get; set; }
    public User CreatedBy { get; set; } // Creator of the event
    public ICollection<EventUser> EventUsers { get; set; } // Users invited to the event
    public ICollection<Wishlist> EventWishlists { get; set; } // Wishlists tied to the event
    public Event CopiedFromEvent { get; set; } // Reference to a past event if copied
    public bool IsRecurring { get; set; } // Indicates if the event is recurring
    public decimal? Budget { get; set; } // Budget for the event
    public bool IsGiftExchange { get; set; } // Indicates if this event has Gift Exchange
    public ICollection<GiftExchange> GiftExchanges { get; set; } // Gift Exchange pairings for the event
    public string Tags { get; set; } // Event-specific tags
    public ICollection<CustomPairingRule> PairingRules { get; set; } // Custom rules for pairing participants
}