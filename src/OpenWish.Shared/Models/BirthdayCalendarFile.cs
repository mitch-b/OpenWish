using System.Text;

namespace OpenWish.Shared.Models;

/// <summary>Builds a yearly-recurring iCalendar file from friends' birthdays the caller can already see. No birth year is included.</summary>
public static class BirthdayCalendarFile
{
    public const string FileName = "openwish-birthdays.ics";

    public static string BuildIcs(IEnumerable<UpcomingBirthdayModel> birthdays, string appUrl, DateOnly today, DateTimeOffset stamp)
    {
        var lines = new List<string>
        {
            "BEGIN:VCALENDAR",
            "VERSION:2.0",
            "PRODID:-//OpenWish//Birthdays//EN",
            "CALSCALE:GREGORIAN",
            "METHOD:PUBLISH",
            "X-WR-CALNAME:OpenWish birthdays"
        };

        foreach (var birthday in birthdays.Where(b => BirthdayCalendar.IsValid(b.Month, b.Day)))
        {
            var start = BirthdayCalendar.NextOccurrence(birthday.Month, birthday.Day, today);
            var link = birthday.WishlistPublicId is null
                ? appUrl
                : $"{appUrl.TrimEnd('/')}/wishlists/{Uri.EscapeDataString(birthday.WishlistPublicId)}";
            lines.Add("BEGIN:VEVENT");
            lines.Add($"UID:birthday-{birthday.Month:00}{birthday.Day:00}-{Uid(birthday.UserName)}@openwish");
            lines.Add($"DTSTAMP:{stamp.UtcDateTime:yyyyMMdd'T'HHmmss'Z'}");
            lines.Add($"DTSTART;VALUE=DATE:{start:yyyyMMdd}");
            lines.Add($"DTEND;VALUE=DATE:{start.AddDays(1):yyyyMMdd}");
            lines.Add(birthday.Month == 2 && birthday.Day == 29 ? "RRULE:FREQ=YEARLY;BYMONTH=2;BYMONTHDAY=-1" : "RRULE:FREQ=YEARLY");
            lines.Add($"SUMMARY:{EventCalendar.Escape($"{birthday.UserName}'s birthday")}");
            lines.Add($"DESCRIPTION:{EventCalendar.Escape($"See gift ideas on OpenWish: {link}")}");
            lines.Add($"URL:{link}");
            lines.Add("TRANSP:TRANSPARENT");
            lines.Add("END:VEVENT");
        }

        lines.Add("END:VCALENDAR");
        var builder = new StringBuilder();
        foreach (var line in lines)
        {
            builder.Append(EventCalendar.Fold(line)).Append("\r\n");
        }

        return builder.ToString();
    }

    private static string Uid(string name)
    {
        var safe = new string(name.Select(c => char.IsLetterOrDigit(c) ? char.ToLowerInvariant(c) : '-').ToArray()).Trim('-');
        return safe.Length == 0 ? "friend" : safe;
    }
}