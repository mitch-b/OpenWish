using OpenWish.Shared.Models;
using Xunit;

namespace OpenWish.Shared.Tests.Models;

public class WishlistTextTests
{
    [Fact]
    public void Build_ListsVisibleIdeasWithPriceAndLinkOnly()
    {
        var items = new[]
        {
            new WishlistItemModel { Name = "Skillet", Price = 38m, Url = "https://example.com/skillet", Reservations = [new ItemReservationModel()] },
            new WishlistItemModel { Name = "Apron" },
            new WishlistItemModel { Name = "Surprise", IsHiddenFromOwner = true },
            new WishlistItemModel { Name = "Already got it", ReceivedOn = DateTimeOffset.UtcNow }
        };

        var text = WishlistText.Build("Kitchen", items, "https://example.test/wishlists/k");

        Assert.Equal("Kitchen\n- Skillet ($38) - https://example.com/skillet\n- Apron\n\nhttps://example.test/wishlists/k\n", text);
    }

    [Fact]
    public void Build_KeepsCents() =>
        Assert.Contains("- Candle ($38.99)", WishlistText.Build("L", [new WishlistItemModel { Name = "Candle", Price = 38.99m }]));

    [Fact]
    public void Build_SaysWhenThereAreNoIdeas() =>
        Assert.Equal("Empty\nNo ideas yet.\n", WishlistText.Build("Empty", []));
}