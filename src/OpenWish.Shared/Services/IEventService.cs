using OpenWish.Shared.Models;

namespace OpenWish.Shared.Services;

public interface IEventService
{
    Task<EventModel> CreateEventAsync(EventModel evt, string creatorId);
    Task<EventModel> GetEventAsync(int id);
    Task<EventModel> GetEventByPublicIdAsync(string publicId, string? requestingUserId = null);
    Task<IEnumerable<EventModel>> GetUserEventsAsync(string userId);
    Task<EventModel> UpdateEventAsync(int id, EventModel evt);
    Task<EventModel> UpdateEventByPublicIdAsync(string publicId, EventModel evt);
    Task DeleteEventAsync(int id);
    Task DeleteEventByPublicIdAsync(string publicId);
    Task<bool> AddUserToEventAsync(int eventId, string userId, string role = "Participant");
    Task<bool> AddUserToEventByPublicIdAsync(string eventPublicId, string userId, string role = "Participant");
    Task<bool> RemoveUserFromEventAsync(int eventId, string userId, string requestorId);
    Task<bool> RemoveUserFromEventByPublicIdAsync(string eventPublicId, string userId, string requestorId);
    Task<IEnumerable<WishlistModel>> GetEventWishlistsAsync(int eventId, string? requestingUserId = null);
    Task<IEnumerable<WishlistModel>> GetEventWishlistsByPublicIdAsync(string eventPublicId, string? requestingUserId = null);
    Task<WishlistModel> CreateEventWishlistAsync(int eventId, WishlistModel wishlistModel, string ownerId);
    Task<WishlistModel> CreateEventWishlistByPublicIdAsync(string eventPublicId, WishlistModel wishlistModel, string ownerId);
    Task<WishlistModel> AttachWishlistAsync(int eventId, int wishlistId, string userId);
    Task<WishlistModel> AttachWishlistByPublicIdAsync(string eventPublicId, string wishlistPublicId, string userId);
    Task<bool> DetachWishlistAsync(int eventId, int wishlistId, string userId);
    Task<bool> DetachWishlistByPublicIdAsync(string eventPublicId, string wishlistPublicId, string userId);
    Task<IEnumerable<EventReservedItemModel>> GetReservedItemsForUserByPublicIdAsync(string eventPublicId, string userId);

    // Event Invitation methods
    Task<EventUserModel> InviteUserToEventAsync(int eventId, string inviterId, string userId);
    Task<EventUserModel> InviteUserToEventByPublicIdAsync(string eventPublicId, string inviterId, string userId);
    Task<EventUserModel> InviteByEmailToEventAsync(int eventId, string inviterId, string email);
    Task<EventUserModel> InviteByEmailToEventByPublicIdAsync(string eventPublicId, string inviterId, string email);
    Task<IEnumerable<EventUserModel>> GetEventInvitationsAsync(int eventId);
    Task<IEnumerable<EventUserModel>> GetEventInvitationsByPublicIdAsync(string eventPublicId);
    Task<EventUserModel?> ClaimEventInvitationByEmailAsync(string eventPublicId, string userId, string? email);
    Task<bool> AcceptEventInvitationAsync(int eventUserId, string userId);
    Task<bool> RejectEventInvitationAsync(int eventUserId, string userId);
    Task<bool> CancelEventInvitationAsync(int eventUserId, string inviterId);
    Task<bool> ResendEventInvitationAsync(int eventUserId, string inviterId);

    // Gift Exchange methods
    Task<EventModel> DrawNamesAsync(int eventId, string ownerId);
    Task<EventModel> DrawNamesByPublicIdAsync(string eventPublicId, string ownerId);
    Task<EventModel> ResetGiftExchangeAsync(int eventId, string ownerId);
    Task<EventModel> ResetGiftExchangeByPublicIdAsync(string eventPublicId, string ownerId);
    Task<GiftExchangeModel?> GetMyGiftExchangeAsync(int eventId, string userId);
    Task<GiftExchangeModel?> GetMyGiftExchangeByPublicIdAsync(string eventPublicId, string userId);
    Task<IEnumerable<CustomPairingRuleModel>> GetPairingRulesAsync(int eventId);
    Task<IEnumerable<CustomPairingRuleModel>> GetPairingRulesByPublicIdAsync(string eventPublicId);
    Task<CustomPairingRuleModel> AddPairingRuleAsync(int eventId, CustomPairingRuleModel rule, string ownerId);
    Task<CustomPairingRuleModel> AddPairingRuleByPublicIdAsync(string eventPublicId, CustomPairingRuleModel rule, string ownerId);
    Task<bool> RemovePairingRuleAsync(int ruleId, string ownerId);
}