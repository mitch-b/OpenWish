namespace OpenWish.Shared.Models;

public class GiftExchangeModel : BaseEntityModel
{
    public int EventId { get; set; }
    public string GiverId { get; set; }
    public ApplicationUserModel? Giver { get; set; }
    public string ReceiverId { get; set; }
    public ApplicationUserModel? Receiver { get; set; }
    public bool IsAnonymous { get; set; }
    public string? ReceiverPreferences { get; set; }
    public decimal? Budget { get; set; }
}