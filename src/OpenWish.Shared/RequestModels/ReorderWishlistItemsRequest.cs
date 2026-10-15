namespace OpenWish.Shared.RequestModels;

public sealed class ReorderWishlistItemsRequest
{
    public List<string> OrderedItemPublicIds { get; init; } = [];
}