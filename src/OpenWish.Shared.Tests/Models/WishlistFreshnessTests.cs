using OpenWish.Shared.Models;
using Xunit;

namespace OpenWish.Shared.Tests.Models;

public class WishlistFreshnessTests
{
    private static readonly DateTimeOffset _now = new(2026, 10, 19, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void GetStaleItems_ReturnsOnlyIdeasUntouchedForSixMonthsOldestFirst()
    {
        var items = new[]
        {
            new WishlistItemModel { Name = "Fresh", UpdatedOn = _now.AddDays(-10) },
            new WishlistItemModel { Name = "Edge", UpdatedOn = _now.AddDays(-WishlistFreshness.StaleAfterDays) },
            new WishlistItemModel { Name = "Ancient", UpdatedOn = _now.AddDays(-900) },
            new WishlistItemModel { Name = "Almost", UpdatedOn = _now.AddDays(-WishlistFreshness.StaleAfterDays + 1) }
        };

        var stale = WishlistFreshness.GetStaleItems(items, _now);

        Assert.Equal(["Ancient", "Edge"], stale.Select(item => item.Name).ToArray());
    }

    [Fact]
    public void GetStaleItems_SkipsSurprisesHiddenFromTheOwner()
    {
        var hidden = new WishlistItemModel { Name = "Surprise", UpdatedOn = _now.AddDays(-500), IsHiddenFromOwner = true };

        Assert.Empty(WishlistFreshness.GetStaleItems([hidden], _now));
    }
}