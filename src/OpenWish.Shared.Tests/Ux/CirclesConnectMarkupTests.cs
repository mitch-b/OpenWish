using Xunit;

namespace OpenWish.Shared.Tests.Ux;

public class CirclesConnectMarkupTests
{
    [Fact]
    public void ConnectInPerson_StylesItsOwnHeadingAndGuardsActionsUntilInteractive()
    {
        var markup = Read("Social", "ConnectInPerson.razor");
        var styles = Read("Social", "ConnectInPerson.razor.css");

        Assert.Contains(".social-heading-icon {", styles, StringComparison.Ordinal);
        Assert.Contains("<span class=\"social-heading-icon\" aria-hidden=\"true\">", markup, StringComparison.Ordinal);
        Assert.Equal(3, Count(markup, "disabled=\"@(_busy || !RendererInfo.IsInteractive)\""));
        Assert.Contains("disabled=\"@(string.IsNullOrWhiteSpace(_enteredCode) || !RendererInfo.IsInteractive)\"", markup, StringComparison.Ordinal);
        Assert.Contains("placeholder=\"ABCD 1234\"", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void ConnectInPerson_CentersTheCodeAndKeepsActionsOnOneLineOnPhones()
    {
        var styles = Read("Social", "ConnectInPerson.razor.css");
        var mobile = styles[styles.IndexOf("@media (max-width: 576px)", StringComparison.Ordinal)..];

        Assert.Contains("align-self: center;", mobile, StringComparison.Ordinal);
        Assert.Contains("grid-template-columns: 1fr 1fr;", mobile, StringComparison.Ordinal);
        Assert.Contains("white-space: nowrap;", mobile, StringComparison.Ordinal);
    }

    [Fact]
    public void CirclesPanel_ConfirmsDeletionInPlaceAndExplainsAnEmptyState()
    {
        var markup = Read("Social", "CirclesPanel.razor");

        Assert.Contains("data-testid=\"circles-empty\"", markup, StringComparison.Ordinal);
        Assert.Contains("data-testid=\"circle-delete-confirm\"", markup, StringComparison.Ordinal);
        Assert.Contains("Keep circle", markup, StringComparison.Ordinal);
        Assert.Contains("@(_busy ? \"Deleting...\" : \"Delete circle\")", markup, StringComparison.Ordinal);
        Assert.Contains("aria-label=\"View lists from @circle.Name\"", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void CirclesPanel_GuardsEveryClickUntilInteractiveAndFocusesRename()
    {
        var markup = Read("Social", "CirclesPanel.razor");

        var clicks = Count(markup, "@onclick=");
        Assert.True(clicks >= 7);
        Assert.Equal(clicks, Count(markup, "!RendererInfo.IsInteractive)") - 2);
        Assert.Contains("@ref=\"_renameInput\"", markup, StringComparison.Ordinal);
        Assert.Contains("await _renameInput.FocusAsync();", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void FriendJoin_UsesAFocusedConfirmationCardWithGuardedActions()
    {
        var markup = Read("Pages", "Social", "FriendJoin.razor");
        var styles = Read("Pages", "Social", "FriendJoin.razor.css");

        Assert.Contains("<h1 id=\"friend-join-title\"", markup, StringComparison.Ordinal);
        Assert.Contains("class=\"spinner-border spinner-border-sm\" aria-hidden=\"true\"", markup, StringComparison.Ordinal);
        Assert.Contains("disabled=\"@(_busy || !RendererInfo.IsInteractive)\" @onclick=\"ConfirmAsync\"", markup, StringComparison.Ordinal);
        Assert.Contains("disabled=\"@(!RendererInfo.IsInteractive)\" @onclick=\"LoadAsync\"", markup, StringComparison.Ordinal);
        Assert.Contains("href=\"friends?section=in-person\">Enter another code", markup, StringComparison.Ordinal);
        Assert.Contains(".friend-join-actions .btn {\n        width: 100%;", styles, StringComparison.Ordinal);
    }

    private static int Count(string source, string value) =>
        source.Split(value, StringSplitOptions.None).Length - 1;

    private static string Read(params string[] segments)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "OpenWish.slnx")))
        {
            directory = directory.Parent;
        }

        var root = directory?.FullName
            ?? throw new DirectoryNotFoundException("Could not locate OpenWish.slnx.");
        return File.ReadAllText(Path.Combine([root, "OpenWish.Web.Client", "Components", .. segments])).ReplaceLineEndings("\n");
    }
}