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
    public void NormalizeName_TrimsAndCollapsesWhitespace()
    {
        Assert.Equal("Book club", CircleRules.NormalizeName("  Book   club "));
    }
}