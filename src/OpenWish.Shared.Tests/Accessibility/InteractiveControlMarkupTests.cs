using Xunit;

namespace OpenWish.Shared.Tests.Accessibility;

public class InteractiveControlMarkupTests
{
    [Fact]
    public void WishlistIndex_ConnectsTabsAndDiscoveryControlsToTheirContent()
    {
        var markup = ReadComponent("OpenWish.Web.Client", "Components", "Pages", "Wishlists", "Index.razor");

        Assert.Contains("aria-controls=\"my-wishlists-panel\"", markup, StringComparison.Ordinal);
        Assert.Contains("aria-controls=\"friends-wishlists-panel\"", markup, StringComparison.Ordinal);
        Assert.Contains("role=\"tabpanel\"", markup, StringComparison.Ordinal);
        Assert.Contains("hidden=\"@(activeTab != \"my-wishlists\")\"", markup, StringComparison.Ordinal);
        Assert.Contains("hidden=\"@(activeTab != \"friends-wishlists\")\"", markup, StringComparison.Ordinal);
        Assert.Contains("for=\"wishlist-search\"", markup, StringComparison.Ordinal);
        Assert.Contains("for=\"wishlist-filter\"", markup, StringComparison.Ordinal);
        Assert.Contains("for=\"wishlist-sort\"", markup, StringComparison.Ordinal);
        Assert.Contains("aria-live=\"polite\"", markup, StringComparison.Ordinal);
        Assert.Contains("\"wishlist\" : \"wishlists\") found.", markup, StringComparison.Ordinal);
        Assert.Contains("filteredWishlists = filtered.ToList();", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void WishlistItemInteractions_ExposeTheirExpandedStateAndContent()
    {
        var listMarkup = ReadComponent("OpenWish.Web.Client", "Components", "Wishlist", "WishlistItemList.razor");
        var cardMarkup = ReadComponent("OpenWish.Web.Client", "Components", "Wishlist", "WishlistItemCard.razor");

        Assert.Contains("aria-expanded=\"@ExpandedInteractions.Contains(item.Id)\"", listMarkup, StringComparison.Ordinal);
        Assert.Contains("id=\"item-interactions-@item.Id\"", listMarkup, StringComparison.Ordinal);
        Assert.Contains("hidden=\"@(!ExpandedInteractions.Contains(item.Id))\"", listMarkup, StringComparison.Ordinal);
        Assert.Contains("aria-expanded=\"@ShowReservation\"", cardMarkup, StringComparison.Ordinal);
        Assert.Contains("id=\"item-reservation-@Item.Id\"", cardMarkup, StringComparison.Ordinal);
        Assert.Contains("aria-expanded=\"@ShowComments\"", cardMarkup, StringComparison.Ordinal);
        Assert.Contains("id=\"item-comments-@Item.Id\"", cardMarkup, StringComparison.Ordinal);
    }

    [Fact]
    public void EventDeletionConfirmation_HasAnAccessibleDestructiveDialogDescription()
    {
        var markup = ReadComponent("OpenWish.Web.Client", "Components", "Pages", "Events", "ManageEvent.razor");

        Assert.Contains("role=\"dialog\"", markup, StringComparison.Ordinal);
        Assert.Contains("aria-labelledby=\"delete-event-dialog-title\"", markup, StringComparison.Ordinal);
        Assert.Contains("aria-describedby=\"delete-event-dialog-description\"", markup, StringComparison.Ordinal);
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