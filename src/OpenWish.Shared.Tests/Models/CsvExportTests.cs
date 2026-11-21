using OpenWish.Shared.Models;
using Xunit;

namespace OpenWish.Shared.Tests.Models;

public class CsvExportTests
{
    private static readonly DateTimeOffset _now = new(2026, 11, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Build_QuotesCommasQuotesAndLineBreaks() =>
        Assert.Equal("A,B\r\n\"x, y\",\"say \"\"hi\"\"\"\r\n\"line\nbreak\",plain\r\n",
            CsvFile.Build(["A", "B"], [["x, y", "say \"hi\""], ["line\nbreak", "plain"]]));

    [Theory]
    [InlineData("=SUM(A1:A9)", "'=SUM(A1:A9)")]
    [InlineData("+1", "'+1")]
    [InlineData("-2", "'-2")]
    [InlineData("@cmd", "'@cmd")]
    [InlineData("\tx", "'\tx")]
    public void Build_NeutralizesCellsThatWouldRunAsFormulas(string cell, string expected) =>
        Assert.Equal($"H\r\n{expected}\r\n", CsvFile.Build(["H"], [[cell]]));

    [Fact]
    public void FileName_KeepsSafeCharactersOnly()
    {
        Assert.Equal("Moms-list", CsvFile.FileName("Mom's list", "x"));
        Assert.Equal("fallback", CsvFile.FileName("///", "fallback"));
    }

    [Fact]
    public void WishlistCsv_ListsVisibleIdeasAndNeverReservations()
    {
        var items = new[]
        {
            new WishlistItemModel
            {
                Name = "Skillet, cast iron", Price = 38m, Url = " https://example.com/s ", WhereToBuy = "Shop", Priority = 1,
                IsPinned = true, Description = "12 inch", Reservations = [new ItemReservationModel()]
            },
            new WishlistItemModel { Name = "Apron" },
            new WishlistItemModel { Name = "Surprise", IsHiddenFromOwner = true },
            new WishlistItemModel { Name = "Got it", ReceivedOn = _now },
            new WishlistItemModel { Name = "Put away", ArchivedOn = _now }
        };

        Assert.Equal(
            "Idea,Price,Where to buy,Link,Priority,Most wanted,Notes\r\n" +
            "\"Skillet, cast iron\",38.00,Shop,https://example.com/s,High,Yes,12 inch\r\n" +
            "Apron,,,,,,\r\n",
            WishlistCsv.Build(items));
    }

    [Fact]
    public void GiftPlanCsv_ListsEachGiftWithItsStatusSoonestOccasionFirst()
    {
        var plan = new GiftPlanModel
        {
            Recipients =
            [
                new GiftPlanRecipientModel
                {
                    RecipientName = "Later",
                    Items = [new GiftPlanItemModel { ItemName = "Scarf", Price = 20m, WishlistName = "Winter", EventName = "Party", EventDate = _now.AddDays(30), EventBudget = 25m }]
                },
                new GiftPlanRecipientModel
                {
                    RecipientName = "Sooner",
                    Items =
                    [
                        new GiftPlanItemModel { ItemName = "Mug", WishlistName = "Kitchen", EventDate = _now.AddDays(3), PurchasedOn = _now },
                        new GiftPlanItemModel { ItemName = "Pan", WishlistName = "Kitchen", EventDate = _now.AddDays(3), PurchasedOn = _now, WrappedOn = _now, Url = "https://example.com/p" }
                    ]
                }
            ]
        };

        Assert.Equal(
            "Recipient,Gift,Price,Budget,Status,Occasion,Occasion date,Wishlist,Link\r\n" +
            "Sooner,Mug,,,Bought,,2026-11-04,Kitchen,\r\n" +
            "Sooner,Pan,,,Wrapped,,2026-11-04,Kitchen,https://example.com/p\r\n" +
            "Later,Scarf,20.00,25.00,To buy,Party,2026-12-01,Winter,\r\n",
            GiftPlanCsv.Build(plan, _now));
    }

    [Fact]
    public void EventsCsv_ShowsEventDetailsWithoutEmailsOrPairings()
    {
        var events = new[]
        {
            new EventModel
            {
                Name = "Family exchange", Date = _now.AddDays(20), Budget = 40m, IsGiftExchange = true, NamesDrawnOn = _now,
                CreatedBy = new ApplicationUserModel { UserName = "host@example.com", Email = "host@example.com" }, EventUsers = [],
                GiftExchanges = [new GiftExchangeModel()]
            },
            new EventModel
            {
                Name = "Birthday", Date = _now.AddDays(5), Description = "Cake",
                CreatedBy = new ApplicationUserModel { UserName = "AlexDemo", Email = "a@example.com" }, EventUsers = []
            }
        };

        var csv = EventsCsv.Build(events);

        Assert.Equal(
            "Event,Date,Budget,Gift exchange,Names drawn,Organizer,Description\r\n" +
            "Birthday,2026-11-06,,No,No,AlexDemo,Cake\r\n" +
            "Family exchange,2026-11-21,40.00,Yes,Yes,,\r\n",
            csv);
        Assert.DoesNotContain("@", csv);
    }
}