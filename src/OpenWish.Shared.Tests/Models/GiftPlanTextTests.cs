using OpenWish.Shared.Models;
using Xunit;

namespace OpenWish.Shared.Tests.Models;

public class GiftPlanTextTests
{
    private static readonly DateTimeOffset _now = new(2026, 11, 4, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Build_ListsOnlyStillToBuyGiftsWithTotals()
    {
        var plan = new GiftPlanModel
        {
            Recipients =
            [
                new GiftPlanRecipientModel
                {
                    RecipientName = "Jordan",
                    Items =
                    [
                        new GiftPlanItemModel { ItemName = "Scarf", Price = 30m, Url = "https://shop.test/scarf", EventDate = _now.AddDays(3) },
                        new GiftPlanItemModel { ItemName = "Book", Price = 12.5m, PurchasedOn = _now },
                        new GiftPlanItemModel { ItemName = "Tea" }
                    ]
                },
                new GiftPlanRecipientModel
                {
                    RecipientName = "Casey",
                    Items = [new GiftPlanItemModel { ItemName = "Mug", Price = 9m, PurchasedOn = _now }]
                }
            ]
        };

        var text = WithCulture("en-US", () => GiftPlanText.Build(plan, _now));

        Assert.Equal(
            "Gift shopping list\n\nJordan (in 3 days)\n- Scarf ($30.00) - https://shop.test/scarf\n- Tea\n\nEstimated to buy: $30.00 (1 gift has no price)\n",
            text);
    }

    [Fact]
    public void Build_SaysWhenNothingIsLeft() =>
        Assert.Equal("Gift shopping list\nNothing left to buy.\n", GiftPlanText.Build(new GiftPlanModel(), _now));

    [Fact]
    public void RecipientsToBuy_DropsBoughtGiftsAndFinishedPeople()
    {
        var plan = new GiftPlanModel
        {
            Recipients =
            [
                new GiftPlanRecipientModel { RecipientName = "A", Items = [new GiftPlanItemModel { ItemName = "x", PurchasedOn = _now }] },
                new GiftPlanRecipientModel
                {
                    RecipientName = "B",
                    Items = [new GiftPlanItemModel { ItemName = "y", PurchasedOn = _now }, new GiftPlanItemModel { ItemName = "z" }]
                }
            ]
        };

        var recipient = Assert.Single(plan.GetRecipientsToBuy(_now));
        Assert.Equal("B", recipient.RecipientName);
        Assert.Equal(["z"], recipient.Items.Select(i => i.ItemName));
    }

    [Fact]
    public void Item_IsOverBudgetOnlyWhenPriceExceedsTheEventBudget()
    {
        Assert.True(new GiftPlanItemModel { Price = 31m, EventBudget = 30m }.IsOverBudget);
        Assert.False(new GiftPlanItemModel { Price = 30m, EventBudget = 30m }.IsOverBudget);
        Assert.False(new GiftPlanItemModel { Price = 99m }.IsOverBudget);
        Assert.False(new GiftPlanItemModel { EventBudget = 30m }.IsOverBudget);
    }

    [Fact]
    public void NextOccasion_PrefersGiftsStillToBuy()
    {
        var recipient = new GiftPlanRecipientModel
        {
            Items =
            [
                new GiftPlanItemModel { EventDate = _now.AddDays(1), PurchasedOn = _now },
                new GiftPlanItemModel { EventDate = _now.AddDays(9) }
            ]
        };

        Assert.Equal(_now.AddDays(9), recipient.GetNextOccasion(_now));
        Assert.Null(new GiftPlanRecipientModel { Items = [new GiftPlanItemModel()] }.GetNextOccasion(_now));
    }

    [Fact]
    public void NextOccasion_SkipsPastEventsWhileAnUpcomingOneExists()
    {
        var recipient = new GiftPlanRecipientModel
        {
            Items =
            [
                new GiftPlanItemModel { EventDate = _now.AddDays(-10) },
                new GiftPlanItemModel { EventDate = _now.AddDays(12) }
            ]
        };

        Assert.Equal(_now.AddDays(12), recipient.GetNextOccasion(_now));
        Assert.Equal(_now.AddDays(-10), new GiftPlanRecipientModel { Items = [recipient.Items[0]] }.GetNextOccasion(_now));
    }

    [Fact]
    public void GetRecipientsToBuy_ReordersByTheOccasionThatRemains()
    {
        var plan = new GiftPlanModel
        {
            Recipients =
            [
                new GiftPlanRecipientModel
                {
                    RecipientName = "Ana",
                    Items = [new GiftPlanItemModel { ItemName = "a", EventDate = _now.AddDays(1), PurchasedOn = _now }, new GiftPlanItemModel { ItemName = "b" }]
                },
                new GiftPlanRecipientModel
                {
                    RecipientName = "Ben",
                    Items = [new GiftPlanItemModel { ItemName = "c", EventDate = _now.AddDays(7) }]
                }
            ]
        };

        Assert.Equal(["Ben", "Ana"], plan.GetRecipientsToBuy(_now).Select(r => r.RecipientName));
    }

    [Fact]
    public void Build_FormatsPricesWithTheCurrentCulture()
    {
        var plan = new GiftPlanModel
        {
            Recipients = [new GiftPlanRecipientModel { RecipientName = "Jo", Items = [new GiftPlanItemModel { ItemName = "Tea", Price = 30m }] }]
        };

        var text = WithCulture("en-GB", () => GiftPlanText.Build(plan, _now));

        Assert.Contains("£30.00", text);
        Assert.DoesNotContain("$", text);
    }

    [Fact]
    public void BuildForRecipient_ListsOnlyThatPersonsGiftsStillToBuy()
    {
        var recipient = new GiftPlanRecipientModel
        {
            RecipientName = "Jordan",
            Items =
            [
                new GiftPlanItemModel { ItemName = "Scarf", Price = 30m, Url = "https://shop.test/scarf", EventDate = _now.AddDays(1) },
                new GiftPlanItemModel { ItemName = "Book", Price = 12.5m, PurchasedOn = _now },
                new GiftPlanItemModel { ItemName = "Tea" }
            ]
        };

        var text = WithCulture("en-US", () => GiftPlanText.BuildForRecipient(recipient, _now));

        Assert.Equal(
            "Gifts for Jordan (tomorrow)\n- Scarf ($30.00) - https://shop.test/scarf\n- Tea\n\nEstimated to buy: $30.00 (1 gift has no price)\n",
            text);
    }

    [Fact]
    public void BuildForRecipient_SaysWhenEverythingIsBought() =>
        Assert.Equal(
            "Gifts for Casey\nNothing left to buy.\n",
            GiftPlanText.BuildForRecipient(
                new GiftPlanRecipientModel { RecipientName = "Casey", Items = [new GiftPlanItemModel { ItemName = "Mug", PurchasedOn = _now }] },
                _now));

    private static T WithCulture<T>(string name, Func<T> action)
    {
        var previous = System.Globalization.CultureInfo.CurrentCulture;
        System.Globalization.CultureInfo.CurrentCulture = new System.Globalization.CultureInfo(name);
        try
        {
            return action();
        }
        finally
        {
            System.Globalization.CultureInfo.CurrentCulture = previous;
        }
    }
}