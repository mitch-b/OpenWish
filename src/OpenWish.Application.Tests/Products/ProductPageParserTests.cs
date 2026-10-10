using OpenWish.Application.Products;
using Xunit;

namespace OpenWish.Application.Tests.Products;

public class ProductPageParserTests
{
    private static readonly Uri _storeUri = new("https://shop.example/products/lilac-tumbler");

    [Fact]
    public void Parse_ReadsJsonLdProductBeforeOtherMarkup()
    {
        const string html = """
            <html><head>
            <title>Wrong title</title>
            <meta property="og:title" content="Open Graph title">
            <script type="application/ld+json">
            {
              "@context": "https://schema.org",
              "@type": "Product",
              "name": "Lilac Tumbler",
              "description": "Keeps drinks cold.",
              "image": ["https://cdn.shop.example/lilac.jpg", "https://cdn.shop.example/lilac-2.jpg"],
              "offers": { "@type": "Offer", "price": "34.95", "priceCurrency": "USD" }
            }
            </script>
            </head><body><h1>Heading</h1></body></html>
            """;

        var details = ProductPageParser.Parse(html, _storeUri);

        Assert.False(details.IsBlocked);
        Assert.Equal("Lilac Tumbler", details.Name);
        Assert.Equal("Keeps drinks cold.", details.Description);
        Assert.Equal(34.95m, details.Price);
        Assert.Equal("https://cdn.shop.example/lilac.jpg", details.ImageCandidates[0]);
    }

    [Fact]
    public void Parse_FindsProductInsideGraphWithAggregateOffer()
    {
        const string html = """
            <script type="application/ld+json">
            {"@context":"https://schema.org","@graph":[
              {"@type":"WebSite","name":"Shop"},
              {"@type":["Product","Thing"],"name":"Wool Scarf",
               "image":{"@type":"ImageObject","url":"/images/scarf.jpg"},
               "offers":{"@type":"AggregateOffer","lowPrice":"19.5","highPrice":"29"}}
            ]}
            </script>
            """;

        var details = ProductPageParser.Parse(html, _storeUri);

        Assert.Equal("Wool Scarf", details.Name);
        Assert.Equal(19.5m, details.Price);
        Assert.Equal("/images/scarf.jpg", details.ImageCandidates[0]);
    }

    [Fact]
    public void Parse_ReadsProductGroupVariantPriceAndToleratesRawNewlines()
    {
        const string html = "<script type=\"application/ld+json\">[{\"@type\":\"BreadcrumbList\"}," +
                            "{\"@type\":\"ProductGroup\",\"name\":\"Trail Runner\",\"description\":\"Line one\nLine two\"," +
                            "\"hasVariant\":[{\"@type\":\"Product\",\"offers\":{\"price\":89}}]}]</script>";

        var details = ProductPageParser.Parse(html, _storeUri);

        Assert.Equal("Trail Runner", details.Name);
        Assert.Equal("Line one Line two", details.Description);
        Assert.Equal(89m, details.Price);
    }

    [Fact]
    public void Parse_ReadsAmazonProductMarkup()
    {
        const string html = """
            <html><head><title>Amazon.com: Lilac Tumbler, 30 oz : Home &amp; Kitchen</title></head><body>
            <span id="productTitle">  Lilac Tumbler, 30 oz  </span>
            <div id="corePriceDisplay_desktop_feature_div">
              <span class="a-price priceToPay"><span class="a-offscreen">$35.00</span></span>
            </div>
            <div id="imgTagWrapperId">
              <img id="landingImage" src="https://m.media-amazon.com/small.jpg"
                   data-old-hires="https://m.media-amazon.com/large.jpg">
            </div>
            <div id="feature-bullets"><ul>
              <li><span class="a-list-item">Double-wall insulation</span></li>
              <li><span class="a-list-item">Fits most cup holders</span></li>
            </ul></div>
            </body></html>
            """;

        var details = ProductPageParser.Parse(html, new Uri("https://www.amazon.com/dp/B0CRMZHDG8"));

        Assert.Equal("Lilac Tumbler, 30 oz", details.Name);
        Assert.Equal(35m, details.Price);
        Assert.Equal("https://m.media-amazon.com/large.jpg", details.ImageCandidates[0]);
        Assert.Equal("Double-wall insulation Fits most cup holders", details.Description);
    }

    [Fact]
    public void Parse_CombinesAmazonWholeAndFractionPrice()
    {
        const string html = """
            <span id="productTitle">Desk Lamp</span>
            <span class="a-price"><span class="a-price-whole">1,249.</span><span class="a-price-fraction">99</span></span>
            <img id="landingImage" data-a-dynamic-image='{"https://m.media-amazon.com/s.jpg":[100,100],"https://m.media-amazon.com/l.jpg":[1500,1500]}'>
            """;

        var details = ProductPageParser.Parse(html, new Uri("https://www.amazon.com/dp/B000000001"));

        Assert.Equal(1249.99m, details.Price);
        Assert.Equal("https://m.media-amazon.com/l.jpg", details.ImageCandidates[0]);
    }

    [Fact]
    public void Parse_CombinesEuropeanAmazonWholeAndFractionPrice()
    {
        const string html = """
            <span id="productTitle">Schreibtischlampe</span>
            <span class="a-price"><span class="a-price-whole">1.299<span class="a-price-decimal">,</span></span><span class="a-price-fraction">99</span></span>
            """;

        var details = ProductPageParser.Parse(html, new Uri("https://www.amazon.de/dp/B000000001"));

        Assert.Equal(1299.99m, details.Price);
    }

    [Theory]
    [InlineData("<title>Robot or human?</title><body>Activate and hold the button</body>")]
    [InlineData("<title>Just a moment...</title><script>window._cf_chl_opt={}</script>")]
    [InlineData("<title>Amazon.com</title><form action=\"/errors/validateCaptcha\">Type the characters you see</form>")]
    [InlineData("<title>Access Denied</title><h1>Access Denied</h1>")]
    public void Parse_DetectsBotChecksAndBlockedPages(string html)
    {
        var details = ProductPageParser.Parse(html, _storeUri);

        Assert.True(details.IsBlocked);
        Assert.Null(details.Name);
    }

    [Fact]
    public void Parse_ReadsOpenGraphTwitterPriceAndSiteName()
    {
        const string html = """
            <meta property="og:site_name" content="Gifts Example">
            <meta property="og:title" content="Ceramic Mug &amp; Saucer | Gifts Example">
            <meta property="og:image:secure_url" content="https://cdn.gifts.example/mug.jpg?w=1200&amp;h=1200">
            <meta name="twitter:label1" content="Price">
            <meta name="twitter:data1" content="$24.00 USD">
            """;

        var details = ProductPageParser.Parse(html, _storeUri);

        Assert.Equal("Ceramic Mug & Saucer | Gifts Example", details.Name);
        Assert.Equal("Gifts Example", details.SiteName);
        Assert.Equal(24m, details.Price);
        Assert.Equal("https://cdn.gifts.example/mug.jpg?w=1200&h=1200", details.ImageCandidates[0]);
    }

    [Fact]
    public void Parse_ReadsMicrodataWhenStructuredDataIsMissing()
    {
        const string html = """
            <div itemscope itemtype="https://schema.org/Product">
              <h2 itemprop="name">Board Game</h2>
              <img itemprop="image" src="/board-game.jpg">
              <span itemprop="price" content="42.00">$42</span>
            </div>
            """;

        var details = ProductPageParser.Parse(html, _storeUri);

        Assert.Equal("Board Game", details.Name);
        Assert.Equal(42m, details.Price);
        Assert.Contains("/board-game.jpg", details.ImageCandidates);
    }

    [Fact]
    public void Parse_IgnoresMalformedJsonLd()
    {
        const string html = """
            <script type="application/ld+json">{ "@type": "Product", "name": </script>
            <meta property="og:title" content="Fallback Name">
            """;

        var details = ProductPageParser.Parse(html, _storeUri);

        Assert.Equal("Fallback Name", details.Name);
    }
}