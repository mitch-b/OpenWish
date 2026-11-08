namespace OpenWish.Data.Entities;

public class GiftExchange : BaseEntity
{
    public int EventId { get; set; }
    public Event Event { get; set; }

    public string? GiverId { get; set; }
    public ApplicationUser? Giver { get; set; }
    public string? GiverEmail { get; set; }
    public int? GiverParticipantId { get; set; }
    public EventUser? GiverParticipant { get; set; }

    public string? ReceiverId { get; set; }
    public ApplicationUser? Receiver { get; set; }
    public string? ReceiverEmail { get; set; }
    public int? ReceiverParticipantId { get; set; }
    public EventUser? ReceiverParticipant { get; set; }

    public bool IsAnonymous { get; set; } // Indicates if the pairing is anonymous
    public string ReceiverPreferences { get; set; } // Receiver's gift preferences or notes
    public decimal? Budget { get; set; } // Budget for this specific exchange
    public DateTimeOffset? MatchOpenedOn { get; set; }
}