using OpenWish.Shared.Models;
using Xunit;

namespace OpenWish.Shared.Tests;

public class EventGiftIdeasTests
{
    [Fact]
    public void Count_OnlyIncludesActiveIdeasVisibleToGiftGivers()
    {
        var wishlist = new WishlistModel
        {
            Name = "Family ideas",
            ItemCount = 99,
            Items =
            [
                new() { Name = "Book" },
                new() { Name = "Surprise", IsHiddenFromOwner = true },
                new() { Name = "Private", IsPrivate = true },
                new() { Name = "Deleted", Deleted = true },
                new() { Name = "Received", ReceivedOn = DateTimeOffset.UtcNow }
            ]
        };

        Assert.Equal(2, EventGiftIdeas.Count(wishlist));
    }

    [Fact]
    public void Count_DoesNotTreatAnEmptyWishlistOrMetadataAsGiftIdeas()
    {
        Assert.Equal(0, EventGiftIdeas.Count(new() { Name = "Empty", ItemCount = 4 }));
    }
}