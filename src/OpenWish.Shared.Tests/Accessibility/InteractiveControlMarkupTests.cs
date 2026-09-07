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

    [Fact]
    public void EventInvitationFilters_ExposeTheirStateAndMatchingResults()
    {
        var markup = ReadComponent("OpenWish.Web.Client", "Components", "Event", "EventInvitations.razor");

        Assert.Contains("aria-label=\"Filter invitations by status\"", markup, StringComparison.Ordinal);
        Assert.Contains("aria-pressed=\"@(_filter == \"All\")\"", markup, StringComparison.Ordinal);
        Assert.Contains("aria-controls=\"event-invitations-list\"", markup, StringComparison.Ordinal);
        Assert.Contains("id=\"event-invitations-list\"", markup, StringComparison.Ordinal);
        Assert.Contains("@FilteredInvitationCountMessage", markup, StringComparison.Ordinal);
        Assert.Contains("var count = FilteredInvitations.Count();", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void EventInvitationDialogs_ExposeTitlesTabsAndSearchLabels()
    {
        var markup = ReadComponent("OpenWish.Web.Client", "Components", "Event", "EventInvitations.razor");

        Assert.Contains("aria-labelledby=\"remove-participant-dialog-title\"", markup, StringComparison.Ordinal);
        Assert.Contains("aria-describedby=\"remove-participant-dialog-description\"", markup, StringComparison.Ordinal);
        Assert.Contains("aria-controls=\"event-invite-email-panel\"", markup, StringComparison.Ordinal);
        Assert.Contains("role=\"tabpanel\"", markup, StringComparison.Ordinal);
        Assert.Contains("hidden=\"@(_inviteMode != \"email\")\"", markup, StringComparison.Ordinal);
        Assert.Contains("hidden=\"@(_inviteMode != \"friends\")\"", markup, StringComparison.Ordinal);
        Assert.Contains("for=\"event-invite-friend-search\"", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void PendingInvitationDecline_IdentifiesItsConfirmationControls()
    {
        var markup = ReadComponent("OpenWish.Web.Client", "Components", "Event", "PendingInvitations.razor");

        Assert.Contains("aria-controls=\"decline-invitation-@invitation.Id\"", markup, StringComparison.Ordinal);
        Assert.Contains("aria-label=\"Confirm declining @(invitation.Event?.Name ?? \"this event\")\"", markup, StringComparison.Ordinal);
        Assert.Contains("aria-expanded=\"true\"", markup, StringComparison.Ordinal);
        Assert.Contains("Keep invitation", markup, StringComparison.Ordinal);
        Assert.Contains("<span>Declining...</span>", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void EventWishlistRemoval_ConnectsItsLabelAndDialogDescription()
    {
        var markup = ReadComponent("OpenWish.Web.Client", "Components", "Event", "EventWishlistManager.razor");

        Assert.Contains("for=\"event-wishlist-select\"", markup, StringComparison.Ordinal);
        Assert.Contains("id=\"event-wishlist-select\"", markup, StringComparison.Ordinal);
        Assert.Contains("aria-describedby=\"remove-wishlist-dialog-description\"", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void GiftExchangePairingRules_KeepVisibleLabelsConnectedToSelectors()
    {
        var markup = ReadComponent("OpenWish.Web.Client", "Components", "Event", "GiftExchangeManager.razor");

        Assert.Contains("for=\"pairing-rule-source\"", markup, StringComparison.Ordinal);
        Assert.Contains("id=\"pairing-rule-source\"", markup, StringComparison.Ordinal);
        Assert.Contains("for=\"pairing-rule-target\"", markup, StringComparison.Ordinal);
        Assert.Contains("id=\"pairing-rule-target\"", markup, StringComparison.Ordinal);
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