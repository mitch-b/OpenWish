using Xunit;

namespace OpenWish.Shared.Tests.Ux;

public class WishlistsIndexMarkupTests
{
    [Fact]
    public void WishlistsIndex_HidesDiscoveryToolsUntilTheOwnerHasLists()
    {
        var markup = ReadComponent("OpenWish.Web.Client", "Components", "Pages", "Wishlists", "Index.razor");

        Assert.Contains("@if (HasLoadedOwnWishlists)", markup, StringComparison.Ordinal);
        Assert.Contains("!_loadingWishlists && string.IsNullOrWhiteSpace(_wishlistLoadError) && TotalWishlistCount > 0", markup, StringComparison.Ordinal);
        Assert.True(
            markup.IndexOf("@if (HasLoadedOwnWishlists)", StringComparison.Ordinal) <
            markup.IndexOf("<div class=\"wishlist-toolbar\">", StringComparison.Ordinal),
            "The search and sort toolbar should only render once the owner has lists.");
        Assert.True(
            markup.IndexOf("aria-labelledby=\"wishlist-pulse-title\"", StringComparison.Ordinal) <
            markup.IndexOf("<div id=\"wishlist-results\">", StringComparison.Ordinal),
            "The collection summary should sit above the filters, not between filters and results.");
    }

    [Fact]
    public void WishlistsIndex_EmptyStateOffersCreateAndFriendsActions()
    {
        var markup = ReadComponent("OpenWish.Web.Client", "Components", "Pages", "Wishlists", "Index.razor");

        Assert.Contains("<OpenWishIcon Name=\"plus-lg\" /> New wishlist", markup, StringComparison.Ordinal);
        Assert.Contains("Create your first wishlist</a>", markup, StringComparison.Ordinal);
        Assert.Contains("<a href=\"/wishlists?tab=friends\" class=\"btn btn-outline-secondary\">See friends' wishlists</a>", markup, StringComparison.Ordinal);
        Assert.Contains("<span>Loading friends' wishlists...</span>", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void WishlistsIndex_KeepsTabsOnOneRowAndGuardsControlsUntilHydrated()
    {
        var markup = ReadComponent("OpenWish.Web.Client", "Components", "Pages", "Wishlists", "Index.razor");
        var styles = ReadComponent("OpenWish.Web.Client", "Components", "Pages", "Wishlists", "Index.razor.css");

        Assert.Contains("@onclick='() => SetActiveTab(\"my-wishlists\")' type=\"button\" role=\"tab\"\n                    disabled=\"@(!RendererInfo.IsInteractive)\"", markup, StringComparison.Ordinal);
        Assert.Contains("@onclick='() => SetActiveTab(\"friends-wishlists\")' type=\"button\" role=\"tab\"\n                    disabled=\"@(!RendererInfo.IsInteractive)\"", markup, StringComparison.Ordinal);
        Assert.Contains("disabled=\"@(!RendererInfo.IsInteractive)\" @onclick=\"ClearFriendFilters\"", markup, StringComparison.Ordinal);
        Assert.Contains("white-space: nowrap;", styles, StringComparison.Ordinal);
        Assert.Contains(".nav-tabs ::deep .ow-icon", styles, StringComparison.Ordinal);
        Assert.Contains(".filter-cluster > .filter-field:first-child", styles, StringComparison.Ordinal);
    }

    [Fact]
    public void WishlistCard_ShowsOwnerPrivacyAndEventLinkAboveTheStretchedLink()
    {
        var markup = ReadComponent("OpenWish.Web.Client", "Components", "Wishlist", "WishlistCard.razor");
        var styles = ReadComponent("OpenWish.Web.Client", "Components", "Wishlist", "WishlistCard.razor.css");

        Assert.Contains("@if (isCurrentUserOwner)\n                {\n                    <span class=\"wishlist-meta-chip\" title=\"@PrivacyDescription\">", markup, StringComparison.Ordinal);
        Assert.Contains("{ IsFriendsOnly: true } => HasEventAccess ? \"Shared with specific friends and event members\" : \"Shared with specific friends\"", markup, StringComparison.Ordinal);
        Assert.Contains("{ IsPrivate: true } => \"Private: only you can see it\"", markup, StringComparison.Ordinal);
        Assert.Contains("<a class=\"wishlist-card-event\" href=\"/events/@eventPublicId\">", markup, StringComparison.Ordinal);
        Assert.Contains("@(DisplayItemCount == 1 ? \"idea\" : \"ideas\")", markup, StringComparison.Ordinal);
        Assert.Contains("disabled=\"@(!RendererInfo.IsInteractive)\"\n                                        @onclick:stopPropagation=\"true\"", markup, StringComparison.Ordinal);

        var eventStyles = styles[
            styles.IndexOf(".wishlist-card-event {", StringComparison.Ordinal)..styles.IndexOf(".wishlist-card-event span", StringComparison.Ordinal)];
        Assert.Contains("z-index: 2;", eventStyles, StringComparison.Ordinal);
        Assert.Contains("min-height: 2.75rem;", eventStyles, StringComparison.Ordinal);
        Assert.DoesNotContain(".wishlist-meta span {", styles, StringComparison.Ordinal);
    }

    private static string ReadComponent(params string[] pathParts)
    {
        var solutionDirectory = FindSolutionDirectory();
        return File.ReadAllText(Path.Combine([solutionDirectory, .. pathParts])).ReplaceLineEndings("\n");
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