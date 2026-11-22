using Xunit;

namespace OpenWish.Shared.Tests.Ux;

public class WhatsNewInstallMarkupTests
{
    [Fact]
    public void ReleasePagination_UsesHydrationGuardedButtonsAndClearsTheStickyHeader()
    {
        var markup = Read("OpenWish.Web.Client", "Components", "Pages", "WhatsNew.razor");
        var styles = Read("OpenWish.Web.Client", "Components", "Pages", "WhatsNew.razor.css");

        Assert.DoesNotContain("href=\"/whats-new#release-timeline\"", markup, StringComparison.Ordinal);
        Assert.Contains(
            "disabled=\"@(_currentPage == 1 || !RendererInfo.IsInteractive)\"",
            markup,
            StringComparison.Ordinal);
        Assert.Contains(
            "disabled=\"@(_currentPage == TotalPages || !RendererInfo.IsInteractive)\"",
            markup,
            StringComparison.Ordinal);
        Assert.Contains("<span aria-live=\"polite\">Page @_currentPage of @TotalPages</span>", markup, StringComparison.Ordinal);
        Assert.Contains("catch (JSDisconnectedException)", markup, StringComparison.Ordinal);

        var timeline = styles.IndexOf(".release-timeline {", StringComparison.Ordinal);
        Assert.True(timeline >= 0);
        Assert.Contains("scroll-margin-top", styles[timeline..styles.IndexOf('}', timeline)], StringComparison.Ordinal);
    }

    [Fact]
    public void ReleaseCards_ShowACompactDateBesideTheVersionOnPhones()
    {
        var markup = Read("OpenWish.Web.Client", "Components", "Pages", "WhatsNew.razor");
        var styles = Read("OpenWish.Web.Client", "Components", "Pages", "WhatsNew.razor.css");

        var versionRow = markup.IndexOf("<div class=\"release-version-row\">", StringComparison.Ordinal);
        var metaDate = markup.IndexOf("class=\"release-meta-date\"", StringComparison.Ordinal);
        var title = markup.IndexOf("<h2>@release.Title</h2>", StringComparison.Ordinal);
        Assert.True(versionRow >= 0 && versionRow < metaDate && metaDate < title);
        Assert.Contains("@FormatShortDate(release.Date)", markup, StringComparison.Ordinal);
        Assert.Contains("placeholder=\"Version, feature, or fix\"", markup, StringComparison.Ordinal);
        Assert.Contains("class=\"container page-shell\"", markup.Replace(" whats-new-page", string.Empty), StringComparison.Ordinal);

        var mobile = styles.IndexOf("@media (max-width: 767.98px)", StringComparison.Ordinal);
        Assert.True(mobile >= 0);
        Assert.Contains(".release-meta-date {\n        display: inline;", styles[mobile..].ReplaceLineEndings("\n"), StringComparison.Ordinal);
    }

    [Fact]
    public void ReleaseFilterActions_WaitForHydration()
    {
        var markup = Read("OpenWish.Web.Client", "Components", "Pages", "WhatsNew.razor");

        foreach (var label in new[] { "Clear filters", "Show all releases" })
        {
            var labelIndex = markup.IndexOf(label + "\n", StringComparison.Ordinal);
            Assert.True(labelIndex >= 0, label);
            var button = markup.LastIndexOf("<button", labelIndex, StringComparison.Ordinal);
            Assert.Contains("disabled=\"@(!RendererInfo.IsInteractive)\"", markup[button..labelIndex], StringComparison.Ordinal);
        }
    }

    [Fact]
    public void AccountInstallPage_ShowsNumberedStepsWithoutARedundantDisclosure()
    {
        var markup = Read("OpenWish.Web", "Components", "Shared", "AppInstall.razor");
        var styles = Read("OpenWish.Web", "Components", "Shared", "AppInstall.razor.css");

        Assert.DoesNotContain("<summary>Install as app</summary>", markup, StringComparison.Ordinal);
        Assert.Contains("<summary>How to install</summary>", markup, StringComparison.Ordinal);
        Assert.Contains("<div class=\"app-install-steps\" data-install-manual>", markup, StringComparison.Ordinal);
        Assert.Contains("<div data-install-browser>", markup, StringComparison.Ordinal);
        Assert.Contains("<li>Open your browser's menu.</li>", markup, StringComparison.Ordinal);
        Assert.Contains("class=\"app-install-benefits\"", markup, StringComparison.Ordinal);
        Assert.Contains("counter-increment: install-step;", styles, StringComparison.Ordinal);
        Assert.Contains(".app-install [hidden] {", styles, StringComparison.Ordinal);
    }

    [Fact]
    public void InstallBanner_KeepsItsDismissActionAfterTheSteps()
    {
        var markup = Read("OpenWish.Web", "Components", "Shared", "AppInstall.razor");

        var steps = markup.IndexOf("<details class=\"app-install-steps\" data-install-manual>", StringComparison.Ordinal);
        var dismiss = markup.IndexOf("data-install-dismiss>Not now</button>", StringComparison.Ordinal);
        Assert.True(steps >= 0 && steps < dismiss);
        Assert.Contains("class=\"app-install-installed\" data-install-installed role=\"status\" hidden", markup, StringComparison.Ordinal);
    }

    private static string Read(params string[] segments)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "OpenWish.slnx")))
        {
            directory = directory.Parent;
        }

        var root = directory?.FullName
            ?? throw new DirectoryNotFoundException("Could not locate OpenWish.slnx.");
        return File.ReadAllText(Path.Combine([root, .. segments])).ReplaceLineEndings("\n");
    }
}