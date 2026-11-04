using OpenWish.Shared.Models;
using Xunit;

namespace OpenWish.Shared.Tests.Models;

public class GiftPlanTextTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 4, 12, 0, 0, TimeSpan.Zero);

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
                        new GiftPlanItemModel { ItemName = "Scarf", Price = 30m, Url = "https://shop.test/scarf", EventDate = Now.AddDays(3) },
                        new GiftPlanItemModel { ItemName = "Book", Price = 12.5m, PurchasedOn = Now },
                        new GiftPlanItemModel { ItemName = "Tea" }
                    ]
                },
                new GiftPlanRecipientModel
                {
                    RecipientName = "Casey",
                    Items = [new GiftPlanItemModel { ItemName = "Mug", Price = 9m, PurchasedOn = Now }]
                }
            ]
        };

        var text = GiftPlanText.Build(plan, Now);

        Assert.Equal(
            "Gift shopping list\n\nJordan (in 3 days)\n- Scarf ($30) - https://shop.test/scarf\n- Tea\n\nEstimated to buy: $30 (1 gift has no price)\n",
            text);
    }

    [Fact]
    public void Build_SaysWhenNothingIsLeft() =>
        Assert.Equal("Gift shopping list\nNothing left to buy.\n", GiftPlanText.Build(new GiftPlanModel(), Now));

    [Fact]
    public void RecipientsToBuy_DropsBoughtGiftsAndFinishedPeople()
    {
        var plan = new GiftPlanModel
        {
            Recipients =
            [
                new GiftPlanRecipientModel { RecipientName = "A", Items = [new GiftPlanItemModel { ItemName = "x", PurchasedOn = Now }] },
                new GiftPlanRecipientModel
                {
                    RecipientName = "B",
                    Items = [new GiftPlanItemModel { ItemName = "y", PurchasedOn = Now }, new GiftPlanItemModel { ItemName = "z" }]
                }
            ]
        };

        var recipient = Assert.Single(plan.RecipientsToBuy);
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
                new GiftPlanItemModel { EventDate = Now.AddDays(1), PurchasedOn = Now },
                new GiftPlanItemModel { EventDate = Now.AddDays(9) }
            ]
        };

        Assert.Equal(Now.AddDays(9), recipient.NextOccasion);
        Assert.Null(new GiftPlanRecipientModel { Items = [new GiftPlanItemModel()] }.NextOccasion);
    }
}