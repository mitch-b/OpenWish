namespace OpenWish.Shared.Models;

public static class EventCountdown
{
    /// <summary>
    /// Describes how far away an occasion is by calendar day, or null when it has already passed.
    /// </summary>
    public static string? Describe(DateTimeOffset occasion, DateTimeOffset now)
    {
        var days = (occasion.Date - now.ToOffset(occasion.Offset).Date).Days;
        return days switch
        {
            < 0 => null,
            0 => "today",
            1 => "tomorrow",
            _ => $"in {days} days"
        };
    }
}