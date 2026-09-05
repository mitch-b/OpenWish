using OpenWish.Shared.Extensions;
using Xunit;

namespace OpenWish.Shared.Tests.Extensions;

public class DateTimeOffsetExtensionsTests
{
    [Fact]
    public void ToLocalTime_WithNullValue_ReturnsNull()
    {
        DateTimeOffset? value = null;

        var result = value.ToLocalTime();

        Assert.Null(result);
    }

    [Fact]
    public void ToLocalTime_PreservesTheInstant()
    {
        DateTimeOffset? value = new DateTimeOffset(2026, 9, 5, 12, 30, 0, TimeSpan.FromHours(5.5));

        var result = value.ToLocalTime();

        Assert.NotNull(result);
        Assert.Equal(value.Value.UtcDateTime, result.Value.UtcDateTime);
        Assert.Equal(TimeZoneInfo.Local.GetUtcOffset(value.Value), result.Value.Offset);
    }

    [Fact]
    public void ToLocalTimeString_WithNullValue_ReturnsNull()
    {
        DateTimeOffset? value = null;

        var result = value.ToLocalTimeString("O");

        Assert.Null(result);
    }

    [Fact]
    public void ToLocalHumanizedString_WithNullValue_ReturnsNull()
    {
        DateTimeOffset? value = null;

        var result = value.ToLocalHumanizedString();

        Assert.Null(result);
    }
}