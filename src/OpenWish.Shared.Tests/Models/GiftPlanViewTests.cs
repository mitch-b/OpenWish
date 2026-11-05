using OpenWish.Shared.Models;
using Xunit;

namespace OpenWish.Shared.Tests.Models;

public class GiftPlanViewTests
{
    private static readonly DateTimeOffset _now = new(2026, 11, 4, 12, 0, 0, TimeSpan.Zero);

    private static GiftPlanModel Plan() => new()
    {
        Recipients =
        [
            new GiftPlanRecipientModel
            {
                RecipientName = "Jordan",
                Items =
                [
                    new GiftPlanItemModel { ItemName = "Headphones", Price = 250m, EventBudget = 75m, EventName = "Holiday Exchange", WishlistName = "Jordan's list", EventDate = _now.AddDays(20) },
                    new GiftPlanItemModel { ItemName = "Book", Price = 12m, PurchasedOn = _now, WishlistName = "Jordan's list" },
                    new GiftPlanItemModel { ItemName = "Tea", WishlistName = "Jordan's list" }
                ]
            },
            new GiftPlanRecipientModel
            {
                RecipientName = "Casey",
                Items = [new GiftPlanItemModel { ItemName = "Mug", Price = 9m, WishlistName = "Casey ideas", EventDate = _now.AddDays(2) }]
            }
        ]
    };

    [Fact]
    public void All_KeepsEveryoneInPlanOrder()
    {
        var result = GiftPlanView.Apply(Plan(), GiftPlanFilter.All, null, _now);

        Assert.Equal(["Jordan", "Casey"], result.Select(r => r.RecipientName));
        Assert.Equal(3, result[0].Items.Count);
    }

    [Fact]
    public void ToBuy_DropsBoughtGiftsKeepsOverallProgressAndOrdersByOccasion()
    {
        var result = GiftPlanView.Apply(Plan(), GiftPlanFilter.ToBuy, null, _now);

        Assert.Equal(["Casey", "Jordan"], result.Select(r => r.RecipientName));
        var jordan = result[1];
        Assert.Equal(["Headphones", "Tea"], jordan.Items.Select(i => i.ItemName));
        Assert.Equal(3, jordan.GiftCount);
        Assert.Equal(1, jordan.BoughtCount);
    }

    [Fact]
    public void OverBudget_ShowsOnlyGiftsAboveTheirBudget()
    {
        var result = GiftPlanView.Apply(Plan(), GiftPlanFilter.OverBudget, null, _now);

        var jordan = Assert.Single(result);
        Assert.Equal(["Headphones"], jordan.Items.Select(i => i.ItemName));
    }

    [Theory]
    [InlineData("  mug ", "Casey", 1)]
    [InlineData("holiday", "Jordan", 1)]
    [InlineData("CASEY IDEAS", "Casey", 1)]
    public void Search_MatchesGiftListOrOccasionIgnoringCaseAndSpaces(string search, string person, int count)
    {
        var result = Assert.Single(GiftPlanView.Apply(Plan(), GiftPlanFilter.All, search, _now));

        Assert.Equal(person, result.RecipientName);
        Assert.Equal(count, result.Items.Count);
    }

    [Fact]
    public void Search_ByPersonKeepsTheirGiftsThatPassTheFilter()
    {
        var result = Assert.Single(GiftPlanView.Apply(Plan(), GiftPlanFilter.ToBuy, "jordan", _now));

        Assert.Equal(2, result.Items.Count);
    }

    [Fact]
    public void Search_WithNoMatchReturnsNobody() =>
        Assert.Empty(GiftPlanView.Apply(Plan(), GiftPlanFilter.All, "zzz", _now));
}