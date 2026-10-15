namespace OpenWish.Shared.RequestModels;

public sealed class ReorderWishlistItemsRequest
{
    public List<int> OrderedItemIds { get; init; } = [];
}