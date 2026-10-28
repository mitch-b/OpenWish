namespace OpenWish.Shared.Models;

/// <summary>One gift the caller has reserved for someone else.</summary>
public record GiftPlanItemModel
{
    public string ItemPublicId { get; init; } = string.Empty;
    public string ItemName { get; init; } = string.Empty;
    public decimal? Price { get; init; }
    public string? Url { get; init; }
    public string? Image { get; init; }
    public string WishlistName { get; init; } = string.Empty;
    public string WishlistPublicId { get; init; } = string.Empty;

    /// <summary>The occasion the wishlist belongs to, when it belongs to one.</summary>
    public string? EventName { get; init; }
    public string? EventPublicId { get; init; }
    public DateTimeOffset ReservedOn { get; init; }
}

/// <summary>Everything the caller has reserved for one person.</summary>
public record GiftPlanRecipientModel
{
    public string RecipientName { get; init; } = string.Empty;
    public IReadOnlyList<GiftPlanItemModel> Items { get; init; } = [];

    public decimal Total => Items.Sum(item => item.Price ?? 0m);
    public int UnpricedCount => Items.Count(item => item.Price is null);
}

/// <summary>A private overview of the gifts the caller has committed to buying.</summary>
public record GiftPlanModel
{
    public IReadOnlyList<GiftPlanRecipientModel> Recipients { get; init; } = [];

    public int ItemCount => Recipients.Sum(recipient => recipient.Items.Count);
    public decimal Total => Recipients.Sum(recipient => recipient.Total);
    public int UnpricedCount => Recipients.Sum(recipient => recipient.UnpricedCount);
}