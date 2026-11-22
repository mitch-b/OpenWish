using OpenWish.Shared.Models;

namespace OpenWish.Shared.Dashboard;

public static class DashboardEventOrder
{
    /// <summary>
    /// Events for the dashboard: today and later, soonest first. Past events appear only when nothing is ahead,
    /// most recent first, so a finished exchange never pushes the next one down.
    /// </summary>
    public static IEnumerable<EventModel> ForDashboard(IEnumerable<EventModel> events, DateTimeOffset now)
    {
        var active = events.Where(evt => evt is not null).ToList();
        var upcoming = active
            .Where(evt => DashboardWelcome.GetDaysUntil(evt.Date, now) >= 0)
            .OrderBy(evt => evt.Date)
            .ToList();
        return upcoming.Count > 0
            ? upcoming
            : active.OrderByDescending(evt => evt.Date);
    }

    public static string DescribeUpcoming(int upcomingCount) => upcomingCount switch
    {
        0 => "Nothing in the next 30 days.",
        1 => "1 event in the next 30 days.",
        _ => $"{upcomingCount} events in the next 30 days."
    };
}