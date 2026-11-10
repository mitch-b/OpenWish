using OpenWish.Shared.Dashboard;
using Xunit;

namespace OpenWish.Shared.Tests.Dashboard;

public class NotificationTimeTests
{
    private static readonly DateTimeOffset _now = new(2026, 11, 10, 12, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(-30, "Just now")]
    [InlineData(0, "Just now")]
    [InlineData(5 * 60, "5 min ago")]
    [InlineData(60 * 60, "1 hour ago")]
    [InlineData(3 * 60 * 60, "3 hours ago")]
    [InlineData(24 * 60 * 60, "Yesterday")]
    [InlineData(3 * 24 * 60 * 60, "3 days ago")]
    public void RecentNotificationsUseRelativeWords(int secondsAgo, string expected)
    {
        Assert.Equal(expected, NotificationTime.Describe(_now.AddSeconds(-secondsAgo), _now));
    }

    [Fact]
    public void OlderNotificationsUseADateWithoutAYearInTheCurrentYear()
    {
        Assert.Equal("Oct 1", NotificationTime.Describe(new DateTimeOffset(2026, 10, 1, 8, 0, 0, TimeSpan.Zero), _now));
    }

    [Fact]
    public void NotificationsFromEarlierYearsIncludeTheYear()
    {
        Assert.Equal("Dec 24, 2025", NotificationTime.Describe(new DateTimeOffset(2025, 12, 24, 8, 0, 0, TimeSpan.Zero), _now));
    }
}