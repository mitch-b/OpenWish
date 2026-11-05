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

    /// <summary>
    /// How many gifts this person has overall, even when <see cref="Items"/> was narrowed by a filter or search, so
    /// progress stays truthful.
    /// </summary>
    public int GiftCount
    {
        get => _giftCount ?? Items.Count;
        init => _giftCount = value;
    }

    public int BoughtCount
    {
        get => _boughtCount ?? Items.Count(item => item.IsPurchased);
        init => _boughtCount = value;
    }

    private readonly int? _giftCount;
    private readonly int? _boughtCount;

    public decimal Total => Items.Sum(item => item.Price ?? 0m);
    public int UnpricedCount => Items.Count(item => item.Price is null);
    public int ToBuyCount => Items.Count(item => !item.IsPurchased);
    public decimal ToBuyTotal => Items.Where(item => !item.IsPurchased).Sum(item => item.Price ?? 0m);
    public int OverBudgetCount => Items.Count(item => item.IsOverBudget);

    /// <summary>
    /// The soonest upcoming occasion with a gift still to buy; failing that, the most recent overdue one, then
    /// any dated occasion. Null when no gift has one.
    /// </summary>
    public DateTimeOffset? GetNextOccasion(DateTimeOffset now)
    {
        var dated = Items.Where(item => item.EventDate is not null).ToList();
        var waiting = dated.Where(item => !item.IsPurchased).Select(item => item.EventDate!.Value).ToList();
        var upcoming = waiting.Where(date => !EventCountdown.IsPast(date, now)).ToList();
        if (upcoming.Count > 0)
        {
            return upcoming.Min();
        }

        return waiting.Count > 0 ? waiting.Max() : dated.Max(item => item.EventDate);
    }
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

    /// <summary>
    /// Recipients reduced to gifts still to buy, dropping people with nothing left and ordered again by the
    /// soonest occasion that remains.
    /// </summary>
    public IReadOnlyList<GiftPlanRecipientModel> GetRecipientsToBuy(DateTimeOffset now) =>
        OrderByOccasion(
            Recipients
                .Where(recipient => recipient.ToBuyCount > 0)
                .Select(recipient => recipient with { Items = recipient.Items.Where(item => !item.IsPurchased).ToList() }),
            now);

    /// <summary>Soonest occasion first, people without one last, then by name.</summary>
    public static IReadOnlyList<GiftPlanRecipientModel> OrderByOccasion(IEnumerable<GiftPlanRecipientModel> recipients, DateTimeOffset now) =>
        recipients
            .Select(recipient => (Recipient: recipient, Next: recipient.GetNextOccasion(now)))
            .OrderBy(entry => entry.Next is null)
            .ThenBy(entry => entry.Next)
            .ThenBy(entry => entry.Recipient.RecipientName, StringComparer.CurrentCultureIgnoreCase)
            .Select(entry => entry.Recipient)
            .ToList();
}

/// <summary>A gift the caller already marked as given to the same person as the item being viewed.</summary>
public record GivenGiftMatchModel
{
    public string RecipientName { get; init; } = string.Empty;
    public string ItemName { get; init; } = string.Empty;
    public DateTimeOffset PurchasedOn { get; init; }
}