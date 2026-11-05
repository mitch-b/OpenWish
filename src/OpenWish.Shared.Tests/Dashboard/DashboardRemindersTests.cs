using OpenWish.Shared.Dashboard;
using OpenWish.Shared.Models;
using Xunit;

namespace OpenWish.Shared.Tests.Dashboard;

public class DashboardRemindersTests
{
    private static readonly DateTimeOffset _now = new(2026, 11, 3, 12, 0, 0, TimeSpan.Zero);

    private static EventModel Event(string id, int daysAway, string organizerId = "me", bool exchange = false, bool drawn = false) => new()
    {
        PublicId = id,
        Name = $"Event {id}",
        Date = _now.Date.AddDays(daysAway),
        CreatedBy = new ApplicationUserModel { Id = organizerId, UserName = organizerId, Email = $"{organizerId}@example.test" },
        IsGiftExchange = exchange,
        NamesDrawnOn = drawn ? _now : null,
        EventUsers = []
    };

    [Fact]
    public void OrganizerIsToldToDrawNamesBeforeAnUndrawnExchange()
    {
        var reminder = Assert.Single(DashboardReminders.For([Event("a", 10, exchange: true)], "me", _now));

        Assert.Equal("Draw names for Event a", reminder.Title);
        Assert.Equal("/events/a/manage", reminder.Href);
    }

    [Fact]
    public void ParticipantsAreNotAskedToDrawNames()
    {
        var reminders = DashboardReminders.For([Event("a", 10, organizerId: "other", exchange: true)], "me", _now);

        Assert.Empty(reminders);
    }

    [Fact]
    public void NearOccasionsRemindEveryoneAndFarOrPastOnesDoNot()
    {
        var reminders = DashboardReminders.For(
            [Event("soon", 1, "other"), Event("far", 30, "other"), Event("past", -2, "other")], "me", _now);

        var reminder = Assert.Single(reminders);
        Assert.Equal("Event soon is tomorrow", reminder.Title);
    }

    [Fact]
    public void DrawnExchangePointsToTheMatchAndIsCappedSoonestFirst()
    {
        var events = new[]
        {
            Event("d", 6, "other"), Event("c", 4, "other"), Event("b", 2, "other"), Event("a", 0, "other", exchange: true, drawn: true)
        };

        var reminders = DashboardReminders.For(events, "me", _now);

        Assert.Equal(["a", "b", "c"], reminders.Select(r => r.Href.Split('/').Last()));
        Assert.Equal("Check your match and finish your gift.", reminders[0].Detail);
    }
}