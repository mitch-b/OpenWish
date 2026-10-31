using OpenWish.Shared.Models;
using Xunit;

namespace OpenWish.Shared.Tests.Models;

public class EventCountdownTests
{
    private static readonly TimeZoneInfo _shopperZone = TimeZoneInfo.CreateCustomTimeZone("test", TimeSpan.FromHours(-5), "test", "test");

    [Theory]
    [InlineData("2026-12-25T18:00:00-05:00", "2026-12-25T08:00:00-05:00", "today")]
    [InlineData("2026-12-25T08:00:00-05:00", "2026-12-24T23:30:00-05:00", "tomorrow")]
    [InlineData("2026-12-25T18:00:00-05:00", "2026-12-15T23:59:00-05:00", "in 10 days")]
    public void Describe_CountsCalendarDays(string occasion, string now, string expected)
    {
        Assert.Equal(expected, EventCountdown.Describe(DateTimeOffset.Parse(occasion), DateTimeOffset.Parse(now), _shopperZone));
    }

    [Fact]
    public void Describe_ReturnsNullAfterTheOccasion()
    {
        Assert.Null(EventCountdown.Describe(
            new DateTimeOffset(2026, 12, 25, 18, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 12, 26, 12, 0, 0, TimeSpan.Zero),
            _shopperZone));
    }

    [Fact]
    public void Describe_UsesTheShoppersDayForAUtcStoredOccasion()
    {
        // Dec 25 18:00Z is Dec 25 13:00 for the shopper; at Dec 24 20:00 -05:00 it is still tomorrow.
        var occasion = new DateTimeOffset(2026, 12, 25, 18, 0, 0, TimeSpan.Zero);
        var now = new DateTimeOffset(2026, 12, 24, 20, 0, 0, TimeSpan.FromHours(-5));

        Assert.Equal("tomorrow", EventCountdown.Describe(occasion, now, _shopperZone));
    }

    [Fact]
    public void Describe_RollsOverAtTheShoppersMidnightNotUtc()
    {
        var occasion = new DateTimeOffset(2026, 12, 25, 3, 0, 0, TimeSpan.Zero); // Dec 24 22:00 for the shopper
        var now = new DateTimeOffset(2026, 12, 24, 23, 0, 0, TimeSpan.Zero); // Dec 24 18:00 for the shopper

        Assert.Equal("today", EventCountdown.Describe(occasion, now, _shopperZone));
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