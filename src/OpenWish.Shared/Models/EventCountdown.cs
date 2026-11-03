using OpenWish.Shared.Dashboard;

namespace OpenWish.Shared.Models;

public static class EventCountdown
{
    /// <summary>
    /// Describes how far away an occasion is by calendar day in the shopper's time zone, or null when it
    /// has already passed.
    /// </summary>
    public static string? Describe(DateTimeOffset occasion, DateTimeOffset now, TimeZoneInfo shopperTimeZone)
    {
        var days = (TimeZoneInfo.ConvertTime(occasion, shopperTimeZone).Date
            - TimeZoneInfo.ConvertTime(now, shopperTimeZone).Date).Days;
        return days switch
        {
            < 0 => null,
            0 => "today",
            1 => "tomorrow",
            _ => $"in {days} days"
        };
    }

    /// <summary>A short badge label counted on the event's own calendar, matching the date shown on its card.</summary>
    public static string Badge(DateTimeOffset date, DateTimeOffset now)
    {
        var days = DashboardWelcome.GetDaysUntil(date, now);
        return days switch
        {
            0 => "Today",
            1 => "Tomorrow",
            > 1 and < 60 => $"In {days} days",
            >= 60 => $"In {days / 30} months",
            -1 => "Yesterday",
            _ => "Past event"
        };
    }

    public static bool IsPast(DateTimeOffset date, DateTimeOffset now) => DashboardWelcome.GetDaysUntil(date, now) < 0;
}