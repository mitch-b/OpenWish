using Humanizer;

namespace OpenWish.Shared.Extensions;
public static class DateTimeOffsetExtensions
{
    public static DateTimeOffset? ToLocalTime(this DateTimeOffset? dateTimeOffset)
    {
        if (dateTimeOffset is null)
        {
            return null;
        }
        return dateTimeOffset.Value.ToOffset(TimeSpan.FromHours(TimeZoneInfo.Local.GetUtcOffset(dateTimeOffset.Value).Hours));
    }

    public static string? ToLocalTimeString(this DateTimeOffset? dateTimeOffset, string format = "g")
    {
        return ToLocalTime(dateTimeOffset)?.ToString(format);
    }

    public static string? ToLocalHumanizedString(this DateTimeOffset? dateTimeOffset)
    {
        return ToLocalHumanizedString(dateTimeOffset.Value);
    }

    public static string? ToLocalHumanizedString(this DateTimeOffset dateTimeOffset)
    {
        return ToLocalTime(dateTimeOffset).Humanize();
    }
}