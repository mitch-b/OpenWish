using System.Text.RegularExpressions;
using Xunit;

namespace OpenWish.Shared.Tests.Ux;

public class GiftPlanMarkupTests
{
    [Fact]
    public void GiftPlan_ClickHandlersWaitForHydration()
    {
        var plan = Read("Pages", "GiftPlan", "Index.razor");

        foreach (Match button in Regex.Matches(plan, "<button[^>]*@onclick[^>]*>", RegexOptions.Singleline))
        {
            Assert.Contains("RendererInfo.IsInteractive", button.Value, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void GiftPlan_ShowsEachGiftsStatusAndOnlyMarksTheClickedActionBusy()
    {
        var plan = Read("Pages", "GiftPlan", "Index.razor");

        Assert.Contains("data-testid=\"gift-plan-status-to-buy\"", plan, StringComparison.Ordinal);
        Assert.Contains("isBusy && _busyAction == BusyWrapped ? \"Saving...\"", plan, StringComparison.Ordinal);
        Assert.Contains("isBusy && _busyAction == BusyPurchased ? \"Saving...\"", plan, StringComparison.Ordinal);
        Assert.Contains("data-testid=\"gift-plan-recipient-budget\"", plan, StringComparison.Ordinal);
    }

    [Fact]
    public void GiftPlan_EmptyStateOffersNextStepsAndThePrivacyPromise()
    {
        var plan = Read("Pages", "GiftPlan", "Index.razor");

        Assert.Contains("never sees your reservations", plan, StringComparison.Ordinal);
        Assert.Contains("href=\"/wishlists\" class=\"btn btn-primary\"", plan, StringComparison.Ordinal);
        Assert.Contains("href=\"/events\"", plan, StringComparison.Ordinal);
    }

    [Fact]
    public void GiftPlan_PhoneLayoutStacksRowsAndStatusUsesThemeTokens()
    {
        var styles = Read("Pages", "GiftPlan", "Index.razor.css");

        Assert.DoesNotContain(".plan-summary dd.plan-item-main", styles, StringComparison.Ordinal);
        Assert.Contains("flex-direction: column;", styles, StringComparison.Ordinal);
        Assert.Contains("var(--color-success-text)", styles, StringComparison.Ordinal);
        Assert.Contains("var(--color-warning-text)", styles, StringComparison.Ordinal);
    }

    [Fact]
    public void EventReservedItems_WrapsOnPhonesAndPointsToTheGiftPlan()
    {
        var markup = Read("Event", "EventReservedItems.razor");
        var styles = Read("Event", "EventReservedItems.razor.css");

        Assert.DoesNotContain("text-truncate", markup, StringComparison.Ordinal);
        Assert.Contains("href=\"/gift-plan\"", markup, StringComparison.Ordinal);
        Assert.Contains("disabled=\"@(_isLoading || !RendererInfo.IsInteractive)\"", markup, StringComparison.Ordinal);
        Assert.Contains("overflow-wrap: anywhere;", styles, StringComparison.Ordinal);
        Assert.Contains("-webkit-line-clamp: 2;", styles, StringComparison.Ordinal);
    }

    private static string Read(params string[] parts)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "OpenWish.slnx")))
        {
            directory = directory.Parent;
        }

        var root = directory?.FullName ?? throw new DirectoryNotFoundException("Could not locate OpenWish.slnx.");
        return File.ReadAllText(Path.Combine([root, "OpenWish.Web.Client", "Components", .. parts]));
    }
}