using System.ComponentModel.DataAnnotations;
using OpenWish.Shared.Models;
using Xunit;

namespace OpenWish.Shared.Tests.Models;

public class EventModelValidationTests
{
    private static List<ValidationResult> Validate(EventModel model)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true);
        return results;
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void BlankName_IsRejectedWithAFriendlyMessage(string? name)
    {
        var results = Validate(new EventModel { Name = name!, EventUsers = [] });

        Assert.Contains(results, r => r.ErrorMessage == "Give your event a name.");
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(1000000000)]
    public void OutOfRangeBudget_IsRejected(int budget)
    {
        var results = Validate(new EventModel { Name = "Cabin swap", Budget = budget, EventUsers = [] });

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(EventModel.Budget)));
    }

    [Theory]
    [InlineData(null)]
    [InlineData(0)]
    [InlineData(50)]
    public void ValidEvent_PassesWithOrWithoutBudget(int? budget)
    {
        var results = Validate(new EventModel { Name = "Cabin swap", Budget = budget, EventUsers = [] });

        Assert.Empty(results);
    }
}