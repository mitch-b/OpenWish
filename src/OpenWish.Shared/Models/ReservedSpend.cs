namespace OpenWish.Shared.Models;

/// <summary>What a shopper's reserved gifts add up to, measured against an optional budget.</summary>
public record ReservedSpend(decimal Total, int UnpricedCount, decimal? Budget)
{
    public static ReservedSpend From(IEnumerable<decimal?> prices, decimal? budget)
    {
        var list = prices.ToList();
        return new ReservedSpend(list.Sum(price => price ?? 0m), list.Count(price => price is null), budget);
    }

    public bool HasBudget => Budget is not null;

    public decimal Remaining => (Budget ?? 0m) - Total;

    public bool IsOverBudget => Budget is { } budget && Total > budget;
}