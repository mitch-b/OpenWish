using Xunit;

namespace OpenWish.Shared.Tests.Ux;

public class FriendListMarkupTests
{
    [Fact]
    public void RemoveDialog_RendersItsTitleBeforeOpeningAndReportsClosing()
    {
        var dialog = Read("Social", "FriendRemoveConfirmationDialog.razor");

        var show = dialog.IndexOf("public void Show(", StringComparison.Ordinal);
        Assert.True(show >= 0);
        Assert.True(
            dialog.IndexOf("StateHasChanged();", show, StringComparison.Ordinal) <
            dialog.IndexOf("dialog.Show();", show, StringComparison.Ordinal));
        Assert.Contains("OnClose=\"HandleClosed\"", dialog, StringComparison.Ordinal);
        Assert.Contains("CanClose=\"@(!_isRemoving)\"", dialog, StringComparison.Ordinal);
    }

    [Fact]
    public void FriendList_NamesTheFriendInTheDialogAndReturnsFocusToTheList()
    {
        var list = Read("Social", "FriendList.razor");

        Assert.Contains("var title = $\"Remove {friendName}?\";", list, StringComparison.Ordinal);
        Assert.Contains("OnClosed=\"HandleRemoveDialogClosed\"", list, StringComparison.Ordinal);
        Assert.Contains("<h2 @ref=\"_heading\" tabindex=\"-1\">", list, StringComparison.Ordinal);
        Assert.Contains("await _heading.FocusAsync(preventScroll: true);", list, StringComparison.Ordinal);
    }

    [Fact]
    public void FriendList_GuardsActionsUntilInteractiveAndOffersANextStepWhenEmpty()
    {
        var list = Read("Social", "FriendList.razor");
        var styles = Read("Social", "FriendList.razor.css");

        Assert.Equal(4, list.Split("disabled=\"@(!RendererInfo.IsInteractive)\"", StringSplitOptions.None).Length - 1);
        Assert.Contains("data-testid=\"friends-empty\"", list, StringComparison.Ordinal);
        Assert.Contains("href=\"/friends?section=invite\"", list, StringComparison.Ordinal);
        Assert.Contains("href=\"/friends?section=in-person\"", list, StringComparison.Ordinal);
        Assert.DoesNotContain("alert alert-info", list, StringComparison.Ordinal);
        Assert.Contains("<span class=\"friend-action-label\">Remove</span>", list, StringComparison.Ordinal);
        Assert.Contains("overflow-wrap: break-word;", styles, StringComparison.Ordinal);
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
        return File.ReadAllText(Path.Combine([root, "OpenWish.Web.Client", "Components", .. segments]));
    }
}