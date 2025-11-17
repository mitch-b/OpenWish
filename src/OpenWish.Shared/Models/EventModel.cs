namespace OpenWish.Shared.Models;

public class EventModel : BaseEntityModel
{
    public string Name { get; set; }
    public string? Description { get; set; }
    public DateTimeOffset Date { get; set; }
    public ApplicationUserModel? CreatedBy { get; set; }
    public bool IsRecurring { get; set; } // Indicates if the event is recurring
    public decimal? Budget { get; set; } // Budget for the event
    public bool IsGiftExchange { get; set; } // Indicates if this event has Gift Exchange
    public DateTimeOffset? NamesDrawnOn { get; set; } // Timestamp when names were drawn for gift exchange
    public IEnumerable<string> Tags { get; set; } = [];
    public DateTimeOffset CreatedOn { get; set; }
    public ICollection<EventUserModel> EventUsers { get; set; } // Users invited to the event
    public ICollection<WishlistModel> EventWishlists { get; set; } = []; // Wishlists tied to the event
    public ICollection<GiftExchangeModel> GiftExchanges { get; set; } = []; // Gift Exchange pairings for the event
    public ICollection<CustomPairingRuleModel> PairingRules { get; set; } = []; // Custom rules for pairing participants
}