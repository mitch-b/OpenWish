using Xunit;

namespace OpenWish.Shared.Tests.Accessibility;

public class GiftCoordinationMarkupTests
{
    [Fact]
    public void Comments_LoadOncePerItemAndKeepTheComposerDuringRefreshes()
    {
        var markup = ReadComponent("ItemComments.razor");

        Assert.Contains("var key = $\"{WishlistId}/{ItemPublicId}\";", markup, StringComparison.Ordinal);
        Assert.Contains("if (key == _loadedKey)", markup, StringComparison.Ordinal);
        Assert.Contains("@if (_loading && !_hasLoaded)", markup, StringComparison.Ordinal);
        Assert.Contains("_hasLoaded = true;", markup, StringComparison.Ordinal);
        Assert.Contains("string.IsNullOrWhiteSpace(commentText) || _loading || _isAdding", markup, StringComparison.Ordinal);
        Assert.Contains("_pendingDeleteComment is null || _loading", markup, StringComparison.Ordinal);
        Assert.Equal(3, markup.Split("await NotifyCommentsChangedAsync();").Length - 1);
    }

    [Fact]
    public void Comments_UpdateTheWishlistCountsAfterAddingOrDeleting()
    {
        var list = ReadComponent("WishlistItemList.razor");
        var details = ReadComponent(Path.Combine("..", "Pages", "Wishlists", "WishlistDetails.razor"));
        // Deleting the last comment under "Has comments" removes the thread; explain why and keep focus on the page.
        Assert.Contains("if (wasShown && HasActiveFilters && !_filteredItems.Contains(item))", details, StringComparison.Ordinal);
        Assert.Contains("It isn't shown because of your current filters.", details, StringComparison.Ordinal);
        Assert.Contains("tabindex=\"-1\" @ref=\"_appliedFiltersHeading\"", details, StringComparison.Ordinal);
        Assert.Contains("await _appliedFiltersHeading.FocusAsync();", details, StringComparison.Ordinal);

        Assert.Contains("OnCommentsChanged=\"OnCommentsChanged\"", list, StringComparison.Ordinal);
        Assert.Equal(3, details.Split("OnCommentsChanged=\"@HandleCommentsChanged\"").Length - 1);
        Assert.Contains("item.Comments = change.Comments.ToList();", details, StringComparison.Ordinal);
    }

    [Fact]
    public void Comments_SendWithCtrlEnterTrimTextAndUseFriendlyTimestamps()
    {
        var markup = ReadComponent("ItemComments.razor");

        Assert.Contains("@onkeydown=\"HandleComposerKeyDown\"", markup, StringComparison.Ordinal);
        Assert.Contains("args.Key == \"Enter\" && (args.CtrlKey || args.MetaKey)", markup, StringComparison.Ordinal);
        Assert.Contains("Press Ctrl+Enter to send.", markup, StringComparison.Ordinal);
        Assert.Contains("commentText.Trim()", markup, StringComparison.Ordinal);
        Assert.Contains("ToString(\"MMM d, yyyy h:mm tt\")", markup, StringComparison.Ordinal);
        Assert.DoesNotContain("MM/dd/yyyy", markup, StringComparison.Ordinal);
        Assert.Contains("_focusCommentComposer = true;", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Reservation_KeepsItsPanelDuringRefreshesAndOffersTheGiftPlan()
    {
        var markup = ReadComponent("ItemReservation.razor");

        Assert.Contains("@if (_loading && !_hasLoaded)", markup, StringComparison.Ordinal);
        Assert.Contains("href=\"/gift-plan\"", markup, StringComparison.Ordinal);
        Assert.Contains("View my gift plan", markup, StringComparison.Ordinal);
        Assert.Contains("_showGiftPlanLink = true;", markup, StringComparison.Ordinal);
        Assert.Contains("_hasLoaded |= _loadErrorMessage is null;", markup, StringComparison.Ordinal);
        Assert.DoesNotContain("disabled=\"@_isUpdating\"", markup, StringComparison.Ordinal);
        Assert.Equal(2, markup.Split("if (_loading || _isUpdating)").Length - 1);
    }

    [Fact]
    public void GiftHints_LoadOncePerListAndViewer()
    {
        var markup = ReadComponent("GiftProfilePanel.razor");

        Assert.Contains("var key = $\"{WishlistPublicId}/{ViewerId}\";", markup, StringComparison.Ordinal);
        Assert.Contains("if (key == _loadedKey)", markup, StringComparison.Ordinal);
        Assert.Contains("_loadedKey = null;", markup, StringComparison.Ordinal);
    }

    private static string ReadComponent(string fileName)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "OpenWish.slnx")))
        {
            directory = directory.Parent;
        }

        var root = directory?.FullName
            ?? throw new DirectoryNotFoundException("Could not locate OpenWish.slnx.");
        return File.ReadAllText(Path.Combine(root, "OpenWish.Web.Client", "Components", "Wishlist", fileName));
    }
}