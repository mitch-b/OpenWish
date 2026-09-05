using OpenWish.Application.Services;
using OpenWish.Data.Entities;
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
}