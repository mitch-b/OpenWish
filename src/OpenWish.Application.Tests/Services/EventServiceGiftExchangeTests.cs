using OpenWish.Application.Services;
using OpenWish.Data.Entities;
using OpenWish.Shared.Models;
using Xunit;

namespace OpenWish.Application.Tests.Services;

public class EventServiceGiftExchangeTests
{
    [Theory]
    [InlineData("Pending", false, false)]
    [InlineData("Rejected", false, false)]
    [InlineData("Accepted", false, false)]
    [InlineData("Accepted", true, true)]
    public void IsEligibleGiftExchangeParticipant_RequiresAcceptedState(
        string status,
        bool isAccepted,
        bool expected)
    {
        var participant = new EventUser
        {
            Event = null!,
            Status = status,
            IsAccepted = isAccepted
        };

        var result = EventService.IsEligibleGiftExchangeParticipant(participant);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void IsEligibleGiftExchangeParticipant_ExcludesDeletedParticipant()
    {
        var participant = new EventUser
        {
            Event = null!,
            Status = "Accepted",
            IsAccepted = true,
            Deleted = true
        };

        var result = EventService.IsEligibleGiftExchangeParticipant(participant);

        Assert.False(result);
    }

    [Fact]
    public void PairingRulesMatch_RecognizesAnEquivalentRetry()
    {
        var existingRule = new CustomPairingRule
        {
            SourceUserId = "source",
            TargetInviteeEmail = "guest@example.com",
            RuleType = "Exclusion",
            RuleDescription = "Cannot be matched together",
            Event = null!
        };
        var requestedRule = new CustomPairingRuleModel
        {
            SourceUserId = "source",
            TargetInviteeEmail = "GUEST@example.com",
            RuleType = "exclusion"
        };

        Assert.True(EventService.PairingRulesMatch(existingRule, requestedRule));
    }

    [Fact]
    public void PairingRulesMatch_PreservesRuleDirection()
    {
        var existingRule = new CustomPairingRule
        {
            SourceUserId = "source",
            TargetUserId = "target",
            RuleType = "Exclusion",
            RuleDescription = "Cannot be matched together",
            Event = null!
        };
        var requestedRule = new CustomPairingRuleModel
        {
            SourceUserId = "target",
            TargetUserId = "source",
            RuleType = "Exclusion"
        };

        Assert.False(EventService.PairingRulesMatch(existingRule, requestedRule));
    }
}