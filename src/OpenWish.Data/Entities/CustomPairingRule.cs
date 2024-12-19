namespace OpenWish.Data.Entities;

public class CustomPairingRule : BaseEntity
{
    public int EventId { get; set; }
    public Event Event { get; set; }

    public int? SourceUserId { get; set; } // The user the rule applies to (e.g., who is excluded or must give to someone)
    public OpenWishUser SourceOpenWishUser { get; set; }

    public int? TargetUserId { get; set; } // The target of the rule (e.g., who is excluded from or must receive a gift from SourceUser)
    public OpenWishUser TargetOpenWishUser { get; set; }

    public string RuleType { get; set; } // e.g., "Exclusion", "MandatoryPairing", "CustomBudget"

    public string RuleDescription { get; set; } // Additional description for custom logic
}