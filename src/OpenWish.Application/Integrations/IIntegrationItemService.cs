using OpenWish.Shared.Integrations;

namespace OpenWish.Application.Integrations;

public enum IntegrationWriteStatus
{
    Updated,
    Created,
    NotFound,
    WishlistNotAllowed,
    DuplicateExternalId
}

public sealed record IntegrationWriteResult(IntegrationWriteStatus Status, IntegrationItemModel? Item = null, string? ExistingItemId = null);

/// <summary>
/// What a connected app may do for the person who connected it: read the lists they allowed and
/// add, edit, and remove the ideas this app added. It never exposes reservations, purchases,
/// comments, or ideas the app did not add.
/// </summary>
public interface IIntegrationItemService
{
    Task<IntegrationProfileModel> GetProfileAsync(IntegrationCaller caller);

    Task<IReadOnlyList<IntegrationWishlistModel>> GetWishlistsAsync(IntegrationCaller caller);

    Task<IReadOnlyList<IntegrationItemModel>> GetItemsAsync(IntegrationCaller caller, string? wishlistId, string? externalId);

    Task<IntegrationItemModel?> GetItemAsync(IntegrationCaller caller, string itemId);

    Task<IntegrationWriteResult> CreateItemAsync(IntegrationCaller caller, IntegrationItemRequest request);

    Task<IntegrationWriteResult> UpdateItemAsync(IntegrationCaller caller, string itemId, IntegrationItemRequest request);

    Task<IntegrationWriteResult> UpsertItemByExternalIdAsync(IntegrationCaller caller, string externalId, IntegrationItemRequest request);

    Task<bool> DeleteItemAsync(IntegrationCaller caller, string itemId);

    Task<bool> DeleteItemByExternalIdAsync(IntegrationCaller caller, string externalId);
}