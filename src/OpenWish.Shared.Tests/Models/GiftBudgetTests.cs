using OpenWish.Shared.Models;
using Xunit;

namespace OpenWish.Shared.Tests.Models;

public class GiftBudgetTests
{
    [Theory]
    [InlineData("75", 75)]
    [InlineData("75.50", 75.50)]
    [InlineData("0", 0)]
    [InlineData("10000000", 10000000)]
    public void Parse_AcceptsInvariantNonNegativeAmountsWithinTheLimit(string input, decimal expected)
    {
        Assert.Equal(expected, GiftBudget.Parse(input));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-a-number")]
    [InlineData("75,50")]
    [InlineData("-1")]
    [InlineData("10000000.01")]
    public void Parse_RejectsMalformedOrOutOfRangeAmounts(string? input)
    {
        Assert.Null(GiftBudget.Parse(input));
    }
}