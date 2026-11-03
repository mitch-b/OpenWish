using System.Text;

namespace OpenWish.Shared.Models;

/// <summary>Builds an iCalendar file for an occasion. Only details every participant can already see are included.</summary>
public static class EventCalendar
{
    /// <summary>The organizer and accepted participants may download the file; a pending invitee may not.</summary>
    public static bool CanDownload(EventModel evt, string userId) =>
        string.Equals(evt.CreatedBy?.Id, userId, StringComparison.Ordinal) ||
        (evt.EventUsers ?? []).Any(participant =>
            !participant.Deleted &&
            string.Equals(participant.UserId, userId, StringComparison.Ordinal) &&
            string.Equals(participant.Status, "Accepted", StringComparison.OrdinalIgnoreCase));

    public static string FileName(EventModel evt)
    {
        var safe = new string((evt.Name ?? string.Empty)
            .Select(character => char.IsLetterOrDigit(character) ? char.ToLowerInvariant(character) : '-')
            .ToArray()).Trim('-');
        while (safe.Contains("--", StringComparison.Ordinal))
        {
            safe = safe.Replace("--", "-", StringComparison.Ordinal);
        }

        return $"{(safe.Length == 0 ? "event" : safe)}.ics";
    }

    public static string BuildIcs(EventModel evt, string eventUrl, DateTimeOffset stamp)
    {
        var day = DateOnly.FromDateTime(evt.Date.DateTime);
        var description = string.IsNullOrWhiteSpace(evt.Description)
            ? eventUrl
            : $"{evt.Description.Trim()}\n\n{eventUrl}";

        var lines = new List<string>
        {
            "BEGIN:VCALENDAR",
            "VERSION:2.0",
            "PRODID:-//OpenWish//Events//EN",
            "CALSCALE:GREGORIAN",
            "METHOD:PUBLISH",
            "BEGIN:VEVENT",
            $"UID:{evt.PublicId}@openwish",
            $"DTSTAMP:{stamp.UtcDateTime:yyyyMMdd'T'HHmmss'Z'}",
            $"DTSTART;VALUE=DATE:{day:yyyyMMdd}",
            $"DTEND;VALUE=DATE:{day.AddDays(1):yyyyMMdd}",
            $"SUMMARY:{Escape(evt.Name)}",
            $"DESCRIPTION:{Escape(description)}",
            $"URL:{eventUrl}",
            "END:VEVENT",
            "END:VCALENDAR"
        };

        var builder = new StringBuilder();
        foreach (var line in lines)
        {
            builder.Append(Fold(line)).Append("\r\n");
        }

        return builder.ToString();
    }

    private static string Escape(string? value) =>
        (value ?? string.Empty)
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace(";", "\\;", StringComparison.Ordinal)
            .Replace(",", "\\,", StringComparison.Ordinal)
            .Replace("\r\n", "\\n", StringComparison.Ordinal)
            .Replace("\n", "\\n", StringComparison.Ordinal)
            .Replace("\r", "\\n", StringComparison.Ordinal);

    // Content lines are limited to 75 octets; continuation lines start with a space.
    private static string Fold(string line)
    {
        if (Encoding.UTF8.GetByteCount(line) <= 75)
        {
            return line;
        }

        var builder = new StringBuilder();
        var octets = 0;
        foreach (var rune in line.EnumerateRunes())
        {
            var size = rune.Utf8SequenceLength;
            if (octets + size > 75)
            {
                builder.Append("\r\n ");
                octets = 1;
            }

            builder.Append(rune.ToString());
            octets += size;
        }

        return builder.ToString();
    }
}