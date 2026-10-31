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
}