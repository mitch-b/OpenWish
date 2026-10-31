using OpenWish.Shared.Models;
using Xunit;

namespace OpenWish.Shared.Tests.Models;

public class EventCountdownTests
{
    private static readonly TimeSpan _offset = TimeSpan.FromHours(-5);

    [Theory]
    [InlineData("2026-12-25T18:00:00-05:00", "2026-12-25T08:00:00-05:00", "today")]
    [InlineData("2026-12-25T08:00:00-05:00", "2026-12-24T23:30:00-05:00", "tomorrow")]
    [InlineData("2026-12-25T18:00:00-05:00", "2026-12-15T23:59:00-05:00", "in 10 days")]
    public void Describe_CountsCalendarDays(string occasion, string now, string expected)
    {
        Assert.Equal(expected, EventCountdown.Describe(DateTimeOffset.Parse(occasion), DateTimeOffset.Parse(now)));
    }

    [Fact]
    public void Describe_ReturnsNullAfterTheOccasion()
    {
        Assert.Null(EventCountdown.Describe(
            new DateTimeOffset(2026, 12, 25, 18, 0, 0, _offset),
            new DateTimeOffset(2026, 12, 26, 0, 30, 0, _offset)));
    }

    [Fact]
    public void Describe_ComparesInTheOccasionsOwnOffset()
    {
        var occasion = new DateTimeOffset(2026, 12, 25, 1, 0, 0, _offset);
        var now = new DateTimeOffset(2026, 12, 25, 3, 0, 0, TimeSpan.Zero); // still Dec 24 at -05:00

        Assert.Equal("tomorrow", EventCountdown.Describe(occasion, now));
    }
}

public class ReservedSpendTests
{
    [Fact]
    public void From_TotalsPricesAndCountsUnpricedItems()
    {
        var spend = ReservedSpend.From([25m, null, 14.5m], 50m);

        Assert.Equal(39.5m, spend.Total);
        Assert.Equal(1, spend.UnpricedCount);
        Assert.Equal(10.5m, spend.Remaining);
        Assert.True(spend.HasBudget);
        Assert.False(spend.IsOverBudget);
    }

    [Fact]
    public void From_FlagsSpendingOverTheBudget()
    {
        var spend = ReservedSpend.From([40m, 20m], 50m);

        Assert.True(spend.IsOverBudget);
        Assert.Equal(-10m, spend.Remaining);
    }

    [Fact]
    public void From_WithoutABudgetIsNeverOver()
    {
        var spend = ReservedSpend.From([999m], null);

        Assert.False(spend.HasBudget);
        Assert.False(spend.IsOverBudget);
    }
}