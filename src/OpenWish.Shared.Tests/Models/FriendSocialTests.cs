using OpenWish.Shared.Models;
using Xunit;

namespace OpenWish.Shared.Tests.Models;

public class FriendSocialTests
{
    [Fact]
    public void Parse_SplitsOnSeparatorsAndKeepsEachAddressOnce()
    {
        var input = FriendInviteInput.Parse("a@example.com, B@example.com;\nb@EXAMPLE.com  c@example.org");

        Assert.Equal(["a@example.com", "B@example.com", "c@example.org"], input.Valid);
        Assert.Empty(input.Invalid);
        Assert.Equal(1, input.Duplicates);
    }

    [Fact]
    public void Parse_ReportsInvalidAddressesOnce()
    {
        var input = FriendInviteInput.Parse("good@example.com not-an-email not-an-email x@y Name <a@b.com>");

        Assert.Equal(["good@example.com"], input.Valid);
        Assert.Contains("not-an-email", input.Invalid);
        Assert.Contains("x@y", input.Invalid);
        Assert.Equal(1, input.Invalid.Count(token => token == "not-an-email"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("  ")]
    public void Parse_ReturnsNothingForBlankText(string? text)
    {
        var input = FriendInviteInput.Parse(text);

        Assert.Empty(input.Valid);
        Assert.Empty(input.Invalid);
    }

    [Fact]
    public void Apply_FiltersByOwnerAndSearchWithoutCaseSensitivity()
    {
        var lists = new[]
        {
            List("Birthday", "JordanDemo"),
            List("Books", "CaseyDemo"),
            List("Jordan's Favorites", "CaseyDemo"),
        };

        Assert.Equal(["Birthday"], FriendWishlistFilter.Apply(lists, null, "jordandemo").Select(l => l.Name));
        Assert.Equal(["Books"], FriendWishlistFilter.Apply(lists, "boo", "CaseyDemo").Select(l => l.Name));
        Assert.Equal(["Birthday", "Jordan's Favorites"], FriendWishlistFilter.Apply(lists, "jordan", null).Select(l => l.Name));
        Assert.Equal(3, FriendWishlistFilter.Apply(lists, " ", " ").Count);
    }

    [Fact]
    public void OrderFriends_SortsByNameIgnoringCase()
    {
        var friends = new[] { User("taylor"), User("Alex"), User("Casey") };

        Assert.Equal(["Alex", "Casey", "taylor"], FriendWishlistFilter.OrderFriends(friends).Select(f => f.UserName));
    }

    private static WishlistModel List(string name, string owner) => new() { Name = name, Owner = User(owner) };

    private static ApplicationUserModel User(string name) => new() { UserName = name };
}