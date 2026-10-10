using OpenWish.Shared.Products;
using Xunit;

namespace OpenWish.Shared.Tests.Products;

public class ProductLinkTests
{
    [Theory]
    [InlineData("https://www.target.com/p/item/-/A-1", "https://www.target.com/p/item/-/A-1")]
    [InlineData("  https://example.com/product  ", "https://example.com/product")]
    [InlineData("www.rei.com/product/12345/patagonia-nano-puff", "https://www.rei.com/product/12345/patagonia-nano-puff")]
    [InlineData("etsy.com/listing/123/handmade-mug", "https://etsy.com/listing/123/handmade-mug")]
    [InlineData("(see https://example.com/products/tea-mug).", "https://example.com/products/tea-mug")]
    [InlineData("https://en.wikipedia.org/wiki/Mug_(cup)", "https://en.wikipedia.org/wiki/Mug_(cup)")]
    public void TryParse_FindsTheLink(string text, string expected)
    {
        Assert.True(ProductLink.TryParse(text, out var link));
        Assert.Equal(expected, link.Url.AbsoluteUri);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-a-web-address")]
    [InlineData("a cozy blanket")]
    [InlineData("ftp://example.com/file")]
    [InlineData("javascript:alert(1)")]
    public void TryParse_RejectsTextWithoutAWebLink(string? text)
    {
        Assert.False(ProductLink.TryParse(text, out _));
    }

    [Theory]
    [InlineData("https://example.com/item", true)]
    [InlineData("Look at this http://shop.example/x", true)]
    [InlineData("Try www.rei.com/product/1 today", true)]
    [InlineData("not-a-web-address", false)]
    [InlineData("a cozy blanket", false)]
    [InlineData("email me at me@www.example.com", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void ContainsWebAddress_FindsLinksInText(string? text, bool expected)
    {
        Assert.Equal(expected, ProductLink.ContainsWebAddress(text));
    }

    [Theory]
    [InlineData("Check out this deal on Amazon\nLEGO Icons Orchid Plant Building Set\nhttps://a.co/d/7oL8hBq", "LEGO Icons Orchid Plant Building Set")]
    [InlineData("Amazon.com: Stanley Quencher Tumbler 40 oz : Home & Kitchen https://www.amazon.com/dp/B0CJZMP7L1", "Stanley Quencher Tumbler 40 oz")]
    [InlineData("Check out Ember Mug 2 from Target: https://www.target.com/p/ember-mug-2/-/A-1", "Ember Mug 2")]
    [InlineData("Personalized Leather Journal by MakerShop https://www.etsy.com/listing/1/journal", "Personalized Leather Journal by MakerShop")]
    [InlineData("\"Hooked on Phonics Learn to Read\" https://www.amazon.com/dp/B000000001", "Hooked on Phonics Learn to Read")]
    public void TryParse_UsesSharedTextAsATitleHint(string text, string expectedHint)
    {
        Assert.True(ProductLink.TryParse(text, out var link));
        Assert.Equal(expectedHint, link.TitleHint);
    }

    [Theory]
    [InlineData("https://a.co/d/7oL8hBq")]
    [InlineData("Check this out! https://a.co/d/7oL8hBq")]
    [InlineData("Check out this item on Amazon https://a.co/d/7oL8hBq")]
    public void TryParse_IgnoresGenericShareText(string text)
    {
        Assert.True(ProductLink.TryParse(text, out var link));
        Assert.Null(link.TitleHint);
    }

    [Theory]
    [InlineData(
        "https://www.amazon.com/Stanley-Quencher-FlowState-Stainless-Insulated/dp/B0CJZMP7L1/ref=sr_1_3?crid=2X&keywords=stanley&qid=1&sr=8-3&th=1",
        "https://www.amazon.com/dp/B0CJZMP7L1")]
    [InlineData("https://smile.amazon.co.uk/gp/product/b0cjzmp7l1?psc=1", "https://www.amazon.co.uk/dp/B0CJZMP7L1")]
    [InlineData(
        "https://www.target.com/p/ember-mug-2/-/A-87654321?preselect=1234&utm_source=newsletter&fbclid=abc#lnk=sametab",
        "https://www.target.com/p/ember-mug-2/-/A-87654321?preselect=1234")]
    [InlineData(
        "https://www.etsy.com/listing/1234567/handmade-mug?ga_order=most_relevant&ref=sr_gallery-1-1&frs=1",
        "https://www.etsy.com/listing/1234567/handmade-mug")]
    [InlineData(
        "https://www.walmart.com/ip/LEGO-Millennium-Falcon/123456789?athbdg=L1600&from=/search",
        "https://www.walmart.com/ip/LEGO-Millennium-Falcon/123456789")]
    [InlineData(
        "https://shop.example/products/the-tee?variant=4242&_pos=1&_sid=abc&_ss=r",
        "https://shop.example/products/the-tee?variant=4242")]
    [InlineData("https://www.bestbuy.com/site/airpods/6447382.p?skuId=6447382", "https://www.bestbuy.com/site/airpods/6447382.p?skuId=6447382")]
    [InlineData("https://example.com/item?tag=blue", "https://example.com/item?tag=blue")]
    [InlineData("https://example.com/item#:~:text=price", "https://example.com/item")]
    [InlineData("https://example.com:443/item?gclid=1", "https://example.com/item")]
    public void Clean_RemovesTrackingAndKeepsVariants(string input, string expected)
    {
        Assert.Equal(expected, ProductLink.Clean(new Uri(input)).AbsoluteUri);
    }

    [Theory]
    [InlineData("https://gifts.example/products/handmade-ceramic-tea-mug?utm_source=newsletter", "Handmade ceramic tea mug")]
    [InlineData("https://www.walmart.com/ip/LEGO-Star-Wars-Millennium-Falcon-75192/123456789", "LEGO Star Wars Millennium Falcon 75192")]
    [InlineData("https://www.bestbuy.com/site/apple-airpods-pro-2nd-generation-white/6447382.p?skuId=6447382", "Apple airpods pro 2nd generation white")]
    [InlineData("https://www.etsy.com/listing/1234567890/personalized-leather-journal", "Personalized leather journal")]
    [InlineData("https://www.amazon.com/Stanley-Quencher-Tumbler/dp/B0CJZMP7L1/ref=sr_1_3", "Stanley Quencher Tumbler")]
    [InlineData("https://www.kohls.com/product/prd-5432109/cozy-sherpa-throw.jsp", "Cozy sherpa throw")]
    [InlineData("https://www.ikea.com/us/en/p/kallax-shelf-unit-white-80275887/", "Kallax shelf unit white")]
    [InlineData("https://www.wayfair.com/furniture/pdp/oak-coffee-table-w001234567.html", "Oak coffee table")]
    [InlineData("https://www.homedepot.com/p/Milwaukee-M18-FUEL-Hammer-Drill-2904-20/315424434", "Milwaukee M18 FUEL Hammer Drill 2904 20")]
    [InlineData("https://www.lego.com/en-us/product/orchid-10311", "Orchid 10311")]
    [InlineData("https://shop.example/products/teapot", "Teapot")]
    public void GuessNameFromUrl_ReadsTheProductSlug(string url, string expected)
    {
        Assert.Equal(expected, ProductLink.GuessNameFromUrl(new Uri(url)));
    }

    [Theory]
    [InlineData("https://a.co/d/7oL8hBq")]
    [InlineData("https://www.amazon.com/dp/B0CJZMP7L1")]
    [InlineData("https://example.com/")]
    [InlineData("https://example.com/item/123456789")]
    [InlineData("https://shop.example/collections/products")]
    [InlineData("https://shop.example/cart")]
    public void GuessNameFromUrl_ReturnsNullWithoutDescriptiveWords(string url)
    {
        Assert.Null(ProductLink.GuessNameFromUrl(new Uri(url)));
    }

    [Theory]
    [InlineData("https://www.amazon.com/dp/B0CJZMP7L1", "Amazon")]
    [InlineData("https://a.co/d/abc", "Amazon")]
    [InlineData("https://www.amazon.co.uk/dp/B0CJZMP7L1", "Amazon")]
    [InlineData("https://www.ebay.com/itm/123", "eBay")]
    [InlineData("https://www.homedepot.com/p/x/1", "The Home Depot")]
    [InlineData("https://m.target.com/p/x", "Target")]
    [InlineData("https://shop.example/products/x", "shop.example")]
    [InlineData("https://www.allbirds.com/products/x", "Allbirds")]
    [InlineData("https://www.myshop.com/x", "myshop.com")]
    public void GetStoreName_UsesFriendlyNames(string url, string expected)
    {
        Assert.Equal(expected, ProductLink.GetStoreName(new Uri(url)));
    }

    [Theory]
    [InlineData("Ember Mug 2 : Target", "Target", "Ember Mug 2")]
    [InlineData("Ember Mug 2 - Target", "Target", "Ember Mug 2")]
    [InlineData("Men's Tree Runners | Allbirds", "allbirds.com", "Men's Tree Runners")]
    [InlineData("Walmart.com | Cozy Throw", "Walmart", "Cozy Throw")]
    [InlineData("Nike Air Max 90 - Men's Shoes", "Nike", "Nike Air Max 90 - Men's Shoes")]
    [InlineData("Cordless Drill | The Home Depot", "The Home Depot", "Cordless Drill")]
    [InlineData("Amazon.com: Kindle Paperwhite : Electronics", "Amazon", "Kindle Paperwhite")]
    [InlineData("  Fish &amp; Chips&nbsp;Plate  ", null, "Fish & Chips Plate")]
    [InlineData("   ", null, null)]
    public void CleanProductName_RemovesStoreNoise(string input, string? store, string? expected)
    {
        Assert.Equal(expected, ProductLink.CleanProductName(input, store));
    }

    [Fact]
    public void CleanProductName_ShortensLongMarketplaceTitles()
    {
        const string title = "Apple AirPods Pro 2 Wireless Earbuds, Active Noise Cancellation, Hearing Aid Feature, Bluetooth Headphones, Transparency, Personalized Spatial Audio";

        var result = ProductLink.CleanProductName(title, "Amazon");

        Assert.Equal("Apple AirPods Pro 2 Wireless Earbuds", result);
    }

    [Fact]
    public void CleanProductName_TruncatesLongTitlesWithoutCommas()
    {
        var title = string.Join(' ', Enumerable.Repeat("blanket", 30));

        var result = ProductLink.CleanProductName(title, null);

        Assert.NotNull(result);
        Assert.True(result.Length <= ProductLink.MaxNameLength);
        Assert.EndsWith("…", result);
    }

    [Fact]
    public void CleanDescription_LimitsLengthAndSkipsTheName()
    {
        Assert.Null(ProductLink.CleanDescription("Tea Mug", "tea mug"));
        var result = ProductLink.CleanDescription(new string('a', 20) + " " + string.Join(' ', Enumerable.Repeat("word", 200)));
        Assert.NotNull(result);
        Assert.True(result.Length <= ProductLink.MaxDescriptionLength);
    }

    [Theory]
    [InlineData("12", 12)]
    [InlineData("34.95", 34.95)]
    [InlineData("$1,299.99", 1299.99)]
    [InlineData("1.299,99 €", 1299.99)]
    [InlineData("€12,50", 12.5)]
    [InlineData("$10 - $20", 10)]
    [InlineData("1 299,00 kr", 1299)]
    [InlineData("34.999", 35)]
    [InlineData("USD 1,000", 1000)]
    public void TryParsePrice_ReadsCommonPriceFormats(string text, decimal expected)
    {
        Assert.True(ProductLink.TryParsePrice(text, out var price));
        Assert.Equal(expected, price);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Free")]
    [InlineData("0.00")]
    [InlineData("99999999999")]
    [InlineData("-5")]
    [InlineData("-$12.00")]
    public void TryParsePrice_RejectsMissingOrUnrealisticPrices(string? text)
    {
        Assert.False(ProductLink.TryParsePrice(text, out _));
    }

    [Theory]
    [InlineData("https://shop.example/products/mug", "Gifts Example", "Gifts Example")]
    [InlineData("https://www.amazon.com/dp/B0CJZMP7L1", "Amazon.com", "Amazon")]
    [InlineData("https://www.shop.example/products/mug", "X", "shop.example")]
    public void GetStoreName_UsesTheSiteNameForUnknownStores(string url, string siteName, string expected)
    {
        Assert.Equal(expected, ProductLink.GetStoreName(new Uri(url), siteName));
    }
}