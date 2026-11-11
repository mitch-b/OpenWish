using System.Globalization;

namespace OpenWish.Shared.Dashboard;

/// <summary>Describes when a notification arrived in words that do not depend on a time zone.</summary>
public static class NotificationTime
{
    public static string Describe(DateTimeOffset date, DateTimeOffset now)
    {
        var elapsed = now - date;
        if (elapsed < TimeSpan.FromMinutes(1))
        {
            return "Just now";
        }

        if (elapsed < TimeSpan.FromHours(1))
        {
            return $"{(int)elapsed.TotalMinutes} min ago";
        }

        if (elapsed < TimeSpan.FromDays(1))
        {
            var hours = (int)elapsed.TotalHours;
            return hours == 1 ? "1 hour ago" : $"{hours} hours ago";
        }

        if (elapsed < TimeSpan.FromDays(7))
        {
            var days = (int)elapsed.TotalDays;
            return days == 1 ? "1 day ago" : $"{days} days ago";
        }

        var format = date.Year == now.Year ? "MMM d" : "MMM d, yyyy";
        return date.UtcDateTime.ToString(format, CultureInfo.InvariantCulture);
    }
}