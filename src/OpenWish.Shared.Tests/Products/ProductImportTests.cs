using OpenWish.Shared.Models;
using OpenWish.Shared.Products;
using Xunit;

namespace OpenWish.Shared.Tests.Products;

public class ProductImportTests
{
    private static readonly Uri _targetLink = new("https://www.target.com/p/ember-mug-2/-/A-87654321?utm_source=newsletter");

    [Fact]
    public void Apply_FillsEmptyFieldsFromTheStorePage()
    {
        var item = new WishlistItemModel();
        var product = new ProductModel
        {
            Name = " Ember Mug 2 ",
            Description = "Keeps coffee hot.",
            Price = 129.995m,
            ImageUrl = "https://target.scene7.com/mug.jpg",
            Url = "https://www.target.com/p/ember-mug-2/-/A-87654321",
            StoreName = "Target"
        };

        var result = ProductImport.Apply(item, _targetLink, product);

        Assert.Equal(ProductImportKind.Imported, result.Kind);
        Assert.True(result.FoundDetails);
        Assert.Equal("Imported details from Target. Review them before saving.", result.Message);
        Assert.Equal("Ember Mug 2", item.Name);
        Assert.Equal("Keeps coffee hot.", item.Description);
        Assert.Equal(130.00m, item.Price);
        Assert.Equal("https://target.scene7.com/mug.jpg", item.Image);
        Assert.Equal("https://www.target.com/p/ember-mug-2/-/A-87654321", item.Url);
        Assert.Equal("Target", item.WhereToBuy);
    }

    [Fact]
    public void Apply_KeepsDetailsThePersonAlreadyEntered()
    {
        var item = new WishlistItemModel
        {
            Name = "My mug",
            Description = "The blue one",
            Price = 99m,
            Image = "https://example.com/mine.jpg",
            Url = "https://example.com/mine",
            WhereToBuy = "Anywhere"
        };
        var product = new ProductModel { Name = "Ember Mug 2", Description = "Hot", Price = 129m, ImageUrl = "https://example.com/new.jpg", StoreName = "Target" };

        var result = ProductImport.Apply(item, _targetLink, product);

        Assert.Equal(ProductImportKind.KeptExisting, result.Kind);
        Assert.True(result.FoundDetails);
        Assert.Equal("My mug", item.Name);
        Assert.Equal("The blue one", item.Description);
        Assert.Equal(99m, item.Price);
        Assert.Equal("https://example.com/mine.jpg", item.Image);
        Assert.Equal("https://example.com/mine", item.Url);
        Assert.Equal("Anywhere", item.WhereToBuy);
    }

    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("data:image/png;base64,AAAA")]
    [InlineData("/images/mug.jpg")]
    [InlineData("not an image")]
    public void Apply_IgnoresImagesThatAreNotWebAddresses(string imageUrl)
    {
        var item = new WishlistItemModel();

        ProductImport.Apply(item, _targetLink, new ProductModel { Name = "Mug", ImageUrl = imageUrl });

        Assert.Null(item.Image);
    }

    [Fact]
    public void Apply_IgnoresNegativePricesAndUnsafeProductLinks()
    {
        var item = new WishlistItemModel();

        ProductImport.Apply(item, _targetLink, new ProductModel { Name = "Mug", Price = -1m, Url = "javascript:alert(1)" });

        Assert.Null(item.Price);
        Assert.Equal("https://www.target.com/p/ember-mug-2/-/A-87654321", item.Url);
    }

    [Fact]
    public void Apply_ExplainsWhenOnlyTheLinkProvidedAName()
    {
        var item = new WishlistItemModel();
        var link = new Uri("https://www.walmart.com/ip/LEGO-Millennium-Falcon/123456789");

        var result = ProductImport.Apply(item, link, new ProductModel { Name = "LEGO Millennium Falcon", StoreName = "Walmart", FromLinkOnly = true });

        Assert.Equal(ProductImportKind.FromLink, result.Kind);
        Assert.False(result.FoundDetails);
        Assert.Equal("LEGO Millennium Falcon", item.Name);
        Assert.Equal("Walmart", item.WhereToBuy);
        Assert.Equal("https://www.walmart.com/ip/LEGO-Millennium-Falcon/123456789", item.Url);
        Assert.StartsWith("Walmart didn't share product details", result.Message);
    }

    [Fact]
    public void Apply_ReportsALinkOnlyResult()
    {
        var item = new WishlistItemModel();

        var result = ProductImport.Apply(item, new Uri("https://a.co/d/abc"), new ProductModel { StoreName = "Amazon", FromLinkOnly = true });

        Assert.Equal(ProductImportKind.LinkOnly, result.Kind);
        Assert.Equal("No product details were found. The product link is ready; add any details you want.", result.Message);
        Assert.Equal("https://a.co/d/abc", item.Url);
    }

    [Fact]
    public void Apply_UsesAGentlerMessageWhenTheItemAlreadyHasAName()
    {
        var item = new WishlistItemModel { Name = "Shared from the bookmarklet" };

        var result = ProductImport.Apply(item, new Uri("https://www.walmart.com/ip/x/1"), new ProductModel { Name = "X", FromLinkOnly = true });

        Assert.Equal(ProductImportKind.LinkOnly, result.Kind);
        Assert.True(result.ItemHasName);
        Assert.Equal("The product link is ready. Review the details before saving.", result.Message);
        Assert.Equal("Shared from the bookmarklet", item.Name);
    }

    [Fact]
    public void Apply_StillFillsTheLinkWhenTheLookupIsUnavailable()
    {
        var item = new WishlistItemModel();

        var result = ProductImport.Apply(item, _targetLink, product: null);

        Assert.Equal(ProductImportKind.Unavailable, result.Kind);
        Assert.Equal("Target", result.StoreName);
        Assert.Equal("https://www.target.com/p/ember-mug-2/-/A-87654321", item.Url);
        Assert.Equal("Target", item.WhereToBuy);
        Assert.Contains("The link is ready", result.Message);
    }

    [Fact]
    public void Apply_ReportsAPageWithoutDetailsAsLinkOnly()
    {
        var item = new WishlistItemModel();

        var result = ProductImport.Apply(item, new Uri("https://shop.example/products/x"), new ProductModel());

        Assert.Equal(ProductImportKind.LinkOnly, result.Kind);
        Assert.Equal("shop.example", result.StoreName);
    }

    [Fact]
    public void SeedLink_KeepsTheLinkWhenTheLookupNeverFinishes()
    {
        var item = new WishlistItemModel { Name = "Mug" };

        var seed = ProductImport.SeedLink(item, _targetLink);

        Assert.Equal("https://www.target.com/p/ember-mug-2/-/A-87654321", item.Url);
        Assert.Equal("Target", item.WhereToBuy);
        Assert.Equal(item.Url, seed.Url);
        Assert.Equal("Target", seed.WhereToBuy);
    }

    [Fact]
    public void SeedLink_LeavesALinkAndStoreThePersonEntered()
    {
        var item = new WishlistItemModel { Url = "https://mine.example/mug", WhereToBuy = "Local shop" };

        var seed = ProductImport.SeedLink(item, _targetLink);

        Assert.Null(seed.Url);
        Assert.Null(seed.WhereToBuy);
        Assert.Equal("https://mine.example/mug", item.Url);
        Assert.Equal("Local shop", item.WhereToBuy);
    }

    [Fact]
    public void Apply_ReplacesSeededPlaceholdersWithThePagesOwnLinkAndStore()
    {
        var shortLink = new Uri("https://amzn.to/3xYzAbC");
        var item = new WishlistItemModel();
        var seed = ProductImport.SeedLink(item, shortLink);

        var result = ProductImport.Apply(item, shortLink, new ProductModel
        {
            Name = "Echo Dot",
            Url = "https://www.amazon.com/dp/B09B8V1LZ3",
            StoreName = "Amazon.com"
        }, seed);

        Assert.Equal(ProductImportKind.Imported, result.Kind);
        Assert.Equal("https://www.amazon.com/dp/B09B8V1LZ3", item.Url);
        Assert.Equal("Amazon.com", item.WhereToBuy);
    }

    [Fact]
    public void Apply_KeepsSeededFieldsThePersonChangedDuringTheLookup()
    {
        var item = new WishlistItemModel();
        var seed = ProductImport.SeedLink(item, _targetLink);
        item.Url = "https://mine.example/mug";
        item.WhereToBuy = "Local shop";

        ProductImport.Apply(item, _targetLink, new ProductModel
        {
            Name = "Ember Mug 2",
            Url = "https://www.target.com/p/ember-mug-2/-/A-11111111",
            StoreName = "Target"
        }, seed);

        Assert.Equal("https://mine.example/mug", item.Url);
        Assert.Equal("Local shop", item.WhereToBuy);
        Assert.Equal("Ember Mug 2", item.Name);
    }
}