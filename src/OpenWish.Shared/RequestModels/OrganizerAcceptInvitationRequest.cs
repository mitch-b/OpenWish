namespace OpenWish.Shared.RequestModels;

/// <summary>Marks a pending invitation as accepted on the invitee's behalf.</summary>
public class OrganizerAcceptInvitationRequest
{
    /// <summary>The name shown for an invitee without an account. Ignored for registered users.</summary>
    public string? DisplayName { get; set; }
}