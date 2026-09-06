using OpenWish.Application.Services;
using Xunit;

namespace OpenWish.Application.Tests.Services;

public class FriendServiceSecurityTests
{
    [Theory]
    [InlineData("person@example.com", "PERSON@EXAMPLE.COM", true)]
    [InlineData("a_b@example.com", "axb@example.com", false)]
    [InlineData("percent%@example.com", "percent-value@example.com", false)]
    public void NormalizeEmailForComparison_PreservesExactEmailIdentity(
        string inviteEmail,
        string userEmail,
        bool expectedMatch)
    {
        var normalizedInviteEmail = FriendService.NormalizeEmailForComparison(inviteEmail);
        var normalizedUserEmail = FriendService.NormalizeEmailForComparison(userEmail);

        Assert.Equal(expectedMatch, normalizedInviteEmail == normalizedUserEmail);
    }
}