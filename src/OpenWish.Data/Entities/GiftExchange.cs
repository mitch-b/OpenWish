namespace OpenWish.Data.Entities;

public class GiftExchange : BaseEntity
{
    public int EventId { get; set; }
    public Event Event { get; set; }

    public int GiverId { get; set; }
    public User Giver { get; set; }

    public int ReceiverId { get; set; }
    public User Receiver { get; set; }

    public bool IsAnonymous { get; set; } // Indicates if the pairing is anonymous
    public string ReceiverPreferences { get; set; } // Receiver's gift preferences or notes
    public decimal? Budget { get; set; } // Budget for this specific exchange
}