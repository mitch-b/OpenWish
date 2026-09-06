using OpenWish.Application.Models;
using OpenWish.Shared.Models;
using Xunit;

namespace OpenWish.Application.Tests.Models;

public class NotificationActionMapperTests
{
    [Fact]
    public void Serialize_WithRelativeApplicationPath_RoundTrips()
    {
        var action = new NotificationActionModel { NavigateTo = "/wishlists/example" };

        var serialized = NotificationActionMapper.Serialize(action);
        var deserialized = NotificationActionMapper.Deserialize(serialized);

        Assert.Equal(action.NavigateTo, deserialized?.NavigateTo);
    }

    [Theory]
    [InlineData("https://example.com")]
    [InlineData("//example.com")]
    [InlineData("javascript:alert(1)")]
    [InlineData("/safe:unsafe")]
    public void Serialize_WithUnsafeNavigationTarget_Throws(string target)
    {
        var action = new NotificationActionModel { NavigateTo = target };

        Assert.Throws<ArgumentException>(() => NotificationActionMapper.Serialize(action));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-json")]
    public void Deserialize_WithMissingOrInvalidData_ReturnsNull(string? value)
    {
        var result = NotificationActionMapper.Deserialize(value);

        Assert.Null(result);
    }
}