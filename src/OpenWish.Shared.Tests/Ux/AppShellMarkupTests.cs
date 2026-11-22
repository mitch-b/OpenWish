using System.Text.RegularExpressions;
using Xunit;

namespace OpenWish.Shared.Tests.Ux;

public class AppShellMarkupTests
{
    [Fact]
    public void MobileNavigation_ClosesOnEscapeAndLocksPageScroll()
    {
        var script = ReadComponent("OpenWish.Web", "wwwroot", "app.js");
        var css = ReadComponent("OpenWish.Web", "wwwroot", "app.css");
        var navCss = ReadComponent("OpenWish.Web", "Components", "Layout", "NavMenu.razor.css");

        Assert.Contains("event.key !== \"Escape\"", script, StringComparison.Ordinal);
        Assert.Contains("navigationToggle.focus();", script, StringComparison.Ordinal);
        Assert.Contains("body:has(.navbar-toggler:checked)", css, StringComparison.Ordinal);
        Assert.Matches(
            new Regex(@"\.nav-scrollable \{\s*position: fixed;[^}]*overflow-y: auto;", RegexOptions.Singleline),
            navCss);
    }

    [Fact]
    public void MobileHeader_KeepsTheNotificationBellAboveTheSidebar()
    {
        var css = ReadComponent("OpenWish.Web", "Components", "Layout", "MainLayout.razor.css");
        var mobile = css[css.IndexOf("@media (max-width: 640.98px)", StringComparison.Ordinal)..];

        Assert.Matches(new Regex(@"\.sidebar \{\s*position: sticky;", RegexOptions.Singleline), mobile);
        Assert.Matches(new Regex(@"\.top-row \{\s*position: fixed;[^}]*z-index: 1030;", RegexOptions.Singleline), mobile);
        Assert.Contains("z-index: 1020;", css, StringComparison.Ordinal);
    }

    [Fact]
    public void UtilityRow_ShowsOnlyForSignedInPeopleAndGitHubMovesToTheNavigationFooter()
    {
        var layout = ReadComponent("OpenWish.Web", "Components", "Layout", "MainLayout.razor");
        var navMenu = ReadComponent("OpenWish.Web", "Components", "Layout", "NavMenu.razor");

        Assert.Matches(
            new Regex(@"<Authorized>\s*<div class=""top-row"">\s*<NotificationFlyout />", RegexOptions.Singleline),
            layout);
        Assert.DoesNotContain("github.com", layout, StringComparison.Ordinal);
        Assert.Contains("aria-label=\"OpenWish on GitHub (opens in a new tab)\"", navMenu, StringComparison.Ordinal);
        Assert.Contains("<span>Version @AppVersionService.GetAppVersion()</span>", navMenu, StringComparison.Ordinal);
    }

    private static string ReadComponent(params string[] pathParts)
    {
        var solutionDirectory = FindSolutionDirectory();
        return File.ReadAllText(Path.Combine([solutionDirectory, .. pathParts]));
    }

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