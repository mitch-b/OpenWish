using System.Net;
using OpenWish.Application.Services;
using Xunit;

namespace OpenWish.Application.Tests.Services;

public class ProductServiceSecurityTests
{
    [Theory]
    [InlineData("8.8.8.8")]
    [InlineData("1.1.1.1")]
    [InlineData("2606:4700:4700::1111")]
    public void IsSafeAddress_WithPublicAddress_ReturnsTrue(string value)
    {
        var result = ProductService.IsSafeAddress(IPAddress.Parse(value));

        Assert.True(result);
    }

    [Theory]
    [InlineData("0.0.0.0")]
    [InlineData("10.1.2.3")]
    [InlineData("100.64.0.1")]
    [InlineData("127.0.0.1")]
    [InlineData("169.254.169.254")]
    [InlineData("172.16.0.1")]
    [InlineData("192.0.0.1")]
    [InlineData("192.0.2.1")]
    [InlineData("192.168.1.1")]
    [InlineData("192.88.99.1")]
    [InlineData("198.18.0.1")]
    [InlineData("198.51.100.1")]
    [InlineData("203.0.113.1")]
    [InlineData("224.0.0.1")]
    [InlineData("240.0.0.1")]
    [InlineData("::")]
    [InlineData("::1")]
    [InlineData("64:ff9b::808:808")]
    [InlineData("100::1")]
    [InlineData("2001:db8::1")]
    [InlineData("2002:808:808::1")]
    [InlineData("3fff::1")]
    [InlineData("fc00::1")]
    [InlineData("fe80::1")]
    [InlineData("fec0::1")]
    [InlineData("ff02::1")]
    [InlineData("::ffff:127.0.0.1")]
    public void IsSafeAddress_WithNonPublicAddress_ReturnsFalse(string value)
    {
        var result = ProductService.IsSafeAddress(IPAddress.Parse(value));

        Assert.False(result);
    }
}