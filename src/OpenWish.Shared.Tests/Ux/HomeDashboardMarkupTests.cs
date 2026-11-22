using OpenWish.Shared.Dashboard;
using OpenWish.Shared.Models;
using Xunit;

namespace OpenWish.Shared.Tests.Ux;

public class HomeDashboardMarkupTests
{
    private static readonly DateTimeOffset _now = new(2026, 12, 1, 15, 0, 0, TimeSpan.Zero);

    [Fact]
    public void ForDashboard_ShowsUpcomingEventsSoonestFirstAndHidesPastOnes()
    {
        var events = new[]
        {
            Event("later", _now.AddDays(40)),
            Event("past", _now.AddDays(-5)),
            Event("soon", _now.AddDays(3)),
            Event("today", _now)
        };

        var ordered = DashboardEventOrder.ForDashboard(events, _now).Select(evt => evt.Name);

        Assert.Equal(["today", "soon", "later"], ordered);
    }

    [Fact]
    public void ForDashboard_FallsBackToRecentPastEventsWhenNothingIsAhead()
    {
        var events = new[] { Event("older", _now.AddDays(-30)), Event("recent", _now.AddDays(-2)) };

        var ordered = DashboardEventOrder.ForDashboard(events, _now).Select(evt => evt.Name);

        Assert.Equal(["recent", "older"], ordered);
    }

    [Fact]
    public void Dashboard_PutsNeedsAttentionBeforeToolsCollectionsAndShortcuts()
    {
        var markup = ReadHome();

        var attention = markup.IndexOf("class=\"attention-panel\"", StringComparison.Ordinal);
        var tools = markup.IndexOf("class=\"dashboard-tools", StringComparison.Ordinal);
        var events = markup.IndexOf("class=\"collection-section collection-events\"", StringComparison.Ordinal);
        var wishlists = markup.IndexOf("class=\"collection-section collection-wishlists\"", StringComparison.Ordinal);
        var shortcuts = markup.IndexOf("class=\"action-panel\"", StringComparison.Ordinal);

        Assert.True(attention >= 0 && attention < tools, "Needs attention should come first.");
        Assert.True(tools < events && events < wishlists && wishlists < shortcuts, "Shortcuts should close the page.");
        Assert.DoesNotContain("Pick your next step", markup, StringComparison.Ordinal);
        Assert.DoesNotContain("<strong>Upcoming events</strong>", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Dashboard_GuardsChecklistDismissUntilInteractiveAndAvoidsDuplicateFirstRunCalls()
    {
        var markup = ReadHome();

        Assert.Contains("disabled=\"@(_isDismissingChecklist || !RendererInfo.IsInteractive)\"", markup, StringComparison.Ordinal);
        Assert.Contains("@(_isDismissingChecklist ? \"Dismissing...\" : \"Dismiss\")", markup, StringComparison.Ordinal);
        Assert.Contains("@if (!IsChecklistVisible)", markup, StringComparison.Ordinal);
        Assert.Contains("You're all caught up.", markup, StringComparison.Ordinal);
        Assert.DoesNotContain("btn-outline-primary", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void DashboardStyles_StackShortcutsAndQuickAddOnPhones()
    {
        var styles = File.ReadAllText(Path.Combine(FindSolutionDirectory(), "OpenWish.Web", "Components", "Pages", "Home.razor.css"));

        Assert.Contains(".action-tile ::deep .action-arrow", styles, StringComparison.Ordinal);
        Assert.Contains("grid-template-columns: repeat(auto-fill, minmax(min(100%, 17rem), 1fr));", styles, StringComparison.Ordinal);
        Assert.Contains(".dashboard-tools-split", styles, StringComparison.Ordinal);
    }

    private static EventModel Event(string name, DateTimeOffset date) => new() { Name = name, Date = date };

    private static string ReadHome() =>
        File.ReadAllText(Path.Combine(FindSolutionDirectory(), "OpenWish.Web", "Components", "Pages", "Home.razor"));

    private static string FindSolutionDirectory()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "OpenWish.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName
            ?? throw new DirectoryNotFoundException("Could not locate OpenWish.slnx.");
    }
}