using OpenWish.Shared.Models;
using OpenWish.Shared.Products;
using Xunit;

namespace OpenWish.Shared.Tests.Products;

public class ProductCaptureTests
{
    [Fact]
    public void FromQuery_ReadsBookmarkletDetails()
    {
        var capture = ProductCapture.FromQuery(
            url: "https://www.walmart.com/ip/LEGO-Millennium-Falcon/123456789?athbdg=L1600&from=/search",
            title: "LEGO Star Wars Millennium Falcon - Walmart.com",
            description: "  Build the  fastest ship  ",
            price: "$169.99",
            image: "https://i5.walmartimages.com/falcon.jpg");

        Assert.Equal("https://www.walmart.com/ip/LEGO-Millennium-Falcon/123456789", capture.Link?.AbsoluteUri);
        Assert.Equal("Walmart", capture.StoreName);
        Assert.Equal("LEGO Star Wars Millennium Falcon", capture.Name);
        Assert.Equal("Build the fastest ship", capture.Description);
        Assert.Equal(169.99m, capture.Price);
        Assert.Equal("https://i5.walmartimages.com/falcon.jpg", capture.Image);
        Assert.False(capture.IsEmpty);
    }

    [Fact]
    public void FromQuery_FindsALinkInSharedText()
    {
        var capture = ProductCapture.FromQuery(
            url: null,
            text: "Check out this Handmade ceramic tea mug https://gifts.example/products/handmade-ceramic-tea-mug?utm_source=newsletter");

        Assert.Equal("https://gifts.example/products/handmade-ceramic-tea-mug", capture.Link?.AbsoluteUri);
        Assert.Equal("gifts.example", capture.StoreName);
        Assert.Equal("Handmade ceramic tea mug", capture.Name);
    }

    [Fact]
    public void FromQuery_UsesTheSharedTitleWhenTheTextIsOnlyALink()
    {
        var capture = ProductCapture.FromQuery(url: null, text: "https://www.target.com/p/ember-mug-2/-/A-1", title: "Ember Mug 2 : Target");

        Assert.Equal("https://www.target.com/p/ember-mug-2/-/A-1", capture.Link?.AbsoluteUri);
        Assert.Equal("Ember Mug 2", capture.Name);
    }

    [Fact]
    public void FromQuery_IgnoresAStoreNameUsedAsTheTitle()
    {
        var capture = ProductCapture.FromQuery(url: "https://www.amazon.com/dp/B0CJZMP7L1", title: "Amazon.com");

        Assert.Null(capture.Name);
        Assert.True(capture.HasLink);
    }

    [Fact]
    public void FromQuery_AcceptsAnIdeaWithoutALink()
    {
        var capture = ProductCapture.FromQuery(url: null, text: "  Cozy  wool socks ");

        Assert.Null(capture.Link);
        Assert.Equal("Cozy wool socks", capture.Name);
        Assert.False(capture.IsEmpty);
    }

    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("data:image/png;base64,AAAA")]
    [InlineData("/relative.jpg")]
    public void FromQuery_RejectsUnsafeImages(string image)
    {
        var capture = ProductCapture.FromQuery("https://shop.example/products/x", image: image);

        Assert.Null(capture.Image);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("free")]
    [InlineData("-5")]
    public void FromQuery_IgnoresUnreadablePrices(string? price)
    {
        Assert.Null(ProductCapture.FromQuery("https://shop.example/products/x", price: price).Price);
    }

    [Fact]
    public void FromQuery_IsEmptyWithoutAnything()
    {
        Assert.True(ProductCapture.FromQuery(null).IsEmpty);
        Assert.True(ProductCapture.FromQuery("not-a-web-address", title: "   ").IsEmpty);
    }

    [Fact]
    public void ApplyTo_FillsOnlyEmptyFields()
    {
        var capture = ProductCapture.FromQuery("https://shop.example/products/teapot", title: "Teapot", price: "24", image: "https://cdn.example/teapot.jpg");
        var item = new WishlistItemModel { Name = "Grandma's teapot", Price = 30m };

        capture.ApplyTo(item);

        Assert.Equal("Grandma's teapot", item.Name);
        Assert.Equal(30m, item.Price);
        Assert.Equal("https://cdn.example/teapot.jpg", item.Image);
        Assert.Equal("https://shop.example/products/teapot", item.Url);
        Assert.Equal("shop.example", item.WhereToBuy);
    }

    [Fact]
    public void ToQueryString_RoundTripsThroughFromQuery()
    {
        var original = ProductCapture.FromQuery(
            "https://shop.example/products/tea-set?utm_source=x",
            title: "Tea set & cups",
            description: "Six cups",
            price: "1,299.50",
            image: "https://cdn.example/tea set.jpg");

        var query = original.ToQueryString();
        var values = query.TrimStart('?')
            .Split('&')
            .Select(part => part.Split('=', 2))
            .ToDictionary(pair => pair[0], pair => Uri.UnescapeDataString(pair[1]));
        var copy = ProductCapture.FromQuery(
            values["url"],
            title: values["title"],
            description: values["description"],
            price: values["price"],
            image: values["image"]);

        Assert.StartsWith("?url=https%3A%2F%2Fshop.example%2Fproducts%2Ftea-set&title=Tea%20set%20%26%20cups", query);
        Assert.Equal(original, copy);
    }

    [Fact]
    public void ToQueryString_IsEmptyWithoutDetails()
    {
        Assert.Equal(string.Empty, ProductCapture.FromQuery(null).ToQueryString());
    }
}