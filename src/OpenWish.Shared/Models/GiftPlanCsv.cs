namespace OpenWish.Shared.Models;

/// <summary>Formats the caller's own gift plan as CSV for a budget spreadsheet. Only the shopper ever sees this data.</summary>
public static class GiftPlanCsv
{
    private static readonly string[] _header =
        ["Recipient", "Gift", "Price", "Budget", "Status", "Occasion", "Occasion date", "Wishlist", "Link"];

    public static string Build(GiftPlanModel plan, DateTimeOffset now) =>
        CsvFile.Build(
            _header,
            GiftPlanModel.OrderByOccasion(plan.Recipients, now)
                .SelectMany(recipient => recipient.Items.Select(item => (IReadOnlyList<string?>)
                [
                    recipient.RecipientName,
                    item.ItemName,
                    CsvFile.Price(item.Price),
                    CsvFile.Price(item.EventBudget),
                    item.IsWrapped ? "Wrapped" : item.IsPurchased ? "Bought" : "To buy",
                    item.EventName,
                    CsvFile.Date(item.EventDate),
                    item.WishlistName,
                    item.Url?.Trim()
                ])));
}