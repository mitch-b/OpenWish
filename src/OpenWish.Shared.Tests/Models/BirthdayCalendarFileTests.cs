using OpenWish.Shared.Models;
using Xunit;

namespace OpenWish.Shared.Tests.Models;

public class BirthdayCalendarFileTests
{
    private static readonly DateTimeOffset _stamp = new(2026, 11, 15, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public void BuildIcs_CreatesYearlyAllDayEventsWithoutABirthYear()
    {
        var ics = BirthdayCalendarFile.BuildIcs(
            [new UpcomingBirthdayModel { UserName = "Sam", Month = 3, Day = 9, WishlistPublicId = "abc" }],
            "https://wish.example", new DateOnly(2026, 11, 15), _stamp);

        Assert.Contains("DTSTART;VALUE=DATE:20270309\r\n", ics, StringComparison.Ordinal);
        Assert.Contains("DTEND;VALUE=DATE:20270310\r\n", ics, StringComparison.Ordinal);
        Assert.Contains("RRULE:FREQ=YEARLY\r\n", ics, StringComparison.Ordinal);
        Assert.Contains("SUMMARY:Sam's birthday\r\n", ics, StringComparison.Ordinal);
        Assert.Contains("URL:https://wish.example/wishlists/abc\r\n", ics, StringComparison.Ordinal);
        Assert.StartsWith("BEGIN:VCALENDAR\r\n", ics, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildIcs_ObservesLeapDayOnTheLastDayOfFebruary()
    {
        var ics = BirthdayCalendarFile.BuildIcs(
            [new UpcomingBirthdayModel { UserName = "Leap", Month = 2, Day = 29 }],
            "https://wish.example/", new DateOnly(2026, 11, 15), _stamp);

        Assert.Contains("DTSTART;VALUE=DATE:20270228\r\n", ics, StringComparison.Ordinal);
        Assert.Contains("RRULE:FREQ=YEARLY;BYMONTH=2;BYMONTHDAY=-1\r\n", ics, StringComparison.Ordinal);
        Assert.Contains("URL:https://wish.example/\r\n", ics, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildIcs_SkipsInvalidDatesAndEscapesNames()
    {
        var ics = BirthdayCalendarFile.BuildIcs(
            [
                new UpcomingBirthdayModel { UserName = "Bad", Month = 2, Day = 31 },
                new UpcomingBirthdayModel { UserName = "A;B,C", Month = 1, Day = 2 }
            ],
            "https://wish.example", new DateOnly(2026, 11, 15), _stamp);

        Assert.DoesNotContain("Bad", ics, StringComparison.Ordinal);
        Assert.Contains("SUMMARY:A\\;B\\,C's birthday", ics, StringComparison.Ordinal);
        Assert.Single(ics.Split("BEGIN:VEVENT", StringSplitOptions.None).Skip(1));
    }

    [Fact]
    public void BuildIcs_WithNoFriends_IsAnEmptyValidCalendar()
    {
        var ics = BirthdayCalendarFile.BuildIcs([], "https://wish.example", new DateOnly(2026, 1, 1), _stamp);

        Assert.DoesNotContain("BEGIN:VEVENT", ics, StringComparison.Ordinal);
        Assert.EndsWith("END:VCALENDAR\r\n", ics, StringComparison.Ordinal);
    }
}