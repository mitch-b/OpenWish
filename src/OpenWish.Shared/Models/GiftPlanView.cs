namespace OpenWish.Shared.Models;

public enum GiftPlanFilter
{
    All,
    ToBuy,
    OverBudget
}

/// <summary>Narrows the gift plan to what the shopper is looking for.</summary>
public static class GiftPlanView
{
    /// <summary>
    /// Applies a filter and a search. A search matches a gift's name, its list, its occasion, or the person; a match
    /// on the person keeps all of that person's gifts that pass the filter. People left with no gifts are dropped.
    /// </summary>
    public static IReadOnlyList<GiftPlanRecipientModel> Apply(
        GiftPlanModel plan,
        GiftPlanFilter filter,
        string? search,
        DateTimeOffset now)
    {
        var term = search?.Trim();
        var narrowed = plan.Recipients
            .Select(recipient =>
            {
                var items = recipient.Items.Where(item => PassesFilter(item, filter));
                if (!string.IsNullOrEmpty(term) && !Contains(recipient.RecipientName, term))
                {
                    items = items.Where(item => Matches(item, term));
                }

                var kept = items.ToList();
                return kept.Count == recipient.Items.Count
                    ? recipient
                    : recipient with { Items = kept, GiftCount = recipient.GiftCount, BoughtCount = recipient.BoughtCount };
            })
            .Where(recipient => recipient.Items.Count > 0)
            .ToList();

        return filter == GiftPlanFilter.ToBuy ? GiftPlanModel.OrderByOccasion(narrowed, now) : narrowed;
    }

    private static bool PassesFilter(GiftPlanItemModel item, GiftPlanFilter filter) => filter switch
    {
        GiftPlanFilter.ToBuy => !item.IsPurchased,
        GiftPlanFilter.OverBudget => item.IsOverBudget,
        _ => true
    };

    private static bool Matches(GiftPlanItemModel item, string term) =>
        Contains(item.ItemName, term) || Contains(item.WishlistName, term) || Contains(item.EventName, term);

    private static bool Contains(string? text, string term) =>
        text?.Contains(term, StringComparison.CurrentCultureIgnoreCase) == true;
}