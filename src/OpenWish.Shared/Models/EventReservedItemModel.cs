using System;

namespace OpenWish.Shared.Models;

public class EventReservedItemModel
{
    public int ItemId { get; init; }
    public string ItemPublicId { get; init; } = string.Empty;
    public string ItemName { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? Url { get; init; }
    public string? Image { get; init; }
    public string? WhereToBuy { get; init; }
    public decimal? Price { get; init; }
    public string WishlistName { get; init; } = string.Empty;
    public string WishlistPublicId { get; init; } = string.Empty;
    public string? WishlistOwnerId { get; init; }
    public string? WishlistOwnerName { get; init; }
    public string? WishlistOwnerEmail { get; init; }
    public DateTimeOffset ReservedOn { get; init; }
    public bool IsAnonymous { get; init; }
}
