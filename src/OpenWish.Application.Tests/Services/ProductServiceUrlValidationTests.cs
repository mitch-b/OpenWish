using Microsoft.Extensions.Logging.Abstractions;
using OpenWish.Application.Services;
using Xunit;

namespace OpenWish.Application.Tests.Services;

public class ProductServiceUrlValidationTests
{
    [Theory]
    [InlineData("https://8.8.8.8", true)]
    [InlineData("http://1.1.1.1", true)]
    [InlineData("ftp://8.8.8.8", false)]
    [InlineData("file:///etc/passwd", false)]
    public async Task IsSafeUrlAsync_ValidatesScheme(string url, bool expected)
    {
        var uri = new Uri(url);
        var result = await ProductService.IsSafeUrlAsync(uri);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("https://localhost", false)]
    [InlineData("https://127.0.0.1", false)]
    [InlineData("https://192.168.1.1", false)]
    [InlineData("https://10.0.0.1", false)]
    [InlineData("https://172.16.0.1", false)]
    [InlineData("https://169.254.0.1", false)]
    [InlineData("https://0.0.0.0", false)]
    public async Task IsSafeUrlAsync_RejectsPrivateAddresses(string url, bool expected)
    {
        var uri = new Uri(url);
        var result = await ProductService.IsSafeUrlAsync(uri);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("127.0.0.1", false)]
    [InlineData("192.168.1.1", false)]
    [InlineData("10.0.0.1", false)]
    [InlineData("172.16.0.1", false)]
    [InlineData("172.20.0.1", false)]
    [InlineData("172.31.255.255", false)]
    [InlineData("169.254.1.1", false)]
    [InlineData("0.0.0.0", false)]
    [InlineData("255.255.255.255", false)]
    [InlineData("224.0.0.1", false)]
    [InlineData("255.0.0.0", false)]
    public void IsSafeAddress_RejectsReservedIPv4Ranges(string ip, bool expected)
    {
        var address = System.Net.IPAddress.Parse(ip);
        var result = ProductService.IsSafeAddress(address);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("::1", false)]
    [InlineData("::ffff:192.0.2.1", false)]
    [InlineData("2001:db8::1", false)]
    [InlineData("fc00::1", false)]
    [InlineData("fe80::1", false)]
    [InlineData("ff02::1", false)]
    public void IsSafeAddress_RejectsReservedIPv6Ranges(string ip, bool expected)
    {
        var address = System.Net.IPAddress.Parse(ip);
        var result = ProductService.IsSafeAddress(address);

        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task TryScrapeProductFromUrl_RejectsMalformedUrl()
    {
        var mockFactory = new MockHttpClientFactory();
        var service = new ProductService(mockFactory, NullLogger<ProductService>.Instance);

        var result = await service.TryScrapeProductFromUrl("ht!tp://[invalid:url");

        Assert.Null(result);
    }

    [Fact]
    public async Task TryScrapeProductFromUrl_RejectsPrivateAddressWithoutRequest()
    {
        var recordingHandler = new RecordingHttpMessageHandler();
        var mockFactory = new RecordingHttpClientFactory(recordingHandler);
        var service = new ProductService(mockFactory, NullLogger<ProductService>.Instance);

        var result = await service.TryScrapeProductFromUrl("https://192.168.1.1");

        Assert.Null(result);
        Assert.Empty(recordingHandler.Requests);
    }

    [Fact]
    public async Task TryScrapeProductFromUrl_RejectsInvalidUrl()
    {
        var mockFactory = new MockHttpClientFactory();
        var service = new ProductService(mockFactory, NullLogger<ProductService>.Instance);

        var result = await service.TryScrapeProductFromUrl("not a valid url");

        Assert.Null(result);
    }

    [Fact]
    public async Task TryScrapeProductFromUrl_RejectsEmptyUrl()
    {
        var mockFactory = new MockHttpClientFactory();
        var service = new ProductService(mockFactory, NullLogger<ProductService>.Instance);

        var result = await service.TryScrapeProductFromUrl("");

        Assert.Null(result);
    }

    [Fact]
    public async Task TryScrapeProductFromUrl_RejectsWhitespaceUrl()
    {
        var mockFactory = new MockHttpClientFactory();
        var service = new ProductService(mockFactory, NullLogger<ProductService>.Instance);

        var result = await service.TryScrapeProductFromUrl("   ");

        Assert.Null(result);
    }

    [Fact]
    public async Task TryScrapeProductFromUrl_RejectsNullUrl()
    {
        var mockFactory = new MockHttpClientFactory();
        var service = new ProductService(mockFactory, NullLogger<ProductService>.Instance);

        var result = await service.TryScrapeProductFromUrl(null!);

        Assert.Null(result);
    }

    private class MockHttpClientFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name)
        {
            var handler = new HttpClientHandler();
            return new HttpClient(handler)
            {
                BaseAddress = new Uri("https://example.com")
            };
        }
    }

    private class RecordingHttpMessageHandler : HttpMessageHandler
    {
        public List<HttpRequestMessage> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Requests.Add(request);
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK));
        }
    }

    private class RecordingHttpClientFactory : IHttpClientFactory
    {
        private readonly RecordingHttpMessageHandler _handler;

        public RecordingHttpClientFactory(RecordingHttpMessageHandler handler)
        {
            _handler = handler;
        }

        public HttpClient CreateClient(string name)
        {
            return new HttpClient(_handler)
            {
                BaseAddress = new Uri("https://example.com")
            };
        }
    }
}