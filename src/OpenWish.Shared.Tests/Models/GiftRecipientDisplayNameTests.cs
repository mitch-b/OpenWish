using OpenWish.Shared.Models;
using Xunit;

namespace OpenWish.Shared.Tests.Models;

public class GiftRecipientDisplayNameTests
{
    [Theory]
    [InlineData("Alex", null, "Alex")]
    [InlineData("alex.person@example.test", null, "alex person")]
    [InlineData("alex@example.test", "Alex from the cabin", "Alex from the cabin")]
    [InlineData("Alex account", "Alex from the cabin", "Alex from the cabin")]
    [InlineData(null, "Robin", "Robin")]
    [InlineData(null, null, "Invited guest")]
    public void GiftRecipientName_PreservesGroupNamesWithoutReturningAnEmail(string? userName, string? groupName, string expected)
    {
        var name = PublicDisplayName.ForGiftRecipient(userName, groupName);

        Assert.Equal(expected, name);
        Assert.DoesNotContain("@", name);
    }
}