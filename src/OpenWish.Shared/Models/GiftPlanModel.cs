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

    /// <summary>The occasion date and per-gift budget; set only when the shopper belongs to that event.</summary>
    public DateTimeOffset? EventDate { get; init; }
    public decimal? EventBudget { get; init; }
    public DateTimeOffset ReservedOn { get; init; }

    /// <summary>When the shopper marked the gift as bought; null while it is still to buy.</summary>
    public DateTimeOffset? PurchasedOn { get; init; }

    public bool IsPurchased => PurchasedOn is not null;

    public bool IsOverBudget => EventBudget is { } budget && Price is { } price && price > budget;
}

/// <summary>Everything the caller has reserved for one person.</summary>
public record GiftPlanRecipientModel
{
    public string RecipientName { get; init; } = string.Empty;
    public IReadOnlyList<GiftPlanItemModel> Items { get; init; } = [];

    public decimal Total => Items.Sum(item => item.Price ?? 0m);
    public int UnpricedCount => Items.Count(item => item.Price is null);
    public int ToBuyCount => Items.Count(item => !item.IsPurchased);
    public decimal ToBuyTotal => Items.Where(item => !item.IsPurchased).Sum(item => item.Price ?? 0m);
    public int OverBudgetCount => Items.Count(item => item.IsOverBudget);

    /// <summary>The soonest occasion still waiting on a gift, else the soonest dated occasion; null when no gift has one.</summary>
    public DateTimeOffset? NextOccasion =>
        Items.Where(item => !item.IsPurchased && item.EventDate is not null).Min(item => item.EventDate)
        ?? Items.Where(item => item.EventDate is not null).Min(item => item.EventDate);
}

/// <summary>A private overview of the gifts the caller has committed to buying.</summary>
public record GiftPlanModel
{
    public IReadOnlyList<GiftPlanRecipientModel> Recipients { get; init; } = [];

    /// <summary>Bought gifts whose occasion has passed, by recipient, most recent first.</summary>
    public IReadOnlyList<GiftPlanRecipientModel> History { get; init; } = [];

    public int HistoryCount => History.Sum(recipient => recipient.Items.Count);

    public int ItemCount => Recipients.Sum(recipient => recipient.Items.Count);
    public decimal Total => Recipients.Sum(recipient => recipient.Total);
    public int UnpricedCount => Recipients.Sum(recipient => recipient.UnpricedCount);
    public int ToBuyCount => Recipients.Sum(recipient => recipient.ToBuyCount);
    public int PurchasedCount => ItemCount - ToBuyCount;
    public decimal ToBuyTotal => Recipients.Sum(recipient => recipient.ToBuyTotal);
    public int OverBudgetCount => Recipients.Sum(recipient => recipient.OverBudgetCount);

    /// <summary>The same plan reduced to gifts that are still to buy, dropping people with nothing left.</summary>
    public IReadOnlyList<GiftPlanRecipientModel> RecipientsToBuy =>
        Recipients
            .Where(recipient => recipient.ToBuyCount > 0)
            .Select(recipient => recipient with { Items = recipient.Items.Where(item => !item.IsPurchased).ToList() })
            .ToList();
}

/// <summary>A gift the caller already marked as given to the same person as the item being viewed.</summary>
public record GivenGiftMatchModel
{
    public string RecipientName { get; init; } = string.Empty;
    public string ItemName { get; init; } = string.Empty;
    public DateTimeOffset PurchasedOn { get; init; }
}