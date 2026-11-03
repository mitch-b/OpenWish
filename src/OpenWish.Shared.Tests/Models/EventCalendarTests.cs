using OpenWish.Shared.Models;
using Xunit;

namespace OpenWish.Shared.Tests.Models;

public class EventCalendarTests
{
    private static EventModel Sample(string name = "Cookout; Swap, 2026", string? description = "Bring a dish.\nSee you there") => new()
    {
        PublicId = "abc123",
        Name = name,
        Description = description,
        Date = new DateTimeOffset(2026, 12, 24, 0, 0, 0, TimeSpan.Zero),
        EventUsers = []
    };

    [Fact]
    public void BuildIcs_WritesAnAllDayEventWithEscapedText()
    {
        var ics = EventCalendar.BuildIcs(Sample(), "https://example.test/events/abc123", new DateTimeOffset(2026, 11, 3, 1, 0, 0, TimeSpan.Zero));

        Assert.StartsWith("BEGIN:VCALENDAR\r\n", ics);
        Assert.EndsWith("END:VCALENDAR\r\n", ics);
        Assert.Contains("UID:abc123@openwish\r\n", ics);
        Assert.Contains("DTSTAMP:20261103T010000Z\r\n", ics);
        Assert.Contains("DTSTART;VALUE=DATE:20261224\r\n", ics);
        Assert.Contains("DTEND;VALUE=DATE:20261225\r\n", ics);
        Assert.Contains("SUMMARY:Cookout\\; Swap\\, 2026\r\n", ics);
        Assert.Contains("Bring a dish.\\nSee you there\\n\\nhttps://example.test/events/abc123", ics.Replace("\r\n ", string.Empty));
    }

    [Fact]
    public void BuildIcs_FoldsLongLinesAtSeventyFiveOctets()
    {
        var ics = EventCalendar.BuildIcs(Sample(description: new string('é', 120)), "https://example.test/events/abc123", DateTimeOffset.UtcNow);

        foreach (var line in ics.Split("\r\n"))
        {
            Assert.True(System.Text.Encoding.UTF8.GetByteCount(line) <= 75, line);
        }
    }

    [Fact]
    public void BuildIcs_NeverIncludesParticipantsOrAssignments()
    {
        var evt = Sample();
        evt.GiftExchanges = [new GiftExchangeModel { Receiver = new ApplicationUserModel { Id = "r", UserName = "SecretMatch", Email = "r@example.test" } }];

        var ics = EventCalendar.BuildIcs(evt, "https://example.test/events/abc123", DateTimeOffset.UtcNow);

        Assert.DoesNotContain("SecretMatch", ics);
    }

    [Theory]
    [InlineData("Cookout; Swap, 2026", "cookout-swap-2026.ics")]
    [InlineData("!!!", "event.ics")]
    public void FileName_IsSafe(string name, string expected) => Assert.Equal(expected, EventCalendar.FileName(Sample(name)));
}