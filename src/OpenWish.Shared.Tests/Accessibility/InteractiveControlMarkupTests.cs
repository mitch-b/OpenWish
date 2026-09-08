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
    public void WishlistDiscovery_UpdatesImmediatelyAndProvidesAClearAction()
    {
        var markup = ReadComponent("OpenWish.Web.Client", "Components", "Pages", "Wishlists", "Index.razor");

        Assert.Contains("aria-controls=\"wishlist-results\"", markup, StringComparison.Ordinal);
        Assert.Contains("@bind:event=\"oninput\" @bind:after=\"HandleSearch\"", markup, StringComparison.Ordinal);
        Assert.Contains("aria-label=\"Clear wishlist search\"", markup, StringComparison.Ordinal);
        Assert.Contains("<div id=\"wishlist-results\">", markup, StringComparison.Ordinal);
        Assert.Contains("No wishlists found.</p>", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void FriendInvitations_ConnectGuidanceValidationAndFeedback()
    {
        var markup = ReadComponent("OpenWish.Web.Client", "Components", "Social", "FriendSearch.razor");

        Assert.Contains("aria-describedby=\"emailInvitesHelp\"", markup, StringComparison.Ordinal);
        Assert.Contains("aria-invalid=\"@(_error ? \"true\" : \"false\")\"", markup, StringComparison.Ordinal);
        Assert.Contains("@oninput=\"UpdateInviteEmails\"", markup, StringComparison.Ordinal);
        Assert.Contains("string.IsNullOrWhiteSpace(inviteEmails)", markup, StringComparison.Ordinal);
        Assert.Contains("alert alert-danger mt-3\" role=\"alert\"", markup, StringComparison.Ordinal);
        Assert.Contains("alert alert-success mt-3\" role=\"status\"", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void NotificationFlyout_ExposesDisclosureStateAndDialogContext()
    {
        var markup = ReadComponent("OpenWish.Web", "Components", "Shared", "NotificationFlyout.razor");

        Assert.Contains("aria-expanded=\"@(_isVisible ? \"true\" : \"false\")\"", markup, StringComparison.Ordinal);
        Assert.Contains("aria-controls=\"notification-flyout\"", markup, StringComparison.Ordinal);
        Assert.Contains("role=\"dialog\"", markup, StringComparison.Ordinal);
        Assert.Contains("aria-labelledby=\"notification-flyout-title\"", markup, StringComparison.Ordinal);
        Assert.Contains("openWishActivateDialog\", \"notification-flyout\"", markup, StringComparison.Ordinal);
        Assert.Contains("data-dialog-close", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void NotificationDeletion_ProvidesAFocusSafeDestructiveConfirmation()
    {
        var markup = ReadComponent("OpenWish.Web", "Components", "Shared", "NotificationFlyout.razor");

        Assert.Contains("id=\"notification-delete-dialog\"", markup, StringComparison.Ordinal);
        Assert.Contains("aria-modal=\"true\"", markup, StringComparison.Ordinal);
        Assert.Contains("aria-describedby=\"notification-delete-description\"", markup, StringComparison.Ordinal);
        Assert.Contains("This cannot be undone.", markup, StringComparison.Ordinal);
        Assert.Contains("data-dialog-initial-focus", markup, StringComparison.Ordinal);
        Assert.Contains("Keep notification", markup, StringComparison.Ordinal);
        Assert.Contains("openWishDeactivateDialog\", \"notification-delete-dialog\"", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void NotificationUpdates_ExposeBusySuccessAndFailureStates()
    {
        var markup = ReadComponent("OpenWish.Web", "Components", "Shared", "NotificationFlyout.razor");

        Assert.Contains("aria-busy=\"@_isMarkingRead\"", markup, StringComparison.Ordinal);
        Assert.Contains("aria-busy=\"@IsNotificationProcessing(notification.Id)\"", markup, StringComparison.Ordinal);
        Assert.Contains("role=\"status\" aria-live=\"polite\"", markup, StringComparison.Ordinal);
        Assert.Contains("All notifications marked as read.", markup, StringComparison.Ordinal);
        Assert.Contains("The invitation could not be updated. Try again.", markup, StringComparison.Ordinal);
        Assert.Contains("Unable to delete notification {NotificationPublicId}", markup, StringComparison.Ordinal);
        Assert.Contains("ex is InvalidOperationException or DbUpdateException or DbException", markup, StringComparison.Ordinal);
        Assert.Contains("openWishFocusElement\", \"notification-close-button\"", markup, StringComparison.Ordinal);
        Assert.DoesNotContain("// Handle gracefully", markup, StringComparison.Ordinal);
        Assert.DoesNotContain("// swallow for now", markup, StringComparison.Ordinal);
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

    [Fact]
    public void WishlistItemSearch_HasALabelClearNameAndLiveResultCount()
    {
        var markup = ReadComponent("OpenWish.Web.Client", "Components", "Pages", "Wishlists", "WishlistDetails.razor");

        Assert.Contains("for=\"wishlist-item-search\"", markup, StringComparison.Ordinal);
        Assert.Contains("aria-label=\"Clear wishlist item search\"", markup, StringComparison.Ordinal);
        Assert.Contains("aria-controls=\"wishlist-items\"", markup, StringComparison.Ordinal);
        Assert.Contains("@bind:after=\"ApplyFiltersAndSort\"", markup, StringComparison.Ordinal);
        Assert.Contains("role=\"status\" aria-live=\"polite\"", markup, StringComparison.Ordinal);
        Assert.Contains("@FilteredItemCountMessage", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void WishlistFilters_ExposeDisclosureAndSelectedStates()
    {
        var markup = ReadComponent("OpenWish.Web.Client", "Components", "Pages", "Wishlists", "WishlistDetails.razor");

        Assert.Contains("aria-expanded=\"@(_showFilters ? \"true\" : \"false\")\"", markup, StringComparison.Ordinal);
        Assert.Contains("aria-controls=\"wishlist-filters\"", markup, StringComparison.Ordinal);
        Assert.Contains("id=\"wishlist-filters\"", markup, StringComparison.Ordinal);
        Assert.Contains("aria-labelledby=\"priority-filter-label\"", markup, StringComparison.Ordinal);
        Assert.Contains("aria-labelledby=\"status-filter-label\"", markup, StringComparison.Ordinal);
        Assert.Contains("aria-pressed=\"@(PriorityFilters.Contains", markup, StringComparison.Ordinal);
        Assert.Contains("aria-pressed=\"@(StatusFilters.Contains", markup, StringComparison.Ordinal);
        Assert.Contains("aria-label=\"Minimum price\"", markup, StringComparison.Ordinal);
        Assert.Contains("aria-label=\"Maximum price\"", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void WishlistSorting_ExposesItsCurrentSelection()
    {
        var markup = ReadComponent("OpenWish.Web.Client", "Components", "Pages", "Wishlists", "WishlistDetails.razor");

        Assert.Contains("aria-labelledby=\"wishlist-sort-label\"", markup, StringComparison.Ordinal);
        Assert.Contains("id=\"wishlist-sort-label\"", markup, StringComparison.Ordinal);
        Assert.Contains("aria-pressed=\"@(SortBy == \"priority\" ? \"true\" : \"false\")\"", markup, StringComparison.Ordinal);
        Assert.Contains("aria-pressed=\"@(SortBy == \"price\" ? \"true\" : \"false\")\"", markup, StringComparison.Ordinal);
        Assert.Contains("aria-pressed=\"@(SortBy == \"name\" ? \"true\" : \"false\")\"", markup, StringComparison.Ordinal);
        Assert.Contains("aria-pressed=\"@(SortBy == \"date\" ? \"true\" : \"false\")\"", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void WishlistViewSwitcher_ExposesItsCurrentView()
    {
        var markup = ReadComponent("OpenWish.Web.Client", "Components", "Pages", "Wishlists", "WishlistDetails.razor");

        Assert.Contains("role=\"group\" aria-label=\"Wishlist view\"", markup, StringComparison.Ordinal);
        Assert.Contains("aria-label=\"Grid view\"", markup, StringComparison.Ordinal);
        Assert.Contains("aria-pressed=\"@(ViewMode == \"grid\" ? \"true\" : \"false\")\"", markup, StringComparison.Ordinal);
        Assert.Contains("aria-label=\"List view\"", markup, StringComparison.Ordinal);
        Assert.Contains("aria-pressed=\"@(ViewMode == \"list\" ? \"true\" : \"false\")\"", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void WishlistItemImport_LabelsUrlFieldsAndDialog()
    {
        var formMarkup = ReadComponent("OpenWish.Web.Client", "Components", "Wishlist", "WishlistItemForm.razor");
        var modalMarkup = ReadComponent("OpenWish.Web.Client", "Components", "Wishlist", "WishlistItemModal.razor");
        var dialogScript = ReadComponent("OpenWish.Web", "wwwroot", "app.js");

        Assert.Contains("for=\"product-url-import\"", formMarkup, StringComparison.Ordinal);
        Assert.Contains("aria-describedby=\"product-url-import-help\"", formMarkup, StringComparison.Ordinal);
        Assert.Contains("@oninput=\"UpdateImportUrl\"", formMarkup, StringComparison.Ordinal);
        Assert.Contains("type=\"url\"", formMarkup, StringComparison.Ordinal);
        Assert.Contains("disabled=\"@(isLoading || string.IsNullOrWhiteSpace(ImportUrl))\"", formMarkup, StringComparison.Ordinal);
        Assert.Contains("role=\"dialog\"", modalMarkup, StringComparison.Ordinal);
        Assert.Contains("aria-modal=\"true\"", modalMarkup, StringComparison.Ordinal);
        Assert.Contains("aria-labelledby=\"wishlist-item-dialog-title\"", modalMarkup, StringComparison.Ordinal);
        Assert.Contains("@if (isLoading)", modalMarkup, StringComparison.Ordinal);
        Assert.Contains("for=\"product-url-import-modal\"", modalMarkup, StringComparison.Ordinal);
        Assert.Contains("aria-describedby=\"product-url-import-modal-help\"", modalMarkup, StringComparison.Ordinal);
        Assert.Contains("data-dialog-initial-focus", modalMarkup, StringComparison.Ordinal);
        Assert.Contains("Model.Id > 0 ? \"Save changes\" : \"Add item\"", modalMarkup, StringComparison.Ordinal);
        Assert.Contains("openWishActivateDialog", modalMarkup, StringComparison.Ordinal);
        Assert.Contains("openWishDeactivateDialog", modalMarkup, StringComparison.Ordinal);
        Assert.Contains("IAsyncDisposable", modalMarkup, StringComparison.Ordinal);
        Assert.Contains("event.key === \"Escape\"", dialogScript, StringComparison.Ordinal);
        Assert.Contains("event.key !== \"Tab\"", dialogScript, StringComparison.Ordinal);
        Assert.Contains("dialog.querySelector(\"[data-dialog-initial-focus]\")", dialogScript, StringComparison.Ordinal);
        Assert.Contains("existingState?.dialog.isConnected", dialogScript, StringComparison.Ordinal);
        Assert.Contains("state.previouslyFocused?.focus", dialogScript, StringComparison.Ordinal);
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