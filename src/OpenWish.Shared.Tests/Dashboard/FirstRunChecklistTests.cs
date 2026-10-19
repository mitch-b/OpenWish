using OpenWish.Shared.Dashboard;
using Xunit;

namespace OpenWish.Shared.Tests.Dashboard;

public class FirstRunChecklistTests
{
    [Fact]
    public void IsVisible_WhenSomeStepsRemain()
    {
        var checklist = new FirstRunChecklist(true, false, false, false, false);

        Assert.True(checklist.IsVisible);
        Assert.Equal(1, checklist.CompletedCount);
        Assert.Equal(["wishlist", "idea", "friend", "event"], checklist.Steps.Select(s => s.Key).ToArray());
    }

    [Fact]
    public void IsHidden_WhenEveryStepIsDone()
    {
        var checklist = new FirstRunChecklist(true, true, true, true, false);

        Assert.True(checklist.IsComplete);
        Assert.False(checklist.IsVisible);
    }

    [Fact]
    public void IsHidden_WhenDismissed()
    {
        Assert.False(new FirstRunChecklist(false, false, false, false, true).IsVisible);
        Assert.False(FirstRunChecklist.Hidden.IsVisible);
    }

    [Fact]
    public void Steps_LinkToExistingPages()
    {
        var hrefs = new FirstRunChecklist(false, false, false, false, false).Steps.Select(s => s.Href);

        Assert.All(hrefs, href => Assert.StartsWith("/", href));
    }
}