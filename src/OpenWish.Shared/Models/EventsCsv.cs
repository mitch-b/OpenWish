namespace OpenWish.Shared.Models;

/// <summary>Formats the events a person belongs to as CSV. Draw results, participants, and emails are never included.</summary>
public static class EventsCsv
{
    private static readonly string[] _header = ["Event", "Date", "Budget", "Gift exchange", "Names drawn", "Organizer", "Description"];

    public static string Build(IEnumerable<EventModel> events) =>
        CsvFile.Build(
            _header,
            events
                .OrderBy(evt => evt.Date)
                .Select(evt => (IReadOnlyList<string?>)
                [
                    evt.Name,
                    CsvFile.Date(evt.Date),
                    CsvFile.Price(evt.Budget),
                    evt.IsGiftExchange ? "Yes" : "No",
                    evt.NamesDrawnOn is null ? "No" : "Yes",
                    PublicDisplayName.From(evt.CreatedBy?.UserName, string.Empty),
                    evt.Description
                ]));
}