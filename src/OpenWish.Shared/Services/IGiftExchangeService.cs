using OpenWish.Shared.Models;

namespace OpenWish.Shared.Services;

public interface IGiftExchangeService
{
    /// <summary>
    /// Draws names for a gift exchange event.
    /// </summary>
    /// <param name="eventId">The event ID.</param>
    /// <param name="ownerId">The ID of the user performing the operation (must be event owner).</param>
    /// <returns>The updated event model with gift exchanges created.</returns>
    Task<EventModel> DrawNamesAsync(int eventId, string ownerId);

    /// <summary>
    /// Draws names for a gift exchange event by public ID.
    /// </summary>
    /// <param name="eventPublicId">The event's public ID.</param>
    /// <param name="ownerId">The ID of the user performing the operation (must be event owner).</param>
    /// <returns>The updated event model with gift exchanges created.</returns>
    Task<EventModel> DrawNamesByPublicIdAsync(string eventPublicId, string ownerId);

    /// <summary>
    /// Resets the gift exchange for an event, clearing all assignments.
    /// </summary>
    /// <param name="eventId">The event ID.</param>
    /// <param name="ownerId">The ID of the user performing the operation (must be event owner).</param>
    /// <returns>The updated event model.</returns>
    Task<EventModel> ResetGiftExchangeAsync(int eventId, string ownerId);

    /// <summary>
    /// Resets the gift exchange for an event by public ID.
    /// </summary>
    /// <param name="eventPublicId">The event's public ID.</param>
    /// <param name="ownerId">The ID of the user performing the operation (must be event owner).</param>
    /// <returns>The updated event model.</returns>
    Task<EventModel> ResetGiftExchangeByPublicIdAsync(string eventPublicId, string ownerId);

    /// <summary>
    /// Gets the authenticated user's gift exchange assignment for an event.
    /// </summary>
    /// <param name="eventId">The event ID.</param>
    /// <param name="userId">The user ID.</param>
    /// <returns>The gift exchange assignment, or null if not found.</returns>
    Task<GiftExchangeModel?> GetMyGiftExchangeAsync(int eventId, string userId);

    /// <summary>
    /// Gets the authenticated user's gift exchange assignment by event public ID.
    /// </summary>
    /// <param name="eventPublicId">The event's public ID.</param>
    /// <param name="userId">The user ID.</param>
    /// <returns>The gift exchange assignment, or null if not found.</returns>
    Task<GiftExchangeModel?> GetMyGiftExchangeByPublicIdAsync(string eventPublicId, string userId);

    /// <summary>
    /// Gets all pairing rules for an event.
    /// </summary>
    /// <param name="eventId">The event ID.</param>
    /// <returns>Collection of pairing rules.</returns>
    Task<IEnumerable<CustomPairingRuleModel>> GetPairingRulesAsync(int eventId);

    /// <summary>
    /// Gets all pairing rules for an event by public ID.
    /// </summary>
    /// <param name="eventPublicId">The event's public ID.</param>
    /// <param name="requestorId">The ID of the user performing the operation (must be event owner).</param>
    /// <returns>Collection of pairing rules.</returns>
    Task<IEnumerable<CustomPairingRuleModel>> GetPairingRulesByPublicIdAsync(string eventPublicId, string requestorId);

    /// <summary>
    /// Adds a pairing rule (exclusion) to an event.
    /// </summary>
    /// <param name="eventId">The event ID.</param>
    /// <param name="rule">The pairing rule to add.</param>
    /// <param name="ownerId">The ID of the user performing the operation (must be event owner).</param>
    /// <returns>The created pairing rule model.</returns>
    Task<CustomPairingRuleModel> AddPairingRuleAsync(int eventId, CustomPairingRuleModel rule, string ownerId);

    /// <summary>
    /// Adds a pairing rule to an event by public ID.
    /// </summary>
    /// <param name="eventPublicId">The event's public ID.</param>
    /// <param name="rule">The pairing rule to add.</param>
    /// <param name="ownerId">The ID of the user performing the operation (must be event owner).</param>
    /// <returns>The created pairing rule model.</returns>
    Task<CustomPairingRuleModel> AddPairingRuleByPublicIdAsync(string eventPublicId, CustomPairingRuleModel rule, string ownerId);

    /// <summary>
    /// Removes a pairing rule from an event.
    /// </summary>
    /// <param name="ruleId">The ID of the rule to remove.</param>
    /// <param name="ownerId">The ID of the user performing the operation (must be event owner).</param>
    /// <returns>True if the rule was successfully removed, false otherwise.</returns>
    Task<bool> RemovePairingRuleAsync(int ruleId, string ownerId);
}