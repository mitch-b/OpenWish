namespace OpenWish.Shared.RequestModels;

public sealed class TransferWishlistItemsRequest
{
    public List<string> ItemPublicIds { get; init; } = [];
    public string TargetWishlistPublicId { get; init; } = string.Empty;
    public bool Copy { get; init; }
}