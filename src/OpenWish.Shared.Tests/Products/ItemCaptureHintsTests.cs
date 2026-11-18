using OpenWish.Shared.Models;
using OpenWish.Shared.Products;
using Xunit;

namespace OpenWish.Shared.Tests.Products;

public class ItemCaptureHintsTests
{
    [Fact]
    public void StoreForLink_NamesTheStore()
    {
        Assert.Equal("Target", ItemCaptureHints.StoreForLink("https://www.target.com/p/item/-/A-1"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("a cozy blanket")]
    public void StoreForLink_IgnoresTextThatIsNotALink(string? text)
    {
        Assert.Null(ItemCaptureHints.StoreForLink(text));
    }

    [Fact]
    public void SuggestStore_FillsOnlyAnEmptyWhereToBuy()
    {
        var item = new WishlistItemModel { Name = "Mug", Url = "https://www.target.com/p/mug/-/A-1" };

        Assert.True(ItemCaptureHints.SuggestStore(item));
        Assert.Equal("Target", item.WhereToBuy);

        item.WhereToBuy = "Local shop";
        Assert.False(ItemCaptureHints.SuggestStore(item));
        Assert.Equal("Local shop", item.WhereToBuy);

        var noLink = new WishlistItemModel { Name = "Mug", Url = "not a link" };
        Assert.False(ItemCaptureHints.SuggestStore(noLink));
        Assert.Null(noLink.WhereToBuy);
    }

    [Fact]
    public void FindDuplicateName_MatchesNameIgnoringCaseAndSpacing()
    {
        var existing = new[] { new WishlistItemModel { Name = "Travel Mug", PublicId = "a" } };

        Assert.Equal("Travel Mug", ItemCaptureHints.FindDuplicateName(existing, "  travel mug ", null));
    }

    [Fact]
    public void FindDuplicateName_MatchesTheSameLinkWithoutTrackingParameters()
    {
        var existing = new[]
        {
            new WishlistItemModel { Name = "Kettle", PublicId = "a", Url = "https://shop.example/p/kettle-1" }
        };

        Assert.Equal("Kettle", ItemCaptureHints.FindDuplicateName(existing, "Electric kettle", "https://shop.example/p/kettle-1?utm_source=news"));
    }

    [Fact]
    public void FindDuplicateName_IgnoresDeletedAndSelfAndDifferentIdeas()
    {
        var existing = new[]
        {
            new WishlistItemModel { Name = "Scarf", PublicId = "a", Deleted = true },
            new WishlistItemModel { Name = "Hat", PublicId = "b" },
            new WishlistItemModel { Name = "Gloves", PublicId = "c" }
        };

        Assert.Null(ItemCaptureHints.FindDuplicateName(existing, "Scarf", null));
        Assert.Null(ItemCaptureHints.FindDuplicateName(existing, "Hat", null, "b"));
        Assert.Null(ItemCaptureHints.FindDuplicateName(existing, "Boots", null));
        Assert.Null(ItemCaptureHints.FindDuplicateName(existing, "", null));
    }
}