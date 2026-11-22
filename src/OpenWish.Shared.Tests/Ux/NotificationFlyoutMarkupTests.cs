using System.Text.RegularExpressions;
using Xunit;

namespace OpenWish.Shared.Tests.Ux;

public class NotificationFlyoutMarkupTests
{
    private static readonly string[] _flyoutPath = ["OpenWish.Web", "Components", "Shared", "NotificationFlyout.razor"];
    private static readonly string[] _stylesPath = ["OpenWish.Web", "Components", "Shared", "NotificationFlyout.razor.css"];

    [Fact]
    public void Bell_WaitsForInteractivityAndKeepsTheBadgeOutOfTheAccessibleName()
    {
        var markup = ReadComponent(_flyoutPath);

        Assert.Contains("disabled=\"@(!RendererInfo.IsInteractive)\"", markup, StringComparison.Ordinal);
        Assert.Contains("notification-badge\" aria-hidden=\"true\"", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Flyout_ClosesFromAnOutsideTapThatStaysInsideTheModalDialog()
    {
        var markup = ReadComponent(_flyoutPath);
        var dialog = markup[markup.IndexOf("id=\"notification-flyout\"", StringComparison.Ordinal)..];

        Assert.Matches(
            new Regex(@"role=""dialog""[^>]*>\s*@\*[^*]*\*@\s*<div class=""notification-flyout-backdrop"" aria-hidden=""true"" @onclick=""CloseFlyout"">", RegexOptions.Singleline),
            dialog);
    }

    [Fact]
    public void Tablet_KeepsTheSheetClearOfTheSidebar()
    {
        var css = ReadComponent(_stylesPath);

        Assert.Matches(
            new Regex(@"@media \(min-width: 641px\) \{\s*\.notification-flyout \{\s*width: min\(26rem, calc\(100vw - 232px - 3rem\)\);", RegexOptions.Singleline),
            css);
    }

    [Fact]
    public void Phone_ShowsAFullWidthScrollingSheetBelowTheHeader()
    {
        var css = ReadComponent(_stylesPath);
        var mobile = css[css.IndexOf("@media (max-width: 640.98px)", StringComparison.Ordinal)..];

        Assert.Matches(
            new Regex(@"\.notification-flyout \{\s*position: fixed;\s*top: var\(--shell-header-height, 4rem\);\s*right: 0;\s*left: 0;", RegexOptions.Singleline),
            mobile);
        Assert.Contains("max-height: calc(100dvh - var(--shell-header-height, 4rem) - 1rem);", mobile, StringComparison.Ordinal);
        Assert.Matches(
            new Regex(@"\.notification-content \{[^}]*min-height: 0;[^}]*overflow-y: auto;", RegexOptions.Singleline),
            css);
    }

    [Fact]
    public void UnreadItems_AreAnnouncedAndMarkedWithoutRelyingOnColour()
    {
        var markup = ReadComponent(_flyoutPath);
        var css = ReadComponent(_stylesPath);

        Assert.Contains("<span class=\"visually-hidden\">Unread: </span>", markup, StringComparison.Ordinal);
        Assert.Contains("<ul class=\"notification-list\" aria-label=\"Notifications\">", markup, StringComparison.Ordinal);
        Assert.Contains(".notification-item.read .unread-indicator", css, StringComparison.Ordinal);
        Assert.Contains("id=\"notification-flyout-description\">@GetSummary()</p>", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void MarkAllAndDelete_ShowBusyLabelsAndTheEmptyStateExplainsWhatAppears()
    {
        var markup = ReadComponent(_flyoutPath);

        Assert.Contains("<span>Marking as read…</span>", markup, StringComparison.Ordinal);
        Assert.Contains("_isMarkingAll = false;", markup, StringComparison.Ordinal);
        Assert.Contains("@(_isDeleting ? \"Deleting…\" : \"Delete\")", markup, StringComparison.Ordinal);
        Assert.Contains("Event invitations and updates from your friends and lists will show up here.", markup, StringComparison.Ordinal);
        Assert.Contains("@GetOpenLabel(action.NavigateTo!)", markup, StringComparison.Ordinal);
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