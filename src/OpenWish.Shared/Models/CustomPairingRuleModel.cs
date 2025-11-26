namespace OpenWish.Shared.Models;

public class CustomPairingRuleModel : BaseEntityModel
{
    public int EventId { get; set; }
    public string? SourceUserId { get; set; }
    public string? SourceInviteeEmail { get; set; }
    public ApplicationUserModel? SourceUser { get; set; }
    public string? TargetUserId { get; set; }
    public string? TargetInviteeEmail { get; set; }
    public ApplicationUserModel? TargetUser { get; set; }
    public string RuleType { get; set; } // "Exclusion", "MandatoryPairing", "CustomBudget"
    public string? RuleDescription { get; set; }
}