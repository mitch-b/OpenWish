using OpenWish.Shared.Models;
using Xunit;

namespace OpenWish.Shared.Tests.Models;

public class CircleRulesTests
{
    private static readonly CircleModel _family = new()
    {
        PublicId = "fam",
        Name = "Family",
        Members = [new CircleMemberModel { UserId = "1", UserName = "JordanDemo" }]
    };

    [Fact]
    public void GetMemberNames_ReturnsNullWithoutAKnownCircle()
    {
        Assert.Null(CircleRules.GetMemberNames([_family], null));
        Assert.Null(CircleRules.GetMemberNames([_family], "gone"));
    }

    [Fact]
    public void GetMemberNames_IsCaseInsensitive()
    {
        var names = CircleRules.GetMemberNames([_family], "fam");

        Assert.NotNull(names);
        Assert.Contains("jordandemo", names);
    }

    [Fact]
    public void Apply_RestrictsListsToCircleMembers()
    {
        var lists = new[]
        {
            new WishlistModel { Name = "Birthday", Owner = new ApplicationUserModel { UserName = "JordanDemo" } },
            new WishlistModel { Name = "Books", Owner = new ApplicationUserModel { UserName = "CaseyDemo" } },
        };

        var visible = FriendWishlistFilter.Apply(lists, null, null, CircleRules.GetMemberNames([_family], "fam"));

        Assert.Equal(["Birthday"], visible.Select(l => l.Name));
    }

    [Fact]
    public void GetSharingCircleNames_ListsOnlyCirclesThatShareTheListAndIncludeThePerson()
    {
        var shared = new CircleModel
        {
            PublicId = "book",
            Name = "Book club",
            Members = [new CircleMemberModel { UserId = "1", UserName = "JordanDemo" }],
            Wishlists = [new CircleWishlistModel { PublicId = "list-1", Name = "Birthday" }]
        };
        var otherList = _family with { Wishlists = [new CircleWishlistModel { PublicId = "list-2", Name = "Books" }] };

        var names = CircleRules.GetSharingCircleNames([otherList, shared], "list-1", "1");

        Assert.Equal(["Book club"], names);
        Assert.Empty(CircleRules.GetSharingCircleNames([shared], "list-1", "2"));
        Assert.Empty(CircleRules.GetSharingCircleNames([shared], "list-1", null));
    }

    [Theory]
    [InlineData(new string[0], "")]
    [InlineData(new[] { "Family" }, "Family")]
    [InlineData(new[] { "Family", "Book club" }, "Family and Book club")]
    [InlineData(new[] { "Family", "Book club", "Work" }, "Family, Book club and Work")]
    public void JoinNames_ReadsAsASentence(string[] names, string expected)
    {
        Assert.Equal(expected, CircleRules.JoinNames(names));
    }

    [Fact]
    public void NormalizeName_TrimsAndCollapsesWhitespace()
    {
        Assert.Equal("Book club", CircleRules.NormalizeName("  Book   club "));
    }
}