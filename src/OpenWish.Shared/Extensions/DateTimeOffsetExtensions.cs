using Humanizer;

namespace OpenWish.Shared.Extensions;

public static class DateTimeOffsetExtensions
{
    public static DateTimeOffset? ToLocalTime(this DateTimeOffset? dateTimeOffset)
    {
        return dateTimeOffset?.ToLocalTime();
    }

    public static string? ToLocalTimeString(this DateTimeOffset? dateTimeOffset, string format = "g")
    {
        return ToLocalTime(dateTimeOffset)?.ToString(format);
    }

    public static string? ToLocalHumanizedString(this DateTimeOffset? dateTimeOffset)
    {
        return dateTimeOffset?.ToLocalHumanizedString();
    }

    public static string? ToLocalHumanizedString(this DateTimeOffset dateTimeOffset)
    {
        return ToLocalTime(dateTimeOffset).Humanize();
    }
}