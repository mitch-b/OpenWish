namespace OpenWish.Data.Entities;

public class GiftExchange : BaseEntity
{
    public int EventId { get; set; }
    public Event Event { get; set; }

    public string GiverId { get; set; }
    public ApplicationUser Giver { get; set; }

    public string ReceiverId { get; set; }
    public ApplicationUser Receiver { get; set; }

    public bool IsAnonymous { get; set; } // Indicates if the pairing is anonymous
    public string ReceiverPreferences { get; set; } // Receiver's gift preferences or notes
    public decimal? Budget { get; set; } // Budget for this specific exchange
}