using OpenWish.Shared.Models;
using Xunit;

namespace OpenWish.Shared.Tests.Models;

public class BirthdayCalendarTests
{
    [Theory]
    [InlineData(2, 29, true)]
    [InlineData(2, 30, false)]
    [InlineData(4, 31, false)]
    [InlineData(12, 31, true)]
    [InlineData(13, 1, false)]
    [InlineData(0, 1, false)]
    public void IsValid_ChecksRealCalendarDates(int month, int day, bool expected) =>
        Assert.Equal(expected, BirthdayCalendar.IsValid(month, day));

    [Fact]
    public void NextOccurrence_RollsIntoNextYearAfterTheDate()
    {
        var today = new DateOnly(2026, 10, 28);
        Assert.Equal(new DateOnly(2026, 10, 28), BirthdayCalendar.NextOccurrence(10, 28, today));
        Assert.Equal(new DateOnly(2027, 1, 5), BirthdayCalendar.NextOccurrence(1, 5, today));
        Assert.Equal(0, BirthdayCalendar.DaysUntil(10, 28, today));
        Assert.Equal(69, BirthdayCalendar.DaysUntil(1, 5, today));
    }

    [Fact]
    public void NextOccurrence_ObservesLeapDayOnFebruary28InCommonYears()
    {
        Assert.Equal(new DateOnly(2027, 2, 28), BirthdayCalendar.NextOccurrence(2, 29, new DateOnly(2026, 10, 28)));
        Assert.Equal(new DateOnly(2028, 2, 29), BirthdayCalendar.NextOccurrence(2, 29, new DateOnly(2028, 1, 1)));
    }
}