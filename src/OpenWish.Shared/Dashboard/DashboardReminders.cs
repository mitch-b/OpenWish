using OpenWish.Shared.Models;

namespace OpenWish.Shared.Dashboard;

public sealed record DashboardReminder(string Title, string Detail, string Href, string Icon, int DaysUntil);

/// <summary>In-app reminders about occasions that are close, derived from events the person already belongs to.</summary>
public static class DashboardReminders
{
    public const int DrawWindowDays = 14;
    public const int OccasionWindowDays = 7;
    public const int MaxReminders = 3;

    public static IReadOnlyList<DashboardReminder> For(IEnumerable<EventModel> events, string? userId, DateTimeOffset now)
    {
        var reminders = new List<DashboardReminder>();
        foreach (var evt in events.Where(evt => evt is not null && !string.IsNullOrWhiteSpace(evt.PublicId)))
        {
            var days = DashboardWelcome.GetDaysUntil(evt.Date, now);
            var when = DashboardWelcome.DescribeWhen(evt.Date, now);
            var isOrganizer = userId is not null && string.Equals(evt.CreatedBy?.Id, userId, StringComparison.Ordinal);

            if (isOrganizer && evt.IsGiftExchange && evt.NamesDrawnOn is null && days is >= 0 and <= DrawWindowDays)
            {
                reminders.Add(new DashboardReminder(
                    $"Draw names for {evt.Name}",
                    $"The exchange is {when} and names are not drawn yet.",
                    $"/events/{evt.PublicId}/manage",
                    "shuffle",
                    days));
            }
            else if (days is >= 0 and <= OccasionWindowDays)
            {
                reminders.Add(new DashboardReminder(
                    $"{evt.Name} is {when}",
                    evt.IsGiftExchange && evt.NamesDrawnOn is not null
                        ? "Check your match and finish your gift."
                        : "Open the event to see the plan.",
                    $"/events/{evt.PublicId}",
                    "calendar-event",
                    days));
            }
        }

        return reminders.OrderBy(reminder => reminder.DaysUntil).ThenBy(reminder => reminder.Title, StringComparer.Ordinal).Take(MaxReminders).ToList();
    }
}