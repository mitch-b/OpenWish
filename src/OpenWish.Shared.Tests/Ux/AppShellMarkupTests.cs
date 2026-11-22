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

    [Fact]
    public void InteractiveComponents_UseOneMeasuredServerPolicy()
    {
        var solutionDirectory = FindSolutionDirectory();
        var components = Directory
            .EnumerateFiles(Path.Combine(solutionDirectory, "OpenWish.Web"), "*.razor", SearchOption.AllDirectories)
            .Concat(Directory.EnumerateFiles(Path.Combine(solutionDirectory, "OpenWish.Web.Client"), "*.razor", SearchOption.AllDirectories))
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                && !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal));
        var otherModes = components
            .Where(path => Regex.IsMatch(File.ReadAllText(path), @"\b(InteractiveAuto|InteractiveWebAssembly)\b"))
            .Select(path => Path.GetRelativePath(solutionDirectory, path))
            .ToList();
        var serverComponents = Directory
            .EnumerateFiles(Path.Combine(solutionDirectory, "OpenWish.Web"), "*.razor", SearchOption.AllDirectories)
            .Where(path => File.ReadAllText(path).Contains("@rendermode", StringComparison.Ordinal))
            .Select(path => Path.GetRelativePath(solutionDirectory, path))
            .ToList();

        // InteractiveServer benchmarked fastest; mixing Server and WebAssembly roots is unsupported.
        Assert.Empty(otherModes);
        Assert.Empty(serverComponents);
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