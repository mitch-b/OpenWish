using System.Text.Json.Serialization;

namespace OpenWish.Shared.RequestModels;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum BulkWishlistItemAction
{
    SetPriority,
    MarkMostWanted,
    ClearMostWanted,
    Remove
}

public sealed class BulkUpdateWishlistItemsRequest
{
    public List<string> ItemPublicIds { get; init; } = [];
    public BulkWishlistItemAction Action { get; init; }
    public int? Priority { get; init; }
}