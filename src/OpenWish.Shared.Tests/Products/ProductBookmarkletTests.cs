using OpenWish.Shared.Products;
using Xunit;

namespace OpenWish.Shared.Tests.Products;

public class ProductBookmarkletTests
{
    [Fact]
    public void Create_PointsAtTheAddPage()
    {
        var bookmarklet = ProductBookmarklet.Create(new Uri("https://wishes.example/"));

        Assert.StartsWith("javascript:(()=>{", bookmarklet);
        Assert.Contains("'https://wishes.example/'+'add?'", bookmarklet);
        Assert.EndsWith("})()", bookmarklet);
    }

    [Fact]
    public void Create_KeepsAnAppPathBase()
    {
        var bookmarklet = ProductBookmarklet.Create(new Uri("https://example.com/openwish/?ignored=1"));

        Assert.Contains("'https://example.com/openwish/'+'add?'", bookmarklet);
        Assert.DoesNotContain("ignored", bookmarklet);
    }

    [Fact]
    public void Create_ProducesASingleLineThatSurvivesPercentDecoding()
    {
        var bookmarklet = ProductBookmarklet.Create(new Uri("http://localhost:5000/"));

        Assert.DoesNotContain('\n', bookmarklet);
        Assert.DoesNotContain('\r', bookmarklet);
        Assert.DoesNotContain('%', bookmarklet);
        Assert.DoesNotContain('#', bookmarklet);
        Assert.DoesNotContain("//", bookmarklet.Replace("http://", string.Empty, StringComparison.Ordinal));
    }

    [Fact]
    public void Create_EscapesTheAppAddress()
    {
        var bookmarklet = ProductBookmarklet.Create(new Uri("https://example.com/it's/"));

        Assert.DoesNotContain("it's", bookmarklet);
    }

    [Theory]
    [InlineData("ftp://example.com/")]
    [InlineData("/relative")]
    public void Create_RequiresAWebAddress(string address)
    {
        Assert.Throws<ArgumentException>(() => ProductBookmarklet.Create(new Uri(address, UriKind.RelativeOrAbsolute)));
    }

    [Fact]
    public void Create_SendsTheFieldsTheAddPageReads()
    {
        var bookmarklet = ProductBookmarklet.Create(new Uri("https://wishes.example/"));

        foreach (var field in new[] { "url:", "title:", "description:", "price:", "image:" })
        {
            Assert.Contains(field, bookmarklet);
        }
    }
}