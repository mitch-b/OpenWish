using System.Net;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using OpenWish.Application.Products;
using OpenWish.Application.Services;
using Xunit;

namespace OpenWish.Application.Tests.Products;

public class ProductServiceImportTests
{
    private const string ProductUrl = "https://8.8.8.8/products/lilac-stainless-tumbler";

    [Fact]
    public async Task TryScrapeProductFromUrl_CleansNameAndStoresStructuredDetails()
    {
        var handler = new StubHandler(_ => Html("""
            <meta property="og:site_name" content="Gifts Example">
            <meta property="og:title" content="Lilac Tumbler &amp; Straw | Gifts Example">
            <meta property="og:description" content="Keeps drinks cold for hours.">
            <meta property="product:price:amount" content="34.999">
            <meta property="og:image" content="/images/lilac.jpg">
            """));
        var service = CreateService(handler);

        var product = await service.TryScrapeProductFromUrl(ProductUrl + "?utm_source=newsletter&color=lilac");

        Assert.NotNull(product);
        Assert.Equal("Lilac Tumbler & Straw", product.Name);
        Assert.Equal("Keeps drinks cold for hours.", product.Description);
        Assert.Equal(35m, product.Price);
        Assert.Equal("https://8.8.8.8/images/lilac.jpg", product.ImageUrl);
        Assert.Equal(ProductUrl + "?color=lilac", product.Url);
        Assert.Equal("Gifts Example", product.StoreName);
        Assert.False(product.FromLinkOnly);
        Assert.Equal(ProductUrl + "?color=lilac", Assert.Single(handler.Requests));
    }

    [Fact]
    public async Task TryScrapeProductFromUrl_SendsOneHonestUserAgent()
    {
        var handler = new StubHandler(_ => Html("<meta property='og:title' content='Lamp'>"));
        var service = CreateService(handler);

        await service.TryScrapeProductFromUrl(ProductUrl);

        var userAgent = Assert.Single(handler.UserAgents);
        Assert.Equal(ProductService.UserAgent, userAgent);
    }

    [Fact]
    public async Task TryScrapeProductFromUrl_UsesLinkDetailsWhenStoreBlocksTheRequest()
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.Forbidden)
        {
            Content = new StringContent("<title>Access Denied</title>")
        });
        var service = CreateService(handler);

        var product = await service.TryScrapeProductFromUrl(ProductUrl + "?utm_campaign=sale");

        Assert.NotNull(product);
        Assert.True(product.FromLinkOnly);
        Assert.Equal("Lilac stainless tumbler", product.Name);
        Assert.Equal(ProductUrl, product.Url);
        Assert.Equal("8.8.8.8", product.StoreName);
        Assert.Null(product.Price);
        Assert.Single(handler.Requests);
    }

    [Fact]
    public async Task TryScrapeProductFromUrl_RetriesABotCheckOnceBeforeUsingTheLink()
    {
        var handler = new StubHandler(_ => Html("<title>Robot or human?</title><p>Activate and hold the button.</p>"));
        var service = CreateService(handler);

        var product = await service.TryScrapeProductFromUrl(ProductUrl);

        Assert.NotNull(product);
        Assert.True(product.FromLinkOnly);
        Assert.Equal(2, handler.Requests.Count);
    }

    [Fact]
    public async Task TryScrapeProductFromUrl_UsesTheRetryWhenTheStoreRecovers()
    {
        var calls = 0;
        var handler = new StubHandler(_ => ++calls == 1
            ? new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
            : Html("<meta property='og:title' content='Desk Lamp'><meta property='og:price:amount' content='49'>"));
        var service = CreateService(handler);

        var product = await service.TryScrapeProductFromUrl(ProductUrl);

        Assert.NotNull(product);
        Assert.False(product.FromLinkOnly);
        Assert.Equal("Desk Lamp", product.Name);
        Assert.Equal(49m, product.Price);
    }

    [Fact]
    public async Task TryScrapeProductFromUrl_UsesShareTextWhenThePageCannotBeRead()
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.Forbidden));
        var service = CreateService(handler);

        var product = await service.TryScrapeProductFromUrl(
            $"Check out this STANLEY Quencher Tumbler, Lilac on 8.8.8.8 {ProductUrl}?utm_source=share");

        Assert.NotNull(product);
        Assert.True(product.FromLinkOnly);
        Assert.Equal("STANLEY Quencher Tumbler, Lilac", product.Name);
        Assert.Equal(ProductUrl, product.Url);
    }

    [Fact]
    public async Task TryScrapeProductFromUrl_ReadsShareTextThatFollowsTheLink()
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.Forbidden));
        var service = CreateService(handler);

        var product = await service.TryScrapeProductFromUrl($"{ProductUrl}?utm_source=share\nSTANLEY Quencher Tumbler, Lilac");

        Assert.NotNull(product);
        Assert.Equal("STANLEY Quencher Tumbler, Lilac", product.Name);
        Assert.Equal(ProductUrl, product.Url);
        Assert.Equal(ProductUrl, Assert.Single(handler.Requests));
    }

    [Fact]
    public async Task TryScrapeProductFromUrl_KeepsEachCallersShareTextOutOfTheCache()
    {
        var handler = new StubHandler(_ => Html("<meta property='og:price:amount' content='34'><meta property='og:image' content='/lilac.jpg'>"));
        using var cache = new ProductLookupCache();
        var service = CreateService(handler, cache);

        var first = await service.TryScrapeProductFromUrl($"Secret gift for Sam {ProductUrl}");
        var second = await service.TryScrapeProductFromUrl($"Lilac tumbler for camping {ProductUrl}");
        var third = await service.TryScrapeProductFromUrl(ProductUrl);

        Assert.Equal("Secret gift for Sam", first?.Name);
        Assert.Equal("Lilac tumbler for camping", second?.Name);
        Assert.Equal(34m, second?.Price);
        Assert.Equal("Lilac stainless tumbler", third?.Name);
        Assert.Single(handler.Requests);
    }

    [Fact]
    public async Task TryScrapeProductFromUrl_ReadsLargePagesInsteadOfFailing()
    {
        var html = "<html><head><meta property='og:title' content='Big Page Gift'>" +
                   "<meta property='og:price:amount' content='18.25'></head><body>" +
                   new string('x', 5 * 1024 * 1024) + "</body></html>";
        var service = CreateService(new StubHandler(_ => Html(html)));

        var product = await service.TryScrapeProductFromUrl(ProductUrl);

        Assert.NotNull(product);
        Assert.Equal("Big Page Gift", product.Name);
        Assert.Equal(18.25m, product.Price);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task TryScrapeProductFromUrl_DecodesDeclaredCharacterSets(bool declareInHeader)
    {
        var latin1 = Encoding.Latin1.GetBytes("<meta charset='iso-8859-1'><meta property='og:title' content='Café Crème Mug'><meta property='og:price:amount' content='18'>");
        var service = CreateService(new StubHandler(_ =>
        {
            var content = new ByteArrayContent(latin1);
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/html");
            if (declareInHeader)
            {
                content.Headers.ContentType.CharSet = "iso-8859-1";
            }

            return new HttpResponseMessage(HttpStatusCode.OK) { Content = content };
        }));

        var product = await service.TryScrapeProductFromUrl(ProductUrl);

        Assert.Equal("Café Crème Mug", product?.Name);
    }

    [Fact]
    public async Task TryScrapeProductFromUrl_UsesDirectImageLinksAsTheImage()
    {
        const string imageUrl = "https://8.8.8.8/media/lilac-tumbler.jpg";
        var service = CreateService(new StubHandler(_ =>
        {
            var content = new ByteArrayContent([0xFF, 0xD8, 0xFF]);
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = content };
        }));

        var product = await service.TryScrapeProductFromUrl(imageUrl);

        Assert.NotNull(product);
        Assert.Equal(imageUrl, product.ImageUrl);
        Assert.Equal("Lilac tumbler", product.Name);
    }

    [Fact]
    public async Task TryScrapeProductFromUrl_IgnoresTitlesThatAreOnlyTheStoreName()
    {
        var service = CreateService(new StubHandler(_ => Html(
            "<meta property='og:site_name' content='Gifts Example'><meta property='og:title' content='Gifts Example'>" +
            "<meta property='og:price:amount' content='12'>")));

        var product = await service.TryScrapeProductFromUrl(ProductUrl);

        Assert.NotNull(product);
        Assert.Equal("Lilac stainless tumbler", product.Name);
        Assert.Equal(12m, product.Price);
    }

    [Fact]
    public async Task TryScrapeProductFromUrl_ReusesRecentSuccessfulLookups()
    {
        var handler = new StubHandler(_ => Html("<meta property='og:title' content='Desk Lamp'><meta property='og:price:amount' content='49'>"));
        using var cache = new ProductLookupCache();
        var service = CreateService(handler, cache);

        var first = await service.TryScrapeProductFromUrl(ProductUrl + "?utm_source=a");
        first!.Name = "Changed by a caller";
        var second = await service.TryScrapeProductFromUrl(ProductUrl + "?utm_source=b");

        Assert.Equal("Desk Lamp", second?.Name);
        Assert.Single(handler.Requests);
    }

    [Fact]
    public async Task TryScrapeProductFromUrl_DoesNotCacheBlockedLookups()
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.Forbidden));
        using var cache = new ProductLookupCache();
        var service = CreateService(handler, cache);

        await service.TryScrapeProductFromUrl(ProductUrl);
        await service.TryScrapeProductFromUrl(ProductUrl);

        Assert.Equal(2, handler.Requests.Count);
    }

    [Fact]
    public async Task TryScrapeProductFromUrl_UsesLinkDetailsWhenTheStoreIsTooSlow()
    {
        var service = new ProductService(
            new TestHttpClientFactory(new HttpClient(new StubHandler(async (_, cancellationToken) =>
            {
                await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
                return Html("<meta property='og:title' content='Too late'>");
            }))),
            NullLogger<ProductService>.Instance)
        {
            LookupTimeout = TimeSpan.FromMilliseconds(200)
        };

        var product = await service.TryScrapeProductFromUrl(ProductUrl);

        Assert.NotNull(product);
        Assert.True(product.FromLinkOnly);
        Assert.Equal("Lilac stainless tumbler", product.Name);
    }

    [Fact]
    public async Task TryScrapeProductFromUrl_StopsWhenTheCallerCancels()
    {
        var service = CreateService(new StubHandler(async (_, cancellationToken) =>
        {
            await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
            return Html("<title>Too late</title>");
        }));
        using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => service.TryScrapeProductFromUrl(ProductUrl, cancellation.Token));
    }

    [Fact]
    public async Task TryScrapeProductFromUrl_UsesLinkDetailsWithoutRequestWhenHostDoesNotResolve()
    {
        var handler = new StubHandler(_ => Html("<title>Unexpected</title>"));
        var service = CreateService(handler);

        var product = await service.TryScrapeProductFromUrl("https://openwish-test.invalid/products/blue-wool-scarf?ref=share");

        Assert.NotNull(product);
        Assert.True(product.FromLinkOnly);
        Assert.Equal("Blue wool scarf", product.Name);
        Assert.Equal("https://openwish-test.invalid/products/blue-wool-scarf", product.Url);
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task TryScrapeProductFromUrl_RejectsRedirectsToPrivateAddresses()
    {
        var handler = new StubHandler(request => request.RequestUri!.Host == "8.8.8.8"
            ? new HttpResponseMessage(HttpStatusCode.Redirect) { Headers = { Location = new Uri("http://10.0.0.5/admin") } }
            : Html("<title>Internal</title>"));
        var service = CreateService(handler);

        var product = await service.TryScrapeProductFromUrl(ProductUrl);

        Assert.Null(product);
        Assert.Single(handler.Requests);
    }

    private static ProductService CreateService(StubHandler handler, ProductLookupCache? cache = null) =>
        new(new TestHttpClientFactory(new HttpClient(handler)), NullLogger<ProductService>.Instance, cache);

    private static HttpResponseMessage Html(string html) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(html, Encoding.UTF8, "text/html")
    };

    private sealed class TestHttpClientFactory(HttpClient client) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => client;
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _respond;

        public StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) =>
            _respond = (request, _) => Task.FromResult(respond(request));

        public StubHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> respond) =>
            _respond = respond;

        public List<string> Requests { get; } = [];

        public List<string> UserAgents { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request.RequestUri!.AbsoluteUri);
            if (request.Headers.NonValidated.TryGetValues("User-Agent", out var userAgents))
            {
                UserAgents.AddRange(userAgents);
            }

            return _respond(request, cancellationToken);
        }
    }

    [Fact]
    public async Task TryScrapeProductFromUrl_IgnoresRedirectsToUnrelatedPages()
    {
        var handler = new StubHandler(request => request.RequestUri!.AbsolutePath.Contains("kallax")
            ? new HttpResponseMessage(HttpStatusCode.Redirect) { Headers = { Location = new Uri("https://8.8.8.8/us/en/cat/products-products/") } }
            : Html("<meta property='og:title' content='Products'><meta property='og:description' content='Browse our full range.'>" +
                   "<meta property='og:image' content='/category.jpg'>"));
        var service = CreateService(handler);

        var product = await service.TryScrapeProductFromUrl("https://8.8.8.8/us/en/p/kallax-shelf-unit-white-80275887/");

        Assert.NotNull(product);
        Assert.True(product.FromLinkOnly);
        Assert.Equal("Kallax shelf unit white", product.Name);
        Assert.Equal("https://8.8.8.8/us/en/p/kallax-shelf-unit-white-80275887/", product.Url);
        Assert.Null(product.ImageUrl);
    }

    [Fact]
    public async Task TryScrapeProductFromUrl_UsesLinkDetailsWhenThePageHasOnlyATitle()
    {
        var service = CreateService(new StubHandler(_ => Html("<title>Loading your experience</title>")));

        var product = await service.TryScrapeProductFromUrl(ProductUrl);

        Assert.NotNull(product);
        Assert.True(product.FromLinkOnly);
        Assert.Equal("Lilac stainless tumbler", product.Name);
    }

    [Fact]
    public async Task TryScrapeProductFromUrl_UsesTheTitleWhenTheLinkHasNoWords()
    {
        var service = CreateService(new StubHandler(_ => Html("<title>Hand-thrown Mug</title>")));

        var product = await service.TryScrapeProductFromUrl("https://8.8.8.8/item?id=5");

        Assert.NotNull(product);
        Assert.Equal("Hand-thrown Mug", product.Name);
    }

    [Theory]
    [InlineData("https://8.8.8.8/p/hoka-clifton-9/product/9696926", "https://8.8.8.8/womens-free-people-lasso-shorts", true)]
    [InlineData("https://8.8.8.8/p/kallax-shelf-unit", "https://8.8.8.8/", true)]
    [InlineData("https://8.8.8.8/kirkland-olive-oil.product.100334841.html", "https://8.8.8.8/p/-/kirkland-olive-oil/100334841", false)]
    [InlineData("https://8.8.8.8/product/12345", "https://8.8.8.8/product/12345/ember-mug", false)]
    [InlineData("http://8.8.8.8/products/mug", "https://8.8.8.8/products/mug/", false)]
    [InlineData("https://a.co/d/7oL8hBq", "https://www.amazon.com/Lilac-Tumbler/dp/B0CRMZHDG8", false)]
    public void IsDifferentPage_DetectsRedirectsToUnrelatedPages(string requested, string final, bool expected)
    {
        Assert.Equal(expected, ProductService.IsDifferentPage(new Uri(requested), new Uri(final)));
    }
}