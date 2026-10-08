using OpenWish.Shared.Models;

namespace OpenWish.Shared.Services;

/// <summary>
/// Service for managing events, event memberships, wishlists within events, and gift exchanges.
/// Supports both internal ID and public ID-based access patterns for authorization and sharing.
/// </summary>
public interface IEventService
{
    /// <summary>Creates a new event owned by the specified user.</summary>
    /// <param name="evt">The event model containing name, description, and other details.</param>
    /// <param name="creatorId">The user ID of the event creator.</param>
    /// <returns>The created event model with generated public ID.</returns>
    Task<EventModel> CreateEventAsync(EventModel evt, string creatorId);

    /// <summary>Retrieves an event by its internal ID.</summary>
    /// <param name="id">The internal event ID.</param>
    /// <returns>The event model.</returns>
    Task<EventModel> GetEventAsync(int id);

    /// <summary>Retrieves an event by its public ID, verifying the requestor is a participant.</summary>
    /// <param name="publicId">The public event identifier.</param>
    /// <param name="requestingUserId">The user ID requesting the event data.</param>
    /// <returns>The event model if the user is an event participant.</returns>
    Task<EventModel> GetEventByPublicIdAsync(string publicId, string requestingUserId);

    /// <summary>Lists all events associated with the specified user.</summary>
    /// <param name="userId">The user ID to fetch events for.</param>
    /// <returns>Collection of event models where the user is a participant or owner.</returns>
    Task<IEnumerable<EventModel>> GetUserEventsAsync(string userId);

    /// <summary>Updates an event by its internal ID.</summary>
    /// <param name="id">The internal event ID.</param>
    /// <param name="evt">The updated event model.</param>
    /// <param name="requestorId">The user ID requesting the update (must be event owner).</param>
    /// <returns>The updated event model.</returns>
    Task<EventModel> UpdateEventAsync(int id, EventModel evt, string requestorId);

    /// <summary>Updates an event by its public ID, verifying the requestor is the owner.</summary>
    /// <param name="publicId">The public event identifier.</param>
    /// <param name="evt">The updated event model.</param>
    /// <param name="requestorId">The user ID requesting the update (must be event owner).</param>
    /// <returns>The updated event model.</returns>
    Task<EventModel> UpdateEventByPublicIdAsync(string publicId, EventModel evt, string requestorId);

    /// <summary>Soft-deletes an event by its internal ID.</summary>
    /// <param name="id">The internal event ID.</param>
    /// <param name="requestorId">The user ID requesting deletion (must be event owner).</param>
    Task DeleteEventAsync(int id, string requestorId);

    /// <summary>Soft-deletes an event by its public ID.</summary>
    /// <param name="publicId">The public event identifier.</param>
    /// <param name="requestorId">The user ID requesting deletion (must be event owner).</param>
    Task DeleteEventByPublicIdAsync(string publicId, string requestorId);

    /// <summary>Adds a user to an event with the specified role by internal event ID.</summary>
    /// <param name="eventId">The internal event ID.</param>
    /// <param name="userId">The user ID to add to the event.</param>
    /// <param name="requestorId">The user ID requesting the action (must have permission).</param>
    /// <param name="role">The role to assign (e.g., "Participant", "Organizer").</param>
    /// <returns>True if the user was successfully added.</returns>
    Task<bool> AddUserToEventAsync(int eventId, string userId, string requestorId, string role = "Participant");

    /// <summary>Adds a user to an event by its public ID.</summary>
    /// <param name="eventPublicId">The public event identifier.</param>
    /// <param name="userId">The user ID to add to the event.</param>
    /// <param name="requestorId">The user ID requesting the action (must have permission).</param>
    /// <param name="role">The role to assign (e.g., "Participant", "Organizer").</param>
    /// <returns>True if the user was successfully added.</returns>
    Task<bool> AddUserToEventByPublicIdAsync(string eventPublicId, string userId, string requestorId, string role = "Participant");

    /// <summary>Removes a user from an event by internal event ID.</summary>
    /// <param name="eventId">The internal event ID.</param>
    /// <param name="userId">The user ID to remove from the event.</param>
    /// <param name="requestorId">The user ID requesting the action (must have permission).</param>
    /// <returns>True if the user was successfully removed.</returns>
    Task<bool> RemoveUserFromEventAsync(int eventId, string userId, string requestorId);

    /// <summary>Removes a user from an event by its public ID.</summary>
    /// <param name="eventPublicId">The public event identifier.</param>
    /// <param name="userId">The user ID to remove from the event.</param>
    /// <param name="requestorId">The user ID requesting the action (must have permission).</param>
    /// <returns>True if the user was successfully removed.</returns>
    Task<bool> RemoveUserFromEventByPublicIdAsync(string eventPublicId, string userId, string requestorId);

    /// <summary>Lists all wishlists associated with an event by its internal ID.</summary>
    /// <param name="eventId">The internal event ID.</param>
    /// <param name="requestingUserId">Optional user ID for access-level filtering.</param>
    /// <returns>Collection of wishlist models linked to the event.</returns>
    Task<IEnumerable<WishlistModel>> GetEventWishlistsAsync(int eventId, string? requestingUserId = null);

    /// <summary>Lists all wishlists associated with an event by its public ID.</summary>
    /// <param name="eventPublicId">The public event identifier.</param>
    /// <param name="requestingUserId">Optional user ID for access-level filtering.</param>
    /// <returns>Collection of wishlist models linked to the event.</returns>
    Task<IEnumerable<WishlistModel>> GetEventWishlistsByPublicIdAsync(string eventPublicId, string? requestingUserId = null);

    /// <summary>Creates a new wishlist directly within an event by internal event ID.</summary>
    /// <param name="eventId">The internal event ID.</param>
    /// <param name="wishlistModel">The wishlist model to create.</param>
    /// <param name="ownerId">The user ID of the wishlist owner.</param>
    /// <returns>The created wishlist model.</returns>
    Task<WishlistModel> CreateEventWishlistAsync(int eventId, WishlistModel wishlistModel, string ownerId);

    /// <summary>Creates a new wishlist directly within an event by its public ID.</summary>
    /// <param name="eventPublicId">The public event identifier.</param>
    /// <param name="wishlistModel">The wishlist model to create.</param>
    /// <param name="ownerId">The user ID of the wishlist owner.</param>
    /// <returns>The created wishlist model.</returns>
    Task<WishlistModel> CreateEventWishlistByPublicIdAsync(string eventPublicId, WishlistModel wishlistModel, string ownerId);

    /// <summary>Attaches an existing wishlist to an event by internal IDs.</summary>
    /// <param name="eventId">The internal event ID.</param>
    /// <param name="wishlistId">The internal wishlist ID to attach.</param>
    /// <param name="userId">The user ID requesting the attachment (must have permission).</param>
    /// <returns>The attached wishlist model.</returns>
    Task<WishlistModel> AttachWishlistAsync(int eventId, int wishlistId, string userId);

    /// <summary>Attaches an existing wishlist to an event by public IDs.</summary>
    /// <param name="eventPublicId">The public event identifier.</param>
    /// <param name="wishlistPublicId">The public wishlist identifier.</param>
    /// <param name="userId">The user ID requesting the attachment (must have permission).</param>
    /// <returns>The attached wishlist model.</returns>
    Task<WishlistModel> AttachWishlistByPublicIdAsync(string eventPublicId, string wishlistPublicId, string userId);

    /// <summary>Removes a wishlist from an event by internal IDs.</summary>
    /// <param name="eventId">The internal event ID.</param>
    /// <param name="wishlistId">The internal wishlist ID to detach.</param>
    /// <param name="userId">The user ID requesting the detachment (must have permission).</param>
    /// <returns>True if the wishlist was successfully detached.</returns>
    Task<bool> DetachWishlistAsync(int eventId, int wishlistId, string userId);

    /// <summary>Removes a wishlist from an event by public IDs.</summary>
    /// <param name="eventPublicId">The public event identifier.</param>
    /// <param name="wishlistPublicId">The public wishlist identifier.</param>
    /// <param name="userId">The user ID requesting the detachment (must have permission).</param>
    /// <returns>True if the wishlist was successfully detached.</returns>
    Task<bool> DetachWishlistByPublicIdAsync(string eventPublicId, string wishlistPublicId, string userId);

    /// <summary>Lists all items reserved by a user within an event, useful for shopping workflow.</summary>
    /// <param name="eventPublicId">The public event identifier.</param>
    /// <param name="userId">The user ID to fetch reservations for.</param>
    /// <returns>Collection of reserved items across event wishlists.</returns>
    Task<IEnumerable<EventReservedItemModel>> GetReservedItemsForUserByPublicIdAsync(string eventPublicId, string userId);

    /// <summary>Sends an event invitation to an existing platform user by internal event ID.</summary>
    /// <param name="eventId">The internal event ID.</param>
    /// <param name="inviterId">The user ID sending the invitation.</param>
    /// <param name="userId">The user ID to invite.</param>
    /// <returns>The event user model representing the invitation.</returns>
    Task<EventUserModel> InviteUserToEventAsync(int eventId, string inviterId, string userId);

    /// <summary>Sends an event invitation to an existing platform user by public event ID.</summary>
    /// <param name="eventPublicId">The public event identifier.</param>
    /// <param name="inviterId">The user ID sending the invitation.</param>
    /// <param name="userId">The user ID to invite.</param>
    /// <returns>The event user model representing the invitation.</returns>
    Task<EventUserModel> InviteUserToEventByPublicIdAsync(string eventPublicId, string inviterId, string userId);

    /// <summary>Sends an event invitation to an email address (user may not yet be registered) by internal event ID.</summary>
    /// <param name="eventId">The internal event ID.</param>
    /// <param name="inviterId">The user ID sending the invitation.</param>
    /// <param name="email">The email address to invite.</param>
    /// <returns>The event user model representing the pending invitation.</returns>
    Task<EventUserModel> InviteByEmailToEventAsync(int eventId, string inviterId, string email);

    /// <summary>Sends an event invitation to an email address by public event ID.</summary>
    /// <param name="eventPublicId">The public event identifier.</param>
    /// <param name="inviterId">The user ID sending the invitation.</param>
    /// <param name="email">The email address to invite.</param>
    /// <returns>The event user model representing the pending invitation.</returns>
    Task<EventUserModel> InviteByEmailToEventByPublicIdAsync(string eventPublicId, string inviterId, string email);

    /// <summary>Lists all invitations (pending or accepted) for an event by internal ID.</summary>
    /// <param name="eventId">The internal event ID.</param>
    /// <returns>Collection of event user models for all participants and invites.</returns>
    Task<IEnumerable<EventUserModel>> GetEventInvitationsAsync(int eventId);

    /// <summary>Lists all invitations for an event by its public ID, with access control.</summary>
    /// <param name="eventPublicId">The public event identifier.</param>
    /// <param name="requestorId">The user ID requesting the list (must have permission).</param>
    /// <returns>Collection of event user models for all participants and invites.</returns>
    Task<IEnumerable<EventUserModel>> GetEventInvitationsByPublicIdAsync(string eventPublicId, string requestorId);

    /// <summary>Claims an event invitation by matching email to a newly registered user.</summary>
    /// <param name="eventPublicId">The public event identifier.</param>
    /// <param name="userId">The newly registered user ID.</param>
    /// <param name="email">The email address used in the invitation (for verification).</param>
    /// <returns>The claimed event user model, or null if no matching invitation exists.</returns>
    Task<EventUserModel?> ClaimEventInvitationByEmailAsync(string eventPublicId, string userId, string? email);

    /// <summary>Accepts an event invitation by internal invitation ID.</summary>
    /// <param name="eventUserId">The internal event user ID (invitation ID).</param>
    /// <param name="userId">The user ID accepting the invitation (must match the invite recipient).</param>
    /// <returns>True if the invitation was successfully accepted.</returns>
    Task<bool> AcceptEventInvitationAsync(int eventUserId, string userId);

    /// <summary>Accepts an event invitation by public ID.</summary>
    /// <param name="eventUserPublicId">The public event user identifier (invitation ID).</param>
    /// <param name="userId">The user ID accepting the invitation (must match the invite recipient).</param>
    /// <returns>True if the invitation was successfully accepted.</returns>
    Task<bool> AcceptEventInvitationByPublicIdAsync(string eventUserPublicId, string userId);

    /// <summary>Rejects an event invitation by internal invitation ID.</summary>
    /// <param name="eventUserId">The internal event user ID (invitation ID).</param>
    /// <param name="userId">The user ID rejecting the invitation (must match the invite recipient).</param>
    /// <returns>True if the invitation was successfully rejected.</returns>
    Task<bool> RejectEventInvitationAsync(int eventUserId, string userId);

    /// <summary>Rejects an event invitation by public ID.</summary>
    /// <param name="eventUserPublicId">The public event user identifier (invitation ID).</param>
    /// <param name="userId">The user ID rejecting the invitation (must match the invite recipient).</param>
    /// <returns>True if the invitation was successfully rejected.</returns>
    Task<bool> RejectEventInvitationByPublicIdAsync(string eventUserPublicId, string userId);

    /// <summary>Cancels a pending event invitation by internal invitation ID.</summary>
    /// <param name="eventUserId">The internal event user ID (invitation ID).</param>
    /// <param name="inviterId">The user ID canceling the invitation (must be the original inviter).</param>
    /// <returns>True if the invitation was successfully canceled.</returns>
    Task<bool> CancelEventInvitationAsync(int eventUserId, string inviterId);

    /// <summary>Resends an event invitation to the original recipient.</summary>
    /// <param name="eventUserId">The internal event user ID (invitation ID).</param>
    /// <param name="inviterId">The user ID resending the invitation (must be the original inviter).</param>
    /// <returns>True if the invitation was successfully resent.</returns>
    Task<bool> ResendEventInvitationAsync(int eventUserId, string inviterId);

    /// <summary>Lists all pending invitations (not yet accepted or rejected) for a user.</summary>
    /// <param name="userId">The user ID to fetch pending invitations for.</param>
    /// <returns>Collection of event user models for pending invitations.</returns>
    Task<IEnumerable<EventUserModel>> GetPendingInvitationsForUserAsync(string userId);

    /// <summary>Draws (assigns) gift exchange pairings for an event by internal ID, randomly assigning who buys for whom.</summary>
    /// <param name="eventId">The internal event ID.</param>
    /// <param name="ownerId">The user ID requesting the draw (must be event owner).</param>
    /// <returns>The event model with updated gift exchange pairings.</returns>
    Task<EventModel> DrawNamesAsync(int eventId, string ownerId);

    /// <summary>Draws gift exchange pairings for an event by its public ID.</summary>
    /// <param name="eventPublicId">The public event identifier.</param>
    /// <param name="ownerId">The user ID requesting the draw (must be event owner).</param>
    /// <returns>The event model with updated gift exchange pairings.</returns>
    Task<EventModel> DrawNamesByPublicIdAsync(string eventPublicId, string ownerId);

    /// <summary>Clears all gift exchange pairings for an event by internal ID.</summary>
    /// <param name="eventId">The internal event ID.</param>
    /// <param name="ownerId">The user ID requesting the reset (must be event owner).</param>
    /// <returns>The event model with cleared gift exchange data.</returns>
    Task<EventModel> ResetGiftExchangeAsync(int eventId, string ownerId);

    /// <summary>Clears all gift exchange pairings for an event by its public ID.</summary>
    /// <param name="eventPublicId">The public event identifier.</param>
    /// <param name="ownerId">The user ID requesting the reset (must be event owner).</param>
    /// <returns>The event model with cleared gift exchange data.</returns>
    Task<EventModel> ResetGiftExchangeByPublicIdAsync(string eventPublicId, string ownerId);

    /// <summary>Retrieves the current user's gift exchange assignment (who they are buying for) by internal event ID.</summary>
    /// <param name="eventId">The internal event ID.</param>
    /// <param name="userId">The user ID requesting their assignment.</param>
    /// <returns>The gift exchange model with recipient info, or null if exchange not yet drawn or user not assigned.</returns>
    Task<GiftExchangeModel?> GetMyGiftExchangeAsync(int eventId, string userId);

    /// <summary>Retrieves the current user's gift exchange assignment by public event ID.</summary>
    /// <param name="eventPublicId">The public event identifier.</param>
    /// <param name="userId">The user ID requesting their assignment.</param>
    /// <returns>The gift exchange model with recipient info, or null if exchange not yet drawn or user not assigned.</returns>
    Task<GiftExchangeModel?> GetMyGiftExchangeByPublicIdAsync(string eventPublicId, string userId);

    /// <summary>Lists all custom pairing rules configured for an event by internal ID.</summary>
    /// <param name="eventId">The internal event ID.</param>
    /// <returns>Collection of pairing rule models used to constrain gift exchange draws.</returns>
    Task<IEnumerable<CustomPairingRuleModel>> GetPairingRulesAsync(int eventId);

    /// <summary>Lists all custom pairing rules configured for an event by its public ID.</summary>
    /// <param name="eventPublicId">The public event identifier.</param>
    /// <param name="requestorId">The user ID requesting the rules (must have permission).</param>
    /// <returns>Collection of pairing rule models used to constrain gift exchange draws.</returns>
    Task<IEnumerable<CustomPairingRuleModel>> GetPairingRulesByPublicIdAsync(string eventPublicId, string requestorId);

    /// <summary>Adds a constraint rule to prevent certain pairings in a gift exchange by internal event ID.</summary>
    /// <param name="eventId">The internal event ID.</param>
    /// <param name="rule">The pairing rule model (e.g., prevent user A from buying for user B).</param>
    /// <param name="ownerId">The user ID adding the rule (must be event owner).</param>
    /// <returns>The created pairing rule model.</returns>
    Task<CustomPairingRuleModel> AddPairingRuleAsync(int eventId, CustomPairingRuleModel rule, string ownerId);

    /// <summary>Adds a constraint rule to prevent certain pairings in a gift exchange by public event ID.</summary>
    /// <param name="eventPublicId">The public event identifier.</param>
    /// <param name="rule">The pairing rule model (e.g., prevent user A from buying for user B).</param>
    /// <param name="ownerId">The user ID adding the rule (must be event owner).</param>
    /// <returns>The created pairing rule model.</returns>
    Task<CustomPairingRuleModel> AddPairingRuleByPublicIdAsync(string eventPublicId, CustomPairingRuleModel rule, string ownerId);

    /// <summary>Removes a pairing rule by internal rule ID.</summary>
    /// <param name="ruleId">The internal pairing rule ID.</param>
    /// <param name="ownerId">The user ID removing the rule (must be event owner).</param>
    /// <returns>True if the rule was successfully removed.</returns>
    Task<bool> RemovePairingRuleAsync(int ruleId, string ownerId);
}