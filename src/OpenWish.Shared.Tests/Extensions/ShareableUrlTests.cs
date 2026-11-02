using OpenWish.Shared.Extensions;
using Xunit;

namespace OpenWish.Shared.Tests.Extensions;

public class ShareableUrlTests
{
    [Theory]
    [InlineData("https://gifts.example/wishlists/abc", "https://gifts.example/wishlists/abc")]
    [InlineData("https://gifts.example/wishlists/abc?budget=25", "https://gifts.example/wishlists/abc")]
    [InlineData("https://gifts.example/wishlists/abc#item-4", "https://gifts.example/wishlists/abc")]
    [InlineData("https://gifts.example/wishlists/abc?budget=25#item-4", "https://gifts.example/wishlists/abc")]
    public void FromCurrentUri_RemovesQueryAndFragment(string current, string expected)
    {
        Assert.Equal(expected, ShareableUrl.FromCurrentUri(current));
    }

    [Fact]
    public void FromCurrentUri_RejectsNull()
    {
        Assert.Throws<ArgumentNullException>(() => ShareableUrl.FromCurrentUri(null!));
    }
}