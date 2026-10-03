using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using OpenWish.Application.Services;
using Xunit;

namespace OpenWish.Application.Tests.Services;

public class ProductServiceRedirectTests
{
    [Theory]
    [InlineData("images/gift.jpg", false, "https://8.8.8.8/products/images/gift.jpg")]
    [InlineData("/images/gift.jpg", false, "https://8.8.8.8/images/gift.jpg")]
    [InlineData("images/gift.jpg", true, "https://8.8.8.8/products/images/gift.jpg")]
    public async Task TryScrapeProductFromUrl_ResolvesRelativeImageAgainstRedirectDestination(
        string imagePath, bool useOpenGraph, string expectedImageUrl)
    {
        const string originalUrl = "https://1.1.1.1/old/gift";
        const string destinationUrl = "https://8.8.8.8/products/gift";
        var handler = new RedirectHandler(originalUrl, destinationUrl, imagePath, useOpenGraph);
        using var client = new HttpClient(handler);
        var service = new ProductService(new TestHttpClientFactory(client), NullLogger<ProductService>.Instance);

        var product = await service.TryScrapeProductFromUrl(originalUrl);

        Assert.NotNull(product);
        Assert.Equal("Gift", product.Name);
        if (useOpenGraph)
        {
            Assert.Equal("A thoughtful gift", product.Description);
            Assert.Equal(12m, product.Price);
        }
        Assert.Equal(expectedImageUrl, product.ImageUrl);
        Assert.Equal(originalUrl, product.Url);
        Assert.Equal(2, handler.RequestCount);
    }

    [Fact]
    public async Task TryScrapeProductFromUrl_SkipsEmptyMetadataAndUsesProductMarkup()
    {
        const string originalUrl = "https://1.1.1.1/old/gift";
        const string destinationUrl = "https://8.8.8.8/products/gift";
        var handler = new RedirectHandler(originalUrl, destinationUrl, "images/gift.jpg", false, emptyMetadata: true);
        using var client = new HttpClient(handler);
        var service = new ProductService(new TestHttpClientFactory(client), NullLogger<ProductService>.Instance);

        var product = await service.TryScrapeProductFromUrl(originalUrl);

        Assert.NotNull(product);
        Assert.Equal("Gift", product.Name);
        Assert.Equal("A thoughtful gift", product.Description);
        Assert.Equal(12m, product.Price);
        Assert.Equal("https://8.8.8.8/products/images/gift.jpg", product.ImageUrl);
        Assert.Equal(2, handler.RequestCount);
    }

    private sealed class TestHttpClientFactory(HttpClient client) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => client;
    }

    private sealed class RedirectHandler(
        string originalUrl, string destinationUrl, string imagePath, bool useOpenGraph, bool emptyMetadata = false) : HttpMessageHandler
    {
        public int RequestCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestCount++;
            if (request.RequestUri?.AbsoluteUri == originalUrl)
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Redirect)
                {
                    Headers = { Location = new Uri(destinationUrl) }
                });
            }

            Assert.Equal(destinationUrl, request.RequestUri?.AbsoluteUri);
            var imageMarkup = emptyMetadata
                ? "<meta property='og:title'>" +
                  "<meta property='og:description' content='  '>" +
                  "<meta property='product:price:amount'>" +
                  "<meta property='og:image' content='  '>" +
                  "<h1 class='product-name'>Gift</h1>" +
                  "<div class='product-description'>A thoughtful gift</div>" +
                  "<span class='price-value'>12</span>" +
                  "<img id='main-image'>" +
                  $"<img itemprop='image' src='{imagePath}'>"
                : useOpenGraph
                ? $"<meta property='og:title' content='Gift'>" +
                  "<meta property='og:description' content='A thoughtful gift'>" +
                  "<meta property='product:price:amount' content='12'>" +
                  $"<meta property='og:image' content='{imagePath}'>"
                : $"<img id='main-image' src='{imagePath}'>";
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    $"{(useOpenGraph || emptyMetadata ? "" : "<h1 class='product-name'>Gift</h1>")}{imageMarkup}")
            });
        }
    }
}