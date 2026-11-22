using Xunit;

namespace OpenWish.Shared.Tests.Ux;

public class FollowupMarkupTests
{
    [Fact]
    public void Dialogs_FocusTheHeadingInsteadOfATextFieldOnTouchScreens()
    {
        var script = Normalize(ReadComponent("OpenWish.Web", "wwwroot", "app.js"));
        var modal = ReadComponent("OpenWish.Web.Client", "Components", "Wishlist", "WishlistItemModal.razor");

        Assert.Contains("window.matchMedia(\"(pointer: coarse)\")", script, StringComparison.Ordinal);
        Assert.Contains("window.openWishFocusDialogField = ", script, StringComparison.Ordinal);
        var activate = script[script.IndexOf("window.openWishActivateDialog = ", StringComparison.Ordinal)..];
        Assert.Contains("openWishPrefersCalmDialogFocus()", activate, StringComparison.Ordinal);
        Assert.Contains("\"openWishFocusDialogField\", \"product-url-import-modal\", \"wishlist-item-dialog\"", modal, StringComparison.Ordinal);
    }

    [Fact]
    public void WishlistDeleteDialog_OnlyOpensWithANamedTitle()
    {
        var dialog = ReadComponent("OpenWish.Web.Client", "Components", "Wishlist", "WishlistDeleteDialog.razor");

        Assert.DoesNotContain("public void Show(", dialog, StringComparison.Ordinal);
        Assert.Contains("ArgumentException.ThrowIfNullOrWhiteSpace(title);", dialog, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("Wishlists", "ManageWishlist.razor")]
    [InlineData("Wishlists", "AddItem.razor")]
    [InlineData("Wishlists", "NewWishlist.razor")]
    [InlineData("Events", "ManageEvent.razor")]
    [InlineData("Events", "NewEvent.razor")]
    public void PageHeaderBackLinks_AreCompactOnPhones(string folder, string page)
    {
        var markup = ReadComponent("OpenWish.Web.Client", "Components", "Pages", folder, page);
        var styles = Normalize(ReadComponent("OpenWish.Web.Client", "Components", "Shared", "PageHeader.razor.css"));

        Assert.Contains("page-header-back", markup, StringComparison.Ordinal);
        var phone = styles[styles.IndexOf("@media (max-width: 767.98px)", StringComparison.Ordinal)..];
        Assert.Contains(".btn.page-header-back {\n        align-self: flex-start;\n        width: auto;", phone, StringComparison.Ordinal);
    }

    [Fact]
    public void EventParticipants_UseQuietBadges()
    {
        var manage = ReadComponent("OpenWish.Web.Client", "Components", "Pages", "Events", "ManageEvent.razor");
        var manager = Normalize(ReadComponent("OpenWish.Web.Client", "Components", "Event", "EventWishlistManager.razor.css"));

        Assert.Contains("@if (ShouldShowRole(participant))", manage, StringComparison.Ordinal);
        Assert.DoesNotContain("--color-info", manager[manager.IndexOf(".event-participant-badge-you {", StringComparison.Ordinal)..][..120], StringComparison.Ordinal);
    }

    [Fact]
    public void NewEventSidePanel_StacksBelowTheFormOnTablets()
    {
        var page = ReadComponent("OpenWish.Web.Client", "Components", "Pages", "Events", "NewEvent.razor");
        var styles = Normalize(ReadComponent("OpenWish.Web.Client", "Components", "Pages", "Events", "NewEvent.razor.css"));

        Assert.Contains("col-lg-8", page, StringComparison.Ordinal);
        Assert.DoesNotContain("col-md-4", page, StringComparison.Ordinal);
        Assert.Contains("top: var(--shell-sticky-top, 1rem);", styles, StringComparison.Ordinal);
    }

    [Fact]
    public void DarkOutlineSecondaryButtons_StayOutlined()
    {
        var styles = Normalize(ReadComponent("OpenWish.Web", "wwwroot", "app.css"));

        Assert.Contains(":root[data-theme='dark'] .btn-outline-secondary {\n    background-color: transparent;", styles, StringComparison.Ordinal);
    }

    [Fact]
    public void StickyElements_RestBelowTheShellHeader()
    {
        var layout = Normalize(ReadComponent("OpenWish.Web", "Components", "Layout", "MainLayout.razor.css"));
        var details = Normalize(ReadComponent("OpenWish.Web.Client", "Components", "Pages", "Wishlists", "WishlistDetails.razor.css"));

        Assert.Contains("--shell-sticky-top: calc(var(--shell-header-height) + 0.75rem);", layout, StringComparison.Ordinal);
        var banner = details[details.IndexOf(".feedback-banner {", StringComparison.Ordinal)..];
        Assert.Contains("top: var(--shell-sticky-top, 1rem);", banner[..banner.IndexOf('}')], StringComparison.Ordinal);
    }

    [Fact]
    public void EmptyWishlist_OffersOneAddActionWithoutIdleControls()
    {
        var details = Normalize(ReadComponent("OpenWish.Web.Client", "Components", "Pages", "Wishlists", "WishlistDetails.razor"));
        var emptyState = details[details.IndexOf("<h2>No ideas in this wishlist yet</h2>", StringComparison.Ordinal)..];

        Assert.DoesNotContain("ShowAddItemModal", emptyState[..emptyState.IndexOf("else", StringComparison.Ordinal)], StringComparison.Ordinal);
        Assert.Contains("@if (_canEdit && _items.Count > 0)\n    {\n        <p class=\"paste-hint\">", details, StringComparison.Ordinal);
        Assert.Contains("@if (_items.Count > 0)\n    {\n        <div class=\"stats-dashboard\">", details, StringComparison.Ordinal);
    }

    private static string Normalize(string text) => text.Replace("\r\n", "\n", StringComparison.Ordinal);

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