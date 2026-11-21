namespace OpenWish.Shared.Models;

/// <summary>Formats the ideas a viewer can already see as CSV. Reservations, purchases, and comments are never included.</summary>
public static class WishlistCsv
{
    private static readonly string[] _header = ["Idea", "Price", "Where to buy", "Link", "Priority", "Most wanted", "Notes"];

    public static string Build(IEnumerable<WishlistItemModel> items) =>
        CsvFile.Build(
            _header,
            items
                .Where(item => !item.IsHiddenFromOwner && item.ReceivedOn is null && item.ArchivedOn is null)
                .Select(item => (IReadOnlyList<string?>)
                [
                    item.Name,
                    CsvFile.Price(item.Price),
                    item.WhereToBuy,
                    item.Url?.Trim(),
                    item.Priority switch { 1 => "High", 2 => "Medium", 3 => "Low", _ => string.Empty },
                    item.IsPinned ? "Yes" : string.Empty,
                    item.Description
                ]));
}