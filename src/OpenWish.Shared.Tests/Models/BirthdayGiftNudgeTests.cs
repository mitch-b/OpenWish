using OpenWish.Shared.Models;
using Xunit;

namespace OpenWish.Shared.Tests.Models;

public class BirthdayGiftNudgeTests
{
    private static GiftPlanItemModel Item(string name, bool bought) =>
        new() { ItemName = name, PurchasedOn = bought ? DateTimeOffset.UtcNow : null };

    [Fact]
    public void CountToBuy_CountsOnlyUnboughtGiftsForTheBirthdayPerson()
    {
        var plan = new GiftPlanModel
        {
            Recipients =
            [
                new GiftPlanRecipientModel { RecipientName = "Jordan", Items = [Item("Game", false), Item("Mug", true)] },
                new GiftPlanRecipientModel { RecipientName = "Casey", Items = [Item("Book", false)] }
            ]
        };

        var counts = BirthdayGiftNudge.CountToBuy(plan, [new UpcomingBirthdayModel { UserName = "jordan" }]);

        Assert.Equal(1, Assert.Single(counts).Value);
    }

    [Fact]
    public void CountToBuy_OmitsPeopleWithNothingLeftToBuy()
    {
        var plan = new GiftPlanModel
        {
            Recipients = [new GiftPlanRecipientModel { RecipientName = "Jordan", Items = [Item("Game", true)] }]
        };

        Assert.Empty(BirthdayGiftNudge.CountToBuy(plan, [new UpcomingBirthdayModel { UserName = "Jordan" }]));
        Assert.Empty(BirthdayGiftNudge.CountToBuy(new GiftPlanModel(), [new UpcomingBirthdayModel { UserName = "Jordan" }]));
    }

    [Theory]
    [InlineData(1, "1 gift still to buy")]
    [InlineData(3, "3 gifts still to buy")]
    public void Describe_UsesSingularAndPlural(int count, string expected) =>
        Assert.Equal(expected, BirthdayGiftNudge.Describe(count));
}
