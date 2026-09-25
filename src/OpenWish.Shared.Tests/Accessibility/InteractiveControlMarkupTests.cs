using Xunit;

namespace OpenWish.Shared.Tests.Accessibility;

public class InteractiveControlMarkupTests
{
    [Fact]
    public void MobileNavigation_ExposesAndSynchronizesItsDisclosureState()
    {
        var markup = ReadComponent("OpenWish.Web", "Components", "Layout", "NavMenu.razor");
        var script = ReadComponent("OpenWish.Web", "wwwroot", "app.js");

        Assert.Contains("aria-label=\"Navigation menu\"", markup, StringComparison.Ordinal);
        Assert.Contains("aria-controls=\"primary-navigation\"", markup, StringComparison.Ordinal);
        Assert.Contains("aria-expanded=\"false\"", markup, StringComparison.Ordinal);
        Assert.Contains("id=\"primary-navigation\"", markup, StringComparison.Ordinal);
        Assert.Contains("document.addEventListener(\"change\"", script, StringComparison.Ordinal);
        Assert.Contains("event.target.matches(\".navbar-toggler\")", script, StringComparison.Ordinal);
        Assert.Contains("syncNavigationDisclosure(navigationToggle);", script, StringComparison.Ordinal);
    }

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
        Assert.Contains("aria-labelledby=\"wishlist-pulse-title\"", markup, StringComparison.Ordinal);
        Assert.Contains("Ideas saved", markup, StringComparison.Ordinal);
        Assert.Contains("@TotalWishlistItemCount", markup, StringComparison.Ordinal);
        Assert.Contains("@SharedWishlistCount", markup, StringComparison.Ordinal);
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
    public void WishlistDiscovery_DistinguishesNoListsFromNoMatches()
    {
        var markup = ReadComponent("OpenWish.Web.Client", "Components", "Pages", "Wishlists", "Index.razor");
        var cardMarkup = ReadComponent("OpenWish.Web.Client", "Components", "Wishlist", "WishlistCard.razor");

        Assert.Contains("TotalWishlistCount == 0", markup, StringComparison.Ordinal);
        Assert.Contains("Start your first wishlist", markup, StringComparison.Ordinal);
        Assert.Contains("No matching wishlists", markup, StringComparison.Ordinal);
        Assert.Contains("@onclick=\"ClearDiscovery\"", markup, StringComparison.Ordinal);
        Assert.Contains("filterBy = \"all\";", markup, StringComparison.Ordinal);
        Assert.Contains("ShowOwner=\"false\"", markup, StringComparison.Ordinal);
        Assert.Contains("ShowOwner &&", cardMarkup, StringComparison.Ordinal);
    }

    [Fact]
    public void WishlistDetails_PrioritizesAddAndExplainsPrivacy()
    {
        var markup = ReadComponent("OpenWish.Web.Client", "Components", "Pages", "Wishlists", "WishlistDetails.razor");
        var styles = ReadComponent("OpenWish.Web.Client", "Components", "Pages", "Wishlists", "WishlistDetails.razor.css");

        Assert.Contains("Only you can see this list", markup, StringComparison.Ordinal);
        Assert.Contains("Copy link", markup, StringComparison.Ordinal);
        Assert.Contains("<button class=\"stat-card stat-action\"", markup, StringComparison.Ordinal);
        Assert.Contains("No ideas shared yet", markup, StringComparison.Ordinal);
        Assert.Contains("Coordinate gift", markup, StringComparison.Ordinal);
        Assert.Contains("OpenItemCoordination(item.Id)", markup, StringComparison.Ordinal);
        Assert.Contains("MaxPrice = PriceCeiling;", markup, StringComparison.Ordinal);
        Assert.Contains("StatusFilters.Contains(\"reserved\") != StatusFilters.Contains(\"unreserved\")", markup, StringComparison.Ordinal);
        Assert.Contains("Math.Max(MinPrice, MaxPrice)", markup, StringComparison.Ordinal);
        Assert.Contains("Math.Min(MinPrice, MaxPrice)", markup, StringComparison.Ordinal);
        Assert.Contains("class=\"fab\"", markup, StringComparison.Ordinal);
        Assert.Contains("position: static;", styles, StringComparison.Ordinal);
    }

    [Fact]
    public void WishlistItemEntry_OffersOptionalImportAndClearPrivacyGuidance()
    {
        var form = ReadComponent("OpenWish.Web.Client", "Components", "Wishlist", "WishlistItemForm.razor");
        var modal = ReadComponent("OpenWish.Web.Client", "Components", "Wishlist", "WishlistItemModal.razor");
        var list = ReadComponent("OpenWish.Web.Client", "Components", "Wishlist", "WishlistItemList.razor");

        Assert.Contains("Have a product link?", form, StringComparison.Ordinal);
        Assert.Contains("Uri.TryCreate(url.Trim(), UriKind.Absolute", form, StringComparison.Ordinal);
        Assert.Contains("The link is still here", form, StringComparison.Ordinal);
        Assert.Contains("No product details were found. The link is ready", form, StringComparison.Ordinal);
        Assert.Contains("<option value=\"\">No priority</option>", form, StringComparison.Ordinal);
        Assert.Contains("Only you can see private ideas", form, StringComparison.Ordinal);
        Assert.Contains("Only you can see private ideas", modal, StringComparison.Ordinal);
        Assert.Contains("id=\"wishlist-item-form\"", modal, StringComparison.Ordinal);
        Assert.Contains("type=\"submit\" form=\"wishlist-item-form\"", modal, StringComparison.Ordinal);
        Assert.Contains("data-label=\"Description\"", list, StringComparison.Ordinal);
        Assert.Contains("gift options for @item.Name", list, StringComparison.Ordinal);
    }

    [Fact]
    public void FriendsWishlistDiscovery_ExplainsBothSharedAndEmptyStates()
    {
        var markup = ReadComponent("OpenWish.Web.Client", "Components", "Pages", "Wishlists", "Index.razor");

        Assert.Contains("Shared with you", markup, StringComparison.Ordinal);
        Assert.Contains("Manage friends", markup, StringComparison.Ordinal);
        Assert.Contains("Nothing shared with you yet", markup, StringComparison.Ordinal);
        Assert.Contains("Find friends", markup, StringComparison.Ordinal);
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
        Assert.Contains("aria-modal=\"true\"", markup, StringComparison.Ordinal);
        Assert.Contains("aria-labelledby=\"notification-flyout-title\"", markup, StringComparison.Ordinal);
        Assert.Contains("!_showDeleteConfirmation && !string.IsNullOrWhiteSpace(_errorMessage)", markup, StringComparison.Ordinal);
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
        Assert.Contains("class=\"notification-modal-backdrop\" aria-hidden=\"true\"", markup, StringComparison.Ordinal);
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

        Assert.Contains("aria-controls=\"decline-invitation-@invitation.PublicId\"", markup, StringComparison.Ordinal);
        Assert.Contains("aria-label=\"Confirm declining @(invitation.Event?.Name ?? \"this event\")\"", markup, StringComparison.Ordinal);
        Assert.Contains("aria-expanded=\"true\"", markup, StringComparison.Ordinal);
        Assert.Contains("Keep invitation", markup, StringComparison.Ordinal);
        Assert.Contains("<span>Declining...</span>", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void EventList_LoadFailureIsDistinctFromAnEmptyList()
    {
        var markup = ReadComponent("OpenWish.Web.Client", "Components", "Pages", "Events", "Index.razor");

        Assert.Contains("_events == null && _isLoading", markup, StringComparison.Ordinal);
        Assert.Contains("Your events could not be loaded. Try again.", markup, StringComparison.Ordinal);
        Assert.Contains("@onclick=\"RetryLoadAsync\"", markup, StringComparison.Ordinal);
        Assert.Contains("role=\"alert\"", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void EventList_RefreshFailurePreservesLoadedEvents()
    {
        var markup = ReadComponent("OpenWish.Web.Client", "Components", "Pages", "Events", "Index.razor");

        Assert.Contains("var events = (await EventService.GetUserEventsAsync(_userId)).ToList();", markup, StringComparison.Ordinal);
        Assert.Contains("_events = events;", markup, StringComparison.Ordinal);
        Assert.Contains("The events already shown are still available.", markup, StringComparison.Ordinal);
        Assert.Contains("LoadEventsAsync(announceRefresh: true)", markup, StringComparison.Ordinal);
        Assert.Contains("var loadVersion = ++_loadVersion;", markup, StringComparison.Ordinal);
        Assert.Contains("if (loadVersion != _loadVersion)", markup, StringComparison.Ordinal);
        Assert.Contains("if (loadVersion == _loadVersion)", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void PendingInvitations_LoadFailureIsVisibleAndRetryable()
    {
        var markup = ReadComponent("OpenWish.Web.Client", "Components", "Event", "PendingInvitations.razor");

        Assert.Contains("!string.IsNullOrWhiteSpace(_loadError)", markup, StringComparison.Ordinal);
        Assert.Contains("Pending invitations could not be loaded. Try again.", markup, StringComparison.Ordinal);
        Assert.Contains("@onclick=\"LoadInvitations\"", markup, StringComparison.Ordinal);
        Assert.Contains("aria-busy=\"@(_isLoading ? \"true\" : \"false\")\"", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void PendingInvitationAcceptance_RetainsFailedInvitations()
    {
        var markup = ReadComponent("OpenWish.Web.Client", "Components", "Event", "PendingInvitations.razor");

        Assert.Contains("if (!success)", markup, StringComparison.Ordinal);
        Assert.Contains("could not be accepted. Try again.", markup, StringComparison.Ordinal);
        Assert.Contains("AcceptEventInvitationByPublicIdAsync(invitation.PublicId", markup, StringComparison.Ordinal);
        Assert.Contains("_invitations?.RemoveAll(i => i.PublicId == invitation.PublicId);", markup, StringComparison.Ordinal);
        Assert.Contains("IsProcessing(invitation.PublicId)", markup, StringComparison.Ordinal);
        Assert.DoesNotContain("IsProcessing(invitation.Id)", markup, StringComparison.Ordinal);
        Assert.Contains("<span>Accepting...</span>", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void PendingInvitationDecline_RetainsFailedInvitations()
    {
        var markup = ReadComponent("OpenWish.Web.Client", "Components", "Event", "PendingInvitations.razor");

        Assert.Contains("could not be declined. Try again.", markup, StringComparison.Ordinal);
        Assert.Contains("RejectEventInvitationByPublicIdAsync(invitation.PublicId", markup, StringComparison.Ordinal);
        Assert.Contains("_pendingDeclinePublicId = null;", markup, StringComparison.Ordinal);
        Assert.Contains("_statusMessage = $\"{GetEventName(invitation)} declined.\"", markup, StringComparison.Ordinal);
        Assert.Contains("Logger.LogError(ex, \"Failed to decline event invitation {InvitationPublicId}.\"", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void EventWishlists_ExposeRecoverableLoadingAndRemovalStates()
    {
        var markup = ReadComponent("OpenWish.Web.Client", "Components", "Event", "EventWishlistManager.razor");

        Assert.Contains("for=\"event-wishlist-select\"", markup, StringComparison.Ordinal);
        Assert.Contains("id=\"event-wishlist-select\"", markup, StringComparison.Ordinal);
        Assert.Contains("aria-describedby=\"remove-wishlist-dialog-description\"", markup, StringComparison.Ordinal);
        Assert.Contains("!_hasLoaded && !string.IsNullOrWhiteSpace(_loadErrorMessage)", markup, StringComparison.Ordinal);
        Assert.Contains("We couldn't load event wishlists. Try again.", markup, StringComparison.Ordinal);
        Assert.Contains("@onclick=\"LoadWishlistsAsync\"", markup, StringComparison.Ordinal);
        Assert.Contains("BuildParticipantGroups() is not { Count: > 0 } participantGroups", markup, StringComparison.Ordinal);
        Assert.Contains("@foreach (var participant in participantGroups)", markup, StringComparison.Ordinal);
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
    public void GiftExchangeForm_OffersAStyleChoiceAndContextualGuidance()
    {
        var markup = ReadComponent("OpenWish.Web.Client", "Components", "Event", "EventForm.razor");
        var styles = ReadComponent("OpenWish.Web.Client", "Components", "Event", "EventForm.razor.css");

        Assert.Contains("Choose an exchange style", markup, StringComparison.Ordinal);
        Assert.Contains("aria-label=\"Gift exchange style\"", markup, StringComparison.Ordinal);
        Assert.Contains("Secret Santa", markup, StringComparison.Ordinal);
        Assert.Contains("Gift exchange", markup, StringComparison.Ordinal);
        Assert.Contains("placeholder=\"@EventNamePlaceholder\"", markup, StringComparison.Ordinal);
        Assert.Contains("Suggested budget per gift", markup, StringComparison.Ordinal);
        Assert.Contains(".exchange-style-options", styles, StringComparison.Ordinal);
        Assert.Contains("grid-template-columns: 1fr;", styles, StringComparison.Ordinal);
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
    public void WishlistItemDeletion_UsesOneFocusSafeFailureAwareDialog()
    {
        var detailsMarkup = ReadComponent("OpenWish.Web.Client", "Components", "Pages", "Wishlists", "WishlistDetails.razor");
        var listMarkup = ReadComponent("OpenWish.Web.Client", "Components", "Wishlist", "WishlistItemList.razor");
        var dialogMarkup = ReadComponent("OpenWish.Web.Client", "Components", "Wishlist", "WishlistDeleteDialog.razor");

        Assert.Contains("id=\"delete-wishlist-item-@item.Id\"", detailsMarkup, StringComparison.Ordinal);
        Assert.Contains("id=\"delete-wishlist-item-@item.Id\"", listMarkup, StringComparison.Ordinal);
        Assert.Contains("OnDelete=\"@ShowItemDeleteDialogAsync\"", detailsMarkup, StringComparison.Ordinal);
        Assert.Contains("await _deleteDialog.ShowAsync(", detailsMarkup, StringComparison.Ordinal);
        Assert.DoesNotContain("InvokeAsync<bool>(\"confirm\"", detailsMarkup, StringComparison.Ordinal);
        Assert.Contains("ReturnFocusElementId=\"@_returnFocusElementId\"", dialogMarkup, StringComparison.Ordinal);
        Assert.Contains("DescriptionId=\"@_descriptionId\"", dialogMarkup, StringComparison.Ordinal);
        Assert.Contains("CanClose=\"@(!IsBusy)\"", dialogMarkup, StringComparison.Ordinal);
        Assert.Contains("data-dialog-initial-focus", dialogMarkup, StringComparison.Ordinal);
        Assert.Contains("role=\"alert\">@ErrorMessage", dialogMarkup, StringComparison.Ordinal);
    }

    [Fact]
    public void WishlistItemDeletion_PreventsDuplicatesAndPreservesPageState()
    {
        var detailsMarkup = ReadComponent("OpenWish.Web.Client", "Components", "Pages", "Wishlists", "WishlistDetails.razor");
        var dialogMarkup = ReadComponent("OpenWish.Web.Client", "Components", "Wishlist", "WishlistDeleteDialog.razor");
        var httpClientMarkup = ReadComponent("OpenWish.Web.Client", "Services", "WishlistHttpClientService.cs")
            .ReplaceLineEndings("\n");
        var serviceMarkup = ReadComponent("OpenWish.Application", "Services", "WishlistService.cs");

        Assert.Contains("if (_isDeletingItem || _itemPendingDeletion is null)", detailsMarkup, StringComparison.Ordinal);
        Assert.Contains("disabled=\"@IsBusy\"", dialogMarkup, StringComparison.Ordinal);
        Assert.Contains("aria-busy=\"@IsBusy\"", dialogMarkup, StringComparison.Ordinal);
        Assert.Contains("Deleting...", dialogMarkup, StringComparison.Ordinal);
        Assert.Contains("_items.RemoveAll(existingItem => existingItem.Id == item.Id);", detailsMarkup, StringComparison.Ordinal);
        Assert.DoesNotContain("await LoadItems();\n            _feedbackMessage", detailsMarkup, StringComparison.Ordinal);
        Assert.Contains("Check your connection and try again.", detailsMarkup, StringComparison.Ordinal);
        Assert.DoesNotContain("Error: {ex.Message}", detailsMarkup, StringComparison.Ordinal);
        Assert.Contains(
            "var response = await _httpClient.DeleteAsync($\"{BaseUrl}/{wishlistPublicId}/items/{itemId}\");\n" +
            "        response.EnsureSuccessStatusCode();\n" +
            "        return true;",
            httpClientMarkup,
            StringComparison.Ordinal);
        Assert.Contains("var deletedCount = await context.WishlistItems", serviceMarkup, StringComparison.Ordinal);
        Assert.Contains("i.Id == itemId && !i.Deleted", serviceMarkup, StringComparison.Ordinal);
        Assert.Contains("await context.Database.BeginTransactionAsync()", serviceMarkup, StringComparison.Ordinal);
        Assert.Contains("GetWishlistItemForUpdateAsync(context, wishlistId, itemId)", serviceMarkup, StringComparison.Ordinal);
        Assert.Contains("FOR UPDATE", serviceMarkup, StringComparison.Ordinal);
        Assert.Contains("if (deletedCount > 0 && wishlist != null)", serviceMarkup, StringComparison.Ordinal);
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
        Assert.Contains("disabled=\"@(isLoading || IsSubmitting || string.IsNullOrWhiteSpace(ImportUrl))\"", formMarkup, StringComparison.Ordinal);
        Assert.Contains("role=\"dialog\"", modalMarkup, StringComparison.Ordinal);
        Assert.Contains("aria-modal=\"true\"", modalMarkup, StringComparison.Ordinal);
        Assert.Contains("aria-labelledby=\"wishlist-item-dialog-title\"", modalMarkup, StringComparison.Ordinal);
        Assert.Contains("aria-busy=\"@_isImporting\"", modalMarkup, StringComparison.Ordinal);
        Assert.Contains("for=\"product-url-import-modal\"", modalMarkup, StringComparison.Ordinal);
        Assert.Contains("aria-describedby=\"product-url-import-modal-help\"", modalMarkup, StringComparison.Ordinal);
        Assert.Contains("data-dialog-initial-focus", modalMarkup, StringComparison.Ordinal);
        Assert.Contains("Model.Id > 0 ? \"Save changes\" : \"Add item\"", modalMarkup, StringComparison.Ordinal);
        Assert.Contains("openWishActivateDialog", modalMarkup, StringComparison.Ordinal);
        Assert.Contains("openWishDeactivateDialog", modalMarkup, StringComparison.Ordinal);
        Assert.Contains("data-dialog-background-allowed", modalMarkup, StringComparison.Ordinal);
        Assert.Contains("IAsyncDisposable", modalMarkup, StringComparison.Ordinal);
        Assert.Contains("event.key === \"Escape\"", dialogScript, StringComparison.Ordinal);
        Assert.Contains("event.key !== \"Tab\"", dialogScript, StringComparison.Ordinal);
        Assert.Contains("dialog.querySelector(\"[data-dialog-initial-focus]\")", dialogScript, StringComparison.Ordinal);
        Assert.Contains("existingState?.dialog.isConnected", dialogScript, StringComparison.Ordinal);
        Assert.Contains("sibling.inert = true", dialogScript, StringComparison.Ordinal);
        Assert.Contains("restoreDialogBackground", dialogScript, StringComparison.Ordinal);
        Assert.Contains("state.previouslyFocused?.isConnected", dialogScript, StringComparison.Ordinal);
    }

    [Fact]
    public void WishlistItemDialog_ProtectsSaveAndImportOperations()
    {
        var modalMarkup = ReadComponent("OpenWish.Web.Client", "Components", "Wishlist", "WishlistItemModal.razor");
        var detailsMarkup = ReadComponent("OpenWish.Web.Client", "Components", "Pages", "Wishlists", "WishlistDetails.razor");

        Assert.Contains("if (_isSubmitting || _isImporting || Model is null)", modalMarkup, StringComparison.Ordinal);
        Assert.Contains("aria-busy=\"@_isSubmitting\"", modalMarkup, StringComparison.Ordinal);
        Assert.Contains("disabled=\"@IsBusy\"", modalMarkup, StringComparison.Ordinal);
        Assert.Contains("Saving changes...", modalMarkup, StringComparison.Ordinal);
        Assert.Contains("Adding item...", modalMarkup, StringComparison.Ordinal);
        Assert.Contains("role=\"alert\">@_saveError", modalMarkup, StringComparison.Ordinal);
        Assert.Contains("if (saved)", modalMarkup, StringComparison.Ordinal);
        Assert.Contains("Uri.TryCreate(url.Trim(), UriKind.Absolute", modalMarkup, StringComparison.Ordinal);
        Assert.Contains("string.IsNullOrWhiteSpace(productUri.Host)", modalMarkup, StringComparison.Ordinal);
        Assert.Contains("The product link is ready", modalMarkup, StringComparison.Ordinal);
        Assert.Contains("The link is still here so you can try again.", modalMarkup, StringComparison.Ordinal);
        Assert.Contains("Your existing details were kept.", modalMarkup, StringComparison.Ordinal);
        Assert.DoesNotContain("ImportUrl = string.Empty;\n            _isImporting = false;", modalMarkup, StringComparison.Ordinal);
        Assert.Contains("if (product != null)", modalMarkup, StringComparison.Ordinal);
        Assert.Contains("Product import timed out", modalMarkup, StringComparison.Ordinal);
        Assert.Contains("Model.PublicId = Guid.NewGuid().ToString();", modalMarkup, StringComparison.Ordinal);
        Assert.Contains("_items[existingItemIndex] = savedItem;", detailsMarkup, StringComparison.Ordinal);
        Assert.Contains("_items.RemoveAt(existingItemIndex);", detailsMarkup, StringComparison.Ordinal);
        Assert.Contains("if (ShouldDisplayItem(savedItem))", detailsMarkup, StringComparison.Ordinal);
        Assert.Contains("else if (ShouldDisplayItem(savedItem))", detailsMarkup, StringComparison.Ordinal);
        Assert.Contains("WishlistService.AddItemToWishlistByPublicIdAsync(WishlistId, item)", detailsMarkup, StringComparison.Ordinal);
        Assert.Contains("savedItem.Comments = existingItem.Comments;", detailsMarkup, StringComparison.Ordinal);
        Assert.Contains("savedItem.Reservations = existingItem.Reservations;", detailsMarkup, StringComparison.Ordinal);
        Assert.DoesNotContain("await LoadItems();\n    }\n\n    private void HandleModalCancel", detailsMarkup, StringComparison.Ordinal);
    }

    [Fact]
    public void EventCards_UseExplicitNamedNavigationLinks()
    {
        var markup = ReadComponent("OpenWish.Web.Client", "Components", "Event", "EventCard.razor");

        Assert.Contains("class=\"event-card-link\" href=\"/events/@Event.PublicId\"", markup, StringComparison.Ordinal);
        Assert.Contains("<span class=\"visually-hidden\">: @Event.Name</span>", markup, StringComparison.Ordinal);
        Assert.DoesNotContain("@onclick=\"NavigateToEvent\"", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void WishlistCards_UseExplicitNamedNavigationLinks()
    {
        var markup = ReadComponent("OpenWish.Web.Client", "Components", "Wishlist", "WishlistCard.razor");
        var styles = ReadComponent("OpenWish.Web.Client", "Components", "Wishlist", "WishlistCard.razor.css");

        Assert.Contains("class=\"wishlist-card-link\" href=\"/wishlists/@Wishlist.PublicId\"", markup, StringComparison.Ordinal);
        Assert.Contains("<span class=\"visually-hidden\">: @Wishlist.Name</span>", markup, StringComparison.Ordinal);
        Assert.Contains("<p role=\"status\">Loading wishlist...</p>", markup, StringComparison.Ordinal);
        Assert.DoesNotContain("@onclick=\"NavigateToWishlist\"", markup, StringComparison.Ordinal);
        var linkStyles = styles[
            styles.IndexOf(".wishlist-card-link {", StringComparison.Ordinal)..styles.IndexOf(".wishlist-card-link:hover", StringComparison.Ordinal)];
        Assert.Contains("color: var(--color-link);", linkStyles, StringComparison.Ordinal);
        Assert.Contains("outline: 3px solid var(--color-link-hover);", styles, StringComparison.Ordinal);
    }

    [Fact]
    public void WishlistManagement_LoadFailuresAreActionable()
    {
        var markup = ReadComponent("OpenWish.Web.Client", "Components", "Pages", "Wishlists", "ManageWishlist.razor");

        Assert.Contains("Loading wishlist settings...", markup, StringComparison.Ordinal);
        Assert.Contains("_loadErrorMessage = \"We couldn't load this wishlist's settings. Try again.\"", markup, StringComparison.Ordinal);
        Assert.Contains("@onclick=\"LoadWishlistAsync\"", markup, StringComparison.Ordinal);
        Assert.Contains("Logger.LogError(ex, \"Failed to load wishlist management", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void WishlistAccessLoading_DistinguishesFailureFromAnEmptyList()
    {
        var markup = ReadComponent("OpenWish.Web.Client", "Components", "Pages", "Wishlists", "ManageWishlist.razor");

        Assert.Contains("aria-busy=\"@(_loadingFriendsWithAccess || _loadingAvailableFriends || _sharingBusy ? \"true\" : \"false\")\"", markup, StringComparison.Ordinal);
        Assert.Contains("_friendsLoadErrorMessage = \"We couldn't load who can see this wishlist. Try again.\"", markup, StringComparison.Ordinal);
        Assert.Contains("@onclick=\"RefreshFriendsWithAccessAsync\"", markup, StringComparison.Ordinal);
        Assert.Contains("_availableFriendsLoadErrorMessage = \"We couldn't load friends to share with. Try again.\"", markup, StringComparison.Ordinal);
        Assert.Contains("@onclick=\"RefreshAvailableFriendsAsync\"", markup, StringComparison.Ordinal);
        Assert.Contains("await RefreshAvailableFriendsAsync();", markup, StringComparison.Ordinal);
        Assert.Contains("_availableFriendsToShare.Clear();", markup, StringComparison.Ordinal);
        Assert.Contains("_friendsLoadErrorMessage is null &&", markup, StringComparison.Ordinal);
        Assert.True(
            markup.IndexOf("_friendsWithAccess = (await WishlistService.GetFriendsWithAccessByPublicIdAsync", StringComparison.Ordinal) <
            markup.IndexOf("private async Task RefreshAvailableFriendsAsync()", StringComparison.Ordinal));
        Assert.True(
            markup.IndexOf("_friendsLoadErrorMessage is not null", StringComparison.Ordinal) <
            markup.IndexOf("_friendsWithAccess.Count == 0", StringComparison.Ordinal));
    }

    [Fact]
    public void WishlistAccessActions_AreNamedAndDuplicateSafe()
    {
        var markup = ReadComponent("OpenWish.Web.Client", "Components", "Pages", "Wishlists", "ManageWishlist.razor");
        var styles = ReadComponent("OpenWish.Web.Client", "Components", "Pages", "Wishlists", "ManageWishlist.razor.css");

        Assert.Contains("aria-label=\"Remove access for @friendName\"", markup, StringComparison.Ordinal);
        Assert.Contains("<label class=\"form-label\" for=\"friend-access-select\">Friend to share with</label>", markup, StringComparison.Ordinal);
        Assert.Contains("<h2 class=\"h5 mb-0\">Who can see this?</h2>", markup, StringComparison.Ordinal);
        Assert.Contains("<h2 class=\"h5\">Event connection</h2>", markup, StringComparison.Ordinal);
        Assert.Contains("if (string.IsNullOrEmpty(_selectedFriendToShareId) || _wishlist == null || _sharingBusy)", markup, StringComparison.Ordinal);
        Assert.Contains("if (string.IsNullOrEmpty(friendId) || _wishlist == null || _sharingBusy)", markup, StringComparison.Ordinal);
        Assert.Contains("<span>Sharing...</span>", markup, StringComparison.Ordinal);
        Assert.DoesNotContain("➕", markup, StringComparison.Ordinal);
        Assert.Contains("width: 44px;", styles, StringComparison.Ordinal);
        Assert.Contains("color: var(--color-link);", styles, StringComparison.Ordinal);
        Assert.Contains("color: var(--color-nav-icon);", styles, StringComparison.Ordinal);
    }

    [Fact]
    public void WishlistEventConnections_DistinguishLoadingFailuresFromNoEvents()
    {
        var markup = ReadComponent("OpenWish.Web.Client", "Components", "Pages", "Wishlists", "ManageWishlist.razor");

        Assert.Contains("aria-busy=\"@(_loadingEvents || _eventBusy ? \"true\" : \"false\")\"", markup, StringComparison.Ordinal);
        Assert.Contains("Loading event connections...", markup, StringComparison.Ordinal);
        Assert.Contains("_eventLoadErrorMessage = \"We couldn't load event connections. Try again.\"", markup, StringComparison.Ordinal);
        Assert.Contains("else if (_eventLoadErrorMessage is null && _availableEvents.Any())", markup, StringComparison.Ordinal);
        Assert.Contains("for=\"event-connection-select\"", markup, StringComparison.Ordinal);
        Assert.Contains("if (_eventBusy)", markup, StringComparison.Ordinal);
        Assert.DoesNotContain("_eventErrorMessage = ex.Message", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void EventDeletionFailures_RemainVisibleAndRetryable()
    {
        var markup = ReadComponent("OpenWish.Web.Client", "Components", "Event", "EventCard.razor");
        var indexMarkup = ReadComponent("OpenWish.Web.Client", "Components", "Pages", "Events", "Index.razor");

        Assert.Contains("role=\"alert\"", markup, StringComparison.Ordinal);
        Assert.Contains("_deleteErrorMessage = $\"We couldn't delete", markup, StringComparison.Ordinal);
        Assert.Contains("Sign in again to delete this event.", markup, StringComparison.Ordinal);
        Assert.Contains("Event deleted, but the event list couldn't refresh.", markup, StringComparison.Ordinal);
        Assert.Contains("The deletion outcome couldn't be confirmed.", markup, StringComparison.Ordinal);
        Assert.Contains("IsMissingEvent(reconciliationException)", markup, StringComparison.Ordinal);
        Assert.Contains("else if (!_deleteCompleted)", markup, StringComparison.Ordinal);
        Assert.Contains("if (_deleteCompleted)", markup, StringComparison.Ordinal);
        Assert.DoesNotContain("_deleteCompleted = false;", markup, StringComparison.Ordinal);
        Assert.Contains("Logger.LogError(ex", markup, StringComparison.Ordinal);
        Assert.DoesNotContain("_currentUserId!", markup, StringComparison.Ordinal);
        Assert.Contains("@key=\"evt.PublicId\"", indexMarkup, StringComparison.Ordinal);
    }

    [Fact]
    public void PairingRuleLoading_DistinguishesFailureFromAnEmptyList()
    {
        var markup = ReadComponent("OpenWish.Web.Client", "Components", "Event", "GiftExchangeManager.razor");

        Assert.Contains("class=\"pairing-rules\" aria-busy=", markup, StringComparison.Ordinal);
        Assert.Contains("Loading pairing rules...", markup, StringComparison.Ordinal);
        Assert.Contains("We couldn't load pairing rules. Try again.", markup, StringComparison.Ordinal);
        Assert.Contains("@onclick=\"LoadPairingRules\"", markup, StringComparison.Ordinal);
        Assert.Contains("Logger.LogError(ex, \"Failed to load pairing rules", markup, StringComparison.Ordinal);
        Assert.DoesNotContain("// Ignore errors loading rules", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void PairingRuleChanges_PreventDuplicatesAndAnnounceOutcomes()
    {
        var markup = ReadComponent("OpenWish.Web.Client", "Components", "Event", "GiftExchangeManager.razor");

        Assert.Contains("if (_updatingPairingRules)", markup, StringComparison.Ordinal);
        Assert.True(
            markup.Split("_updatingPairingRules ||", StringSplitOptions.None).Length >= 3,
            "Both pairing-direction paths should reject queued duplicate updates.");
        Assert.Contains("disabled=\"@_updatingPairingRules\"", markup, StringComparison.Ordinal);
        Assert.Contains("role=\"status\" aria-live=\"polite\"", markup, StringComparison.Ordinal);
        Assert.Contains("Exclusion rule added.", markup, StringComparison.Ordinal);
        Assert.Contains("Exclusion rule removed.", markup, StringComparison.Ordinal);
        Assert.Contains("Your change was saved, but the rule list couldn't be refreshed.", markup, StringComparison.Ordinal);
        Assert.Contains("The change outcome couldn't be confirmed.", markup, StringComparison.Ordinal);
        Assert.Contains("ReconcilePairingRuleAddAsync(", markup, StringComparison.Ordinal);
        Assert.Contains("_updatingRuleAction == PairingRuleAction.Toggle", markup, StringComparison.Ordinal);
        Assert.Contains("_updatingRuleAction == PairingRuleAction.Remove", markup, StringComparison.Ordinal);
        Assert.Contains("<span>Updating...</span>", markup, StringComparison.Ordinal);
        Assert.Contains("<span>Removing...</span>", markup, StringComparison.Ordinal);
        Assert.Contains("The exclusion rule couldn't be updated. Try again.", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void GiftExchangeDrawAndReset_LogFailuresWithoutLeakingExceptions()
    {
        var markup = ReadComponent("OpenWish.Web.Client", "Components", "Event", "GiftExchangeManager.razor");

        Assert.Contains("if (Event?.PublicId == null || _drawing || _eventStateChanged)", markup, StringComparison.Ordinal);
        Assert.Contains("if (Event?.PublicId == null || _resetting || _eventStateChanged)", markup, StringComparison.Ordinal);
        Assert.Contains("Names couldn't be drawn. Check the participant list and try again.", markup, StringComparison.Ordinal);
        Assert.Contains("The gift exchange couldn't be reset. Try again.", markup, StringComparison.Ordinal);
        Assert.Contains("Names were drawn, but event details couldn't refresh.", markup, StringComparison.Ordinal);
        Assert.Contains("The gift exchange was reset, but event details couldn't refresh.", markup, StringComparison.Ordinal);
        Assert.Contains("ReconcileGiftExchangeStateAsync(", markup, StringComparison.Ordinal);
        Assert.Contains("ResetEventMutationState();", markup, StringComparison.Ordinal);
        Assert.Contains("disabled=\"@(_drawing || _eventStateChanged)\"", markup, StringComparison.Ordinal);
        Assert.Contains("disabled=\"@(_resetting || _eventStateChanged)\"", markup, StringComparison.Ordinal);
        Assert.True(
            markup.Split("disabled=\"@(_drawing || _eventStateChanged)\"", StringSplitOptions.None).Length >= 3,
            "Drawing and cancel controls should both preserve reload-only recovery.");
        Assert.True(
            markup.Split("disabled=\"@(_resetting || _eventStateChanged)\"", StringSplitOptions.None).Length >= 3,
            "Reset and cancel controls should both preserve reload-only recovery.");
        Assert.Contains("|| _eventStateChanged", markup, StringComparison.Ordinal);
        Assert.DoesNotContain("_drawErrorMessage = ex.Message", markup, StringComparison.Ordinal);
        Assert.DoesNotContain("_resetErrorMessage = ex.Message", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void EventDetails_ReloadsWhenInteractiveRoutingChangesTheEvent()
    {
        var markup = ReadComponent("OpenWish.Web.Client", "Components", "Pages", "Events", "EventDetails.razor");

        Assert.Contains("protected override async Task OnParametersSetAsync()", markup, StringComparison.Ordinal);
        Assert.Contains("_loadedEventId", markup, StringComparison.Ordinal);
        Assert.Contains("loadVersion != _loadVersion", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void EventDetails_ExposeRecoverableLoadingFailures()
    {
        var markup = ReadComponent("OpenWish.Web.Client", "Components", "Pages", "Events", "EventDetails.razor");

        Assert.Contains("Title=\"Event unavailable\"", markup, StringComparison.Ordinal);
        Assert.Contains("We couldn't load this event. Check your connection and try again.", markup, StringComparison.Ordinal);
        Assert.Contains("@onclick=\"RetryLoadAsync\"", markup, StringComparison.Ordinal);
        Assert.Contains("aria-busy=\"@_isLoading\"", markup, StringComparison.Ordinal);
        Assert.Contains("disabled=\"@_isLoading\"", markup, StringComparison.Ordinal);
        Assert.Contains("@(_isLoading ? \"Retrying...\" : \"Try again\")", markup, StringComparison.Ordinal);
        Assert.Contains("Logger.LogError(ex, \"Failed to load event {EventId}\"", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void EventManagement_ExposesRecoverableLoadingFailures()
    {
        var markup = ReadComponent("OpenWish.Web.Client", "Components", "Pages", "Events", "ManageEvent.razor");

        Assert.Contains("Title=\"Event management unavailable\"", markup, StringComparison.Ordinal);
        Assert.Contains("The event itself has not been changed.", markup, StringComparison.Ordinal);
        Assert.Contains("@onclick=\"RetryLoadAsync\"", markup, StringComparison.Ordinal);
        Assert.Contains("var loadVersion = ++_loadVersion;", markup, StringComparison.Ordinal);
        Assert.Contains("loadVersion != _loadVersion", markup, StringComparison.Ordinal);
        Assert.Contains("if (_isLoading)", markup, StringComparison.Ordinal);
        Assert.True(
            markup.IndexOf("NavigationManager.NavigateTo($\"/events/{eventId}\");", StringComparison.Ordinal) >
            markup.IndexOf("catch (Exception ex)", StringComparison.Ordinal));
        Assert.Contains("Logger.LogError(ex, \"Failed to load event management for {EventId}\"", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void EventManagement_PreventsDuplicateSavesAndKeepsFailuresOnTheForm()
    {
        var formMarkup = ReadComponent("OpenWish.Web.Client", "Components", "Event", "EventForm.razor");
        var managementMarkup = ReadComponent("OpenWish.Web.Client", "Components", "Pages", "Events", "ManageEvent.razor");

        Assert.Contains("IsSubmitting=\"@_isSaving\"", managementMarkup, StringComparison.Ordinal);
        Assert.Contains("if (_event == null || _isSaving)", managementMarkup, StringComparison.Ordinal);
        Assert.Contains("var reloaded = await LoadEvent();", managementMarkup, StringComparison.Ordinal);
        Assert.Contains("Event changes saved.", managementMarkup, StringComparison.Ordinal);
        Assert.Contains("We couldn't save the event changes. Review the details and try again.", managementMarkup, StringComparison.Ordinal);
        Assert.Contains("\"Saving changes...\"", formMarkup, StringComparison.Ordinal);

        var controller = ReadComponent("OpenWish.Web", "Controllers", "EventController.cs");
        Assert.Contains("return Ok(updatedEvent);", controller, StringComparison.Ordinal);
    }

    [Fact]
    public void EventManagement_ConfirmsAndReconcilesParticipantRemoval()
    {
        var markup = ReadComponent("OpenWish.Web.Client", "Components", "Pages", "Events", "ManageEvent.razor");

        Assert.Contains("Title=\"Remove participant\"", markup, StringComparison.Ordinal);
        Assert.Contains("DescriptionId=\"remove-event-participant-description\"", markup, StringComparison.Ordinal);
        Assert.Contains("data-dialog-initial-focus", markup, StringComparison.Ordinal);
        Assert.Contains("<span>Removing...</span>", markup, StringComparison.Ordinal);
        Assert.Contains("<span>Reloading event...</span>", markup, StringComparison.Ordinal);
        Assert.Contains("@if (_event?.IsGiftExchange == true)", markup, StringComparison.Ordinal);
        Assert.Contains("We couldn't confirm whether the participant was removed.", markup, StringComparison.Ordinal);
        Assert.Contains("@onclick=\"ReloadParticipantRemovalAsync\"", markup, StringComparison.Ordinal);
        Assert.Contains("CanClose=\"@(!_isRemovingParticipant)\"", markup, StringComparison.Ordinal);
        Assert.Contains("<EventInvitations @key=\"_invitationsVersion\"", markup, StringComparison.Ordinal);
        Assert.True(
            markup.Split("_invitationsVersion++;", StringSplitOptions.None).Length >= 3,
            "Successful direct and reconciled removals should refresh invitation state.");

        var dialogMarkup = ReadComponent("OpenWish.Web.Client", "Components", "Shared", "Dialog.razor");
        Assert.Contains("disabled=\"@(!CanClose)\"", dialogMarkup, StringComparison.Ordinal);
        Assert.Contains("return CanClose ? CloseAsync() : Task.CompletedTask;", dialogMarkup, StringComparison.Ordinal);
    }

    [Fact]
    public void InvitationDecline_RequiresConfirmationAndUsesSafeErrors()
    {
        var markup = ReadComponent("OpenWish.Web.Client", "Components", "Pages", "Events", "AcceptInvite.razor");

        Assert.Contains("Title=\"Decline invitation\"", markup, StringComparison.Ordinal);
        Assert.Contains("DescriptionId=\"decline-invitation-description\"", markup, StringComparison.Ordinal);
        Assert.Contains("data-dialog-initial-focus", markup, StringComparison.Ordinal);
        Assert.Contains("CanClose=\"@(!_isProcessing)\"", markup, StringComparison.Ordinal);
        Assert.Contains("<span>Declining invitation...</span>", markup, StringComparison.Ordinal);
        Assert.Contains("The host will see that you declined", markup, StringComparison.Ordinal);
        Assert.Contains("We couldn't accept the invitation. Check your connection and try again.", markup, StringComparison.Ordinal);
        Assert.DoesNotContain("_errorMessage = ex.Message", markup, StringComparison.Ordinal);
        Assert.DoesNotContain("_rejectErrorMessage = ex.Message", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void EventInvitations_ExposeRecoverableLoadingAndBusyActionStates()
    {
        var markup = ReadComponent("OpenWish.Web.Client", "Components", "Event", "EventInvitations.razor");

        Assert.Contains("class=\"card shadow-sm event-invitations\" aria-busy=", markup, StringComparison.Ordinal);
        Assert.Contains("We couldn't load event invitations. Try again.", markup, StringComparison.Ordinal);
        Assert.Contains("We couldn't load your friends. Try again.", markup, StringComparison.Ordinal);
        Assert.Contains("disabled=\"@IsInvitationActionBusy\"", markup, StringComparison.Ordinal);
        Assert.Contains("RunInvitationActionAsync(", markup, StringComparison.Ordinal);
        Assert.Contains("Func<string, Task<bool>> operation", markup, StringComparison.Ordinal);
        Assert.Contains("if (!succeeded)", markup, StringComparison.Ordinal);
        Assert.True(
            markup.IndexOf("_inlineSuccessMessage = successMessage;", StringComparison.Ordinal) <
            markup.IndexOf("await LoadInvitations();", markup.IndexOf("_inlineSuccessMessage = successMessage;", StringComparison.Ordinal), StringComparison.Ordinal));
        Assert.Contains("role=\"status\" aria-live=\"polite\"", markup, StringComparison.Ordinal);
        Assert.DoesNotContain("_inlineErrorMessage = ex.Message", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void ReservedItems_AnnounceRefreshesErrorsAndNewTabDestinations()
    {
        var markup = ReadComponent("OpenWish.Web.Client", "Components", "Event", "EventReservedItems.razor");

        Assert.Contains("aria-busy=\"@_isLoading\"", markup, StringComparison.Ordinal);
        Assert.Contains("id=\"reserved-items-status\"", markup, StringComparison.Ordinal);
        Assert.Contains("Reserved items refreshed.", markup, StringComparison.Ordinal);
        Assert.Contains("role=\"alert\">@_errorMessage", markup, StringComparison.Ordinal);
        Assert.Contains("aria-label=\"Open @item.ItemName product in a new tab\"", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void FriendRequests_NameActionsAndAnnounceTheirOutcomes()
    {
        var markup = ReadComponent("OpenWish.Web.Client", "Components", "Social", "FriendRequestList.razor");

        Assert.Contains("aria-label=\"Accept friend request from @GetRequesterName(request)\"", markup, StringComparison.Ordinal);
        Assert.Contains("aria-label=\"Reject friend request from @GetRequesterName(request)\"", markup, StringComparison.Ordinal);
        Assert.Contains("aria-label=\"Resend friend request to @GetReceiverName(request)\"", markup, StringComparison.Ordinal);
        Assert.Contains("disabled=\"@_processingRequestId.HasValue\"", markup, StringComparison.Ordinal);
        Assert.Contains("role=\"status\" aria-live=\"polite\"", markup, StringComparison.Ordinal);
        Assert.Contains("role=\"alert\"", markup, StringComparison.Ordinal);
        Assert.Contains("await OnFriendshipsChanged.InvokeAsync();", markup, StringComparison.Ordinal);
        Assert.Contains("else if (!string.IsNullOrWhiteSpace(_loadErrorMessage))", markup, StringComparison.Ordinal);
        Assert.Contains("Friend requests could not be loaded. Try again.", markup, StringComparison.Ordinal);
        Assert.Contains("@onclick=\"RetryLoadRequests\"", markup, StringComparison.Ordinal);
        Assert.Contains("private async Task RetryLoadRequests()\n    {\n        if (_loading)", markup, StringComparison.Ordinal);
        Assert.Contains("disabled=\"@_loading\"", markup, StringComparison.Ordinal);
        Assert.Contains("string.IsNullOrWhiteSpace(_loadErrorMessage) && _sentRequests.Any()", markup, StringComparison.Ordinal);
        Assert.True(
            markup.IndexOf("var sentRequests = await", StringComparison.Ordinal) <
            markup.IndexOf("_receivedRequests = receivedRequests.ToList();", StringComparison.Ordinal));
    }

    [Fact]
    public void CommentDeletion_RequiresAFocusSafeConfirmation()
    {
        var markup = ReadComponent("OpenWish.Web.Client", "Components", "Wishlist", "ItemComments.razor");

        Assert.Contains("role=\"alertdialog\"", markup, StringComparison.Ordinal);
        Assert.Contains("aria-labelledby=\"delete-comment-title-@comment.Id\"", markup, StringComparison.Ordinal);
        Assert.Contains("aria-describedby=\"delete-comment-description-@comment.Id\"", markup, StringComparison.Ordinal);
        Assert.Contains("@ref=\"_keepCommentButton\"", markup, StringComparison.Ordinal);
        Assert.Contains("await _keepCommentButton.FocusAsync();", markup, StringComparison.Ordinal);
        Assert.Contains("openWishFocusElement\", elementId", markup, StringComparison.Ordinal);
        Assert.Contains("await _commentComposer.FocusAsync();", markup, StringComparison.Ordinal);
        Assert.Contains("var removed = await WishlistService.RemoveItemCommentAsync", markup, StringComparison.Ordinal);
        Assert.Contains("await _errorAlert.FocusAsync();", markup, StringComparison.Ordinal);
        Assert.Contains("Comment deleted.", markup, StringComparison.Ordinal);
        Assert.Contains("id=\"commentText-@ItemId\"", markup, StringComparison.Ordinal);
        Assert.Contains("for=\"commentText-@ItemId\"", markup, StringComparison.Ordinal);
        Assert.DoesNotContain("id=\"commentText\"", markup, StringComparison.Ordinal);
        Assert.Contains("Comments could not be loaded. Try again.", markup, StringComparison.Ordinal);
        Assert.Contains("@onclick=\"RetryLoadComments\"", markup, StringComparison.Ordinal);
        Assert.Contains("private async Task<bool> LoadComments()", markup, StringComparison.Ordinal);
        Assert.Contains("private async Task RetryLoadComments()\n    {\n        if (_loading)", markup, StringComparison.Ordinal);
        Assert.Contains("disabled=\"@_loading\"", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void ReservationCancellation_RequiresAFocusSafeConfirmation()
    {
        var markup = ReadComponent("OpenWish.Web.Client", "Components", "Wishlist", "ItemReservation.razor");
        var client = ReadComponent("OpenWish.Web.Client", "Services", "WishlistHttpClientService.cs");

        Assert.Contains("role=\"alertdialog\"", markup, StringComparison.Ordinal);
        Assert.Contains("aria-labelledby=\"cancel-reservation-title-@ItemId\"", markup, StringComparison.Ordinal);
        Assert.Contains("aria-describedby=\"cancel-reservation-description-@ItemId\"", markup, StringComparison.Ordinal);
        Assert.Contains("@ref=\"_keepReservationButton\"", markup, StringComparison.Ordinal);
        Assert.Contains("await _keepReservationButton.FocusAsync();", markup, StringComparison.Ordinal);
        Assert.Contains("await _cancelReservationButton.FocusAsync();", markup, StringComparison.Ordinal);
        Assert.Contains("await _reserveItemButton.FocusAsync();", markup, StringComparison.Ordinal);
        Assert.Contains("var canceled = await WishlistService.CancelReservationByPublicIdAsync", markup, StringComparison.Ordinal);
        Assert.Contains("await _errorAlert.FocusAsync();", markup, StringComparison.Ordinal);
        Assert.Contains("Reservation released.", markup, StringComparison.Ordinal);
        Assert.Contains("Sign in again to release this reservation.", markup, StringComparison.Ordinal);
        Assert.Contains("Reservation status could not be loaded. Try again.", markup, StringComparison.Ordinal);
        Assert.Contains("@onclick=\"RetryReservation\"", markup, StringComparison.Ordinal);
        Assert.Contains("else if (!string.IsNullOrWhiteSpace(_loadErrorMessage))", markup, StringComparison.Ordinal);
        Assert.Contains("private async Task RetryReservation()\n    {\n        if (_loading)", markup, StringComparison.Ordinal);
        Assert.Contains("disabled=\"@_loading\"", markup, StringComparison.Ordinal);
        Assert.Contains("PostAsJsonAsync($\"{BaseUrl}/{wishlistPublicId}/items/{itemId}/reserve\"", client, StringComparison.Ordinal);
        Assert.Contains("DeleteAsync($\"{BaseUrl}/{wishlistPublicId}/items/{itemId}/reservation\"", client, StringComparison.Ordinal);
        var reservationMethods = client[
            client.IndexOf("public async Task<bool> ReserveItemByPublicIdAsync", StringComparison.Ordinal)..client.IndexOf("public async Task<ItemReservationModel?> GetItemReservationByPublicIdAsync", StringComparison.Ordinal)];
        Assert.Equal(2, reservationMethods.Split(
            "return await response.Content.ReadFromJsonAsync<bool>();",
            StringSplitOptions.None).Length - 1);
    }

    [Fact]
    public void SharedDialog_ContainsFocusAndRestoresItToTheOpener()
    {
        var markup = ReadComponent("OpenWish.Web.Client", "Components", "Shared", "Dialog.razor");

        Assert.Contains("role=\"dialog\"", markup, StringComparison.Ordinal);
        Assert.Contains("aria-modal=\"true\"", markup, StringComparison.Ordinal);
        Assert.Contains("aria-labelledby=\"@_titleId\"", markup, StringComparison.Ordinal);
        Assert.Contains("data-dialog-close", markup, StringComparison.Ordinal);
        Assert.Contains("openWishActivateDialog\", _dialogId, ReturnFocusElementId, true", markup, StringComparison.Ordinal);
        Assert.Contains("openWishDeactivateDialog\", _dialogId", markup, StringComparison.Ordinal);
        Assert.Contains("catch (JSDisconnectedException)", markup, StringComparison.Ordinal);
        Assert.True(
            markup.IndexOf("_isOpen = false;", StringComparison.Ordinal) <
            markup.IndexOf("openWishDeactivateDialog\", _dialogId", StringComparison.Ordinal));
        Assert.Contains("IAsyncDisposable", markup, StringComparison.Ordinal);

        var eventCardMarkup = ReadComponent("OpenWish.Web.Client", "Components", "Event", "EventCard.razor");
        var dialogScript = ReadComponent("OpenWish.Web", "wwwroot", "app.js");
        Assert.Contains("data-dialog-initial-focus", eventCardMarkup, StringComparison.Ordinal);
        Assert.Contains("ReturnFocusElementId=\"@($\"dropdownMenu{Event?.Id}\")\"", eventCardMarkup, StringComparison.Ordinal);
        Assert.Contains("await _cancelDeleteButton.FocusAsync();", eventCardMarkup, StringComparison.Ordinal);
        Assert.Contains("state.previouslyFocused?.isConnected", dialogScript, StringComparison.Ordinal);
        Assert.Contains("document.querySelector(\"main h1, main h2, main h3, main\")", dialogScript, StringComparison.Ordinal);
        Assert.Contains("new MutationObserver", dialogScript, StringComparison.Ordinal);
    }

    [Fact]
    public void WishlistVisibility_UsesNativeExclusiveChoices()
    {
        var markup = ReadComponent("OpenWish.Web.Client", "Components", "Wishlist", "WishlistForm.razor");
        var styles = ReadComponent("OpenWish.Web.Client", "Components", "Wishlist", "WishlistForm.razor.css");

        Assert.Equal(3, markup.Split(
            "type=\"radio\" name=\"wishlist-visibility\"",
            StringSplitOptions.None).Length - 1);
        Assert.Contains("checked=\"@(!Model.IsPrivate && !Model.IsFriendsOnly)\"", markup, StringComparison.Ordinal);
        Assert.Contains("@onchange=\"@(() => SetVisibility(true, false))\"", markup, StringComparison.Ordinal);
        Assert.DoesNotContain("role=\"radio\"", markup, StringComparison.Ordinal);
        Assert.DoesNotContain("HandleVisibilityKeyDown", markup, StringComparison.Ordinal);
        Assert.Contains(".visibility-option:focus-within", styles, StringComparison.Ordinal);
        Assert.DoesNotContain(".visibility-option:has(", styles, StringComparison.Ordinal);
    }

    [Fact]
    public void WishlistCreation_PreventsDuplicatesAndKeepsFailuresVisible()
    {
        var pageMarkup = ReadComponent("OpenWish.Web.Client", "Components", "Pages", "Wishlists", "NewWishlist.razor");
        var formMarkup = ReadComponent("OpenWish.Web.Client", "Components", "Wishlist", "WishlistForm.razor");

        Assert.Contains("if (_isSubmitting)", pageMarkup, StringComparison.Ordinal);
        Assert.Contains("role=\"alert\">@_errorMessage", pageMarkup, StringComparison.Ordinal);
        Assert.Contains("We couldn't create the wishlist.", pageMarkup, StringComparison.Ordinal);
        Assert.Contains("await Task.Yield();", pageMarkup, StringComparison.Ordinal);
        Assert.Contains("disabled=\"@IsSubmitting\"", formMarkup, StringComparison.Ordinal);
        Assert.Contains("Creating wishlist...", formMarkup, StringComparison.Ordinal);
        Assert.Contains("aria-busy=\"@IsSubmitting\"", formMarkup, StringComparison.Ordinal);

        var eventManagerMarkup = ReadComponent("OpenWish.Web.Client", "Components", "Event", "EventWishlistManager.razor");
        Assert.Contains("IsSubmitting=\"@_isSubmitting\"", eventManagerMarkup, StringComparison.Ordinal);
        Assert.Contains("var createdSuccessfully = false;", eventManagerMarkup, StringComparison.Ordinal);
        Assert.Contains("if (createdSuccessfully)", eventManagerMarkup, StringComparison.Ordinal);
    }

    [Fact]
    public void StandaloneItemCreation_PreventsDuplicatesAndKeepsFailuresVisible()
    {
        var pageMarkup = ReadComponent("OpenWish.Web.Client", "Components", "Pages", "Wishlists", "AddItem.razor");
        var formMarkup = ReadComponent("OpenWish.Web.Client", "Components", "Wishlist", "WishlistItemForm.razor");

        Assert.Contains("if (_isSubmitting)", pageMarkup, StringComparison.Ordinal);
        Assert.Contains("role=\"alert\">@_errorMessage", pageMarkup, StringComparison.Ordinal);
        Assert.Contains("We couldn't add this item.", pageMarkup, StringComparison.Ordinal);
        Assert.Contains("await Task.Yield();", pageMarkup, StringComparison.Ordinal);
        Assert.Contains("new() { PublicId = Guid.NewGuid().ToString() }", pageMarkup, StringComparison.Ordinal);
        Assert.Contains("disabled=\"@IsSubmitting\"", formMarkup, StringComparison.Ordinal);
        Assert.Contains("class=\"btn btn-outline-secondary\" disabled=\"@IsSubmitting\"", formMarkup, StringComparison.Ordinal);
        var cancelHandler = pageMarkup[pageMarkup.IndexOf("private void HandleCancel()", StringComparison.Ordinal)..];
        Assert.Contains("if (_isSubmitting)", cancelHandler, StringComparison.Ordinal);
        Assert.True(
            cancelHandler.IndexOf("if (_isSubmitting)", StringComparison.Ordinal) <
            cancelHandler.IndexOf("NavigationManager.NavigateTo", StringComparison.Ordinal));
        Assert.Contains("Adding item...", formMarkup, StringComparison.Ordinal);
        Assert.Contains("aria-busy=\"@IsSubmitting\"", formMarkup, StringComparison.Ordinal);
        Assert.DoesNotContain("<EditForm Enhance", formMarkup, StringComparison.Ordinal);
    }

    [Fact]
    public void GiftExchangeDisplay_DistinguishesLoadingFailuresFromNoAssignment()
    {
        var markup = ReadComponent("OpenWish.Web.Client", "Components", "Event", "GiftExchangeDisplay.razor");

        Assert.Contains("aria-busy=\"@_loading\"", markup, StringComparison.Ordinal);
        Assert.Contains("aria-labelledby=\"gift-match-title\"", markup, StringComparison.Ordinal);
        Assert.Contains("Loading your match...", markup, StringComparison.Ordinal);
        Assert.Contains("role=\"alert\"", markup, StringComparison.Ordinal);
        Assert.Contains("We couldn't load your gift exchange match. Try again.", markup, StringComparison.Ordinal);
        Assert.Contains("@onclick=\"LoadGiftExchange\"", markup, StringComparison.Ordinal);
        Assert.Contains("Your assignment isn't available yet.", markup, StringComparison.Ordinal);
        Assert.Contains("Logger.LogError(ex", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void GiftExchangeDisplay_ShowsTheNextStepAndDistinguishesMissingIdeas()
    {
        var markup = ReadComponent("OpenWish.Web.Client", "Components", "Event", "GiftExchangeDisplay.razor");
        var styles = ReadComponent("OpenWish.Web.Client", "Components", "Event", "GiftExchangeDisplay.razor.css");
        var page = ReadComponent("OpenWish.Web.Client", "Components", "Pages", "Events", "EventDetails.razor");

        Assert.Contains("You're shopping for", markup, StringComparison.Ordinal);
        Assert.Contains("<dt>Suggested budget</dt>", markup, StringComparison.Ordinal);
        Assert.Contains("<dt>Exchange date</dt>", markup, StringComparison.Ordinal);
        Assert.Contains("?? \"Not set\"", markup, StringComparison.Ordinal);
        Assert.Contains("Checking shared gift ideas...", markup, StringComparison.Ordinal);
        Assert.Contains("!WishlistsLoaded", markup, StringComparison.Ordinal);
        Assert.Contains("_recipientWishlist != null && RecipientItemCount > 0", markup, StringComparison.Ordinal);
        Assert.Contains("View @RecipientDisplayName's wishlist", markup, StringComparison.Ordinal);
        Assert.Contains("hasn't added gift ideas to their wishlist yet.", markup, StringComparison.Ordinal);
        Assert.Contains("hasn't shared a wishlist for this exchange yet.", markup, StringComparison.Ordinal);
        Assert.Contains("OrderByDescending(GetItemCount)", markup, StringComparison.Ordinal);
        Assert.Contains("Your match stays private.", markup, StringComparison.Ordinal);
        Assert.Contains("overflow-wrap: anywhere;", styles, StringComparison.Ordinal);
        Assert.Contains("min-height: 44px;", styles, StringComparison.Ordinal);
        Assert.Contains("ExchangeDate=\"@_event.Date\"", page, StringComparison.Ordinal);
        Assert.Contains("WishlistsLoaded=\"@_wishlistsLoaded\"", page, StringComparison.Ordinal);
        Assert.Contains("event-header-match", page, StringComparison.Ordinal);
    }

    [Fact]
    public void MobileWishlistAddAction_IsInFlowAndVisiblyLabelled()
    {
        var markup = ReadComponent("OpenWish.Web.Client", "Components", "Pages", "Wishlists", "WishlistDetails.razor");
        var styles = ReadComponent("OpenWish.Web.Client", "Components", "Pages", "Wishlists", "WishlistDetails.razor.css");

        Assert.Contains("id=\"add-wishlist-item\"", markup, StringComparison.Ordinal);
        Assert.Contains("<span class=\"fab-label\">Add item</span>", markup, StringComparison.Ordinal);
        Assert.True(
            markup.IndexOf("id=\"add-wishlist-item\"", StringComparison.Ordinal) <
            markup.IndexOf("id=\"wishlist-items\"", StringComparison.Ordinal));
        Assert.Contains(".fab-label", styles, StringComparison.Ordinal);
        Assert.Contains("position: static;", styles, StringComparison.Ordinal);
    }

    [Fact]
    public void WishlistItemActions_HaveNamesAndMobileTouchTargets()
    {
        var pageMarkup = ReadComponent("OpenWish.Web.Client", "Components", "Pages", "Wishlists", "WishlistDetails.razor");
        var pageStyles = ReadComponent("OpenWish.Web.Client", "Components", "Pages", "Wishlists", "WishlistDetails.razor.css");
        var listMarkup = ReadComponent("OpenWish.Web.Client", "Components", "Wishlist", "WishlistItemList.razor");
        var listStyles = ReadComponent("OpenWish.Web.Client", "Components", "Wishlist", "WishlistItemList.razor.css");

        Assert.Contains("aria-label=\"Edit @item.Name\"", pageMarkup, StringComparison.Ordinal);
        Assert.Contains("aria-label=\"Delete @item.Name\"", pageMarkup, StringComparison.Ordinal);
        Assert.Contains("aria-expanded=\"@(_expandedItemId == item.Id)\"", pageMarkup, StringComparison.Ordinal);
        Assert.Contains("aria-controls=\"item-details-@item.Id\"", pageMarkup, StringComparison.Ordinal);
        Assert.Contains("id=\"item-details-@item.Id\"", pageMarkup, StringComparison.Ordinal);
        Assert.Contains("aria-label=\"Edit @item.Name\"", listMarkup, StringComparison.Ordinal);
        Assert.Contains("aria-label=\"Delete @item.Name\"", listMarkup, StringComparison.Ordinal);
        Assert.Contains("min-width: 2.75rem;", pageStyles, StringComparison.Ordinal);
        Assert.Contains(".edit-actions .btn", listStyles, StringComparison.Ordinal);
        Assert.Contains("min-height: 2.75rem;", listStyles, StringComparison.Ordinal);
    }

    [Fact]
    public void WishlistFilterAndViewControls_HaveMobileTouchTargets()
    {
        var styles = ReadComponent("OpenWish.Web.Client", "Components", "Pages", "Wishlists", "WishlistDetails.razor.css");

        Assert.Contains(".search-clear", styles, StringComparison.Ordinal);
        Assert.Contains(".view-toggle .btn", styles, StringComparison.Ordinal);
        Assert.Contains(".filter-chip", styles, StringComparison.Ordinal);
        Assert.Contains(".sort-option", styles, StringComparison.Ordinal);
        Assert.Contains("min-height: 2.75rem;", styles, StringComparison.Ordinal);
        Assert.Contains("flex: 1;", styles, StringComparison.Ordinal);
    }

    [Fact]
    public void SecretSantaSetupActions_HaveMobileTouchTargets()
    {
        var styles = ReadComponent("OpenWish.Web.Client", "Components", "Event", "SetupStep.razor.css");

        Assert.Contains(".setup-step .btn", styles, StringComparison.Ordinal);
        Assert.Contains("min-height: 2.75rem;", styles, StringComparison.Ordinal);
        Assert.Contains("grid-column: 1 / -1;", styles, StringComparison.Ordinal);
        Assert.Contains("width: 100%;", styles, StringComparison.Ordinal);
    }

    [Fact]
    public void InlineConfirmations_HaveMobileTouchTargets()
    {
        var commentStyles = ReadComponent(
            "OpenWish.Web.Client", "Components", "Wishlist", "ItemComments.razor.css");
        var reservationStyles = ReadComponent(
            "OpenWish.Web.Client", "Components", "Wishlist", "ItemReservation.razor.css");

        Assert.Contains(".item-comments .btn", commentStyles, StringComparison.Ordinal);
        Assert.Contains("min-width: 2.75rem;", commentStyles, StringComparison.Ordinal);
        Assert.Contains("min-height: 2.75rem;", commentStyles, StringComparison.Ordinal);
        Assert.Contains(".item-reservation .btn", reservationStyles, StringComparison.Ordinal);
        Assert.Contains("min-height: 2.75rem;", reservationStyles, StringComparison.Ordinal);
    }

    [Fact]
    public void PersonalWishlistFailures_DoNotAppearAsEmptyResults()
    {
        var markup = ReadComponent("OpenWish.Web.Client", "Components", "Pages", "Wishlists", "Index.razor");

        Assert.Contains("aria-busy=\"@(_loadingWishlists ? \"true\" : \"false\")\"", markup, StringComparison.Ordinal);
        Assert.Contains("else if (_loadingWishlists)", markup, StringComparison.Ordinal);
        Assert.Contains("if (firstRender && _loadingWishlists)", markup, StringComparison.Ordinal);
        Assert.Contains("@if (!string.IsNullOrWhiteSpace(_wishlistLoadError))", markup, StringComparison.Ordinal);
        Assert.Contains("Your wishlists could not be loaded. Refresh the page to try again.", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void FriendsWishlistFailures_DoNotAppearAsEmptyResults()
    {
        var markup = ReadComponent("OpenWish.Web.Client", "Components", "Pages", "Wishlists", "Index.razor");

        Assert.Contains("aria-busy=\"@(loadingFriendsWishlists ? \"true\" : \"false\")\"", markup, StringComparison.Ordinal);
        Assert.Contains("else if (!string.IsNullOrWhiteSpace(_friendsWishlistLoadError))", markup, StringComparison.Ordinal);
        Assert.Contains("Friends' wishlists could not be loaded. Refresh the page to try again.", markup, StringComparison.Ordinal);
        Assert.Contains("Sign in again to load friends' wishlists.", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void FriendInvitations_PreventDuplicateSubmissionsAndExposeBusyState()
    {
        var markup = ReadComponent("OpenWish.Web.Client", "Components", "Social", "FriendSearch.razor");

        Assert.Contains("aria-busy=\"@(_sendingInvites ? \"true\" : \"false\")\"", markup, StringComparison.Ordinal);
        Assert.Contains("if (_sendingInvites)", markup, StringComparison.Ordinal);
        Assert.Contains("type=\"button\" @onclick=\"SendEmailInvites\"", markup, StringComparison.Ordinal);
        Assert.Contains("Sign in again to send friend invitations.", markup, StringComparison.Ordinal);
        Assert.Contains(
            "catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Unauthorized)",
            markup,
            StringComparison.Ordinal);
        Assert.Contains("Logger.LogError(ex, \"Failed to send friend invitations.\")", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void FriendList_ProvidesRecoverableNamedActionsAndFeedback()
    {
        var listMarkup = ReadComponent("OpenWish.Web.Client", "Components", "Social", "FriendList.razor");
        var dialogMarkup = ReadComponent(
            "OpenWish.Web.Client", "Components", "Social", "FriendRemoveConfirmationDialog.razor");
        var styles = ReadComponent("OpenWish.Web.Client", "Components", "Social", "FriendList.razor.css");

        Assert.Contains("aria-busy=\"@(_loading ? \"true\" : \"false\")\"", listMarkup, StringComparison.Ordinal);
        Assert.Contains("@onclick=\"LoadFriends\"", listMarkup, StringComparison.Ordinal);
        Assert.Contains("aria-label=\"Remove @GetFriendName(friend) from friends\"", listMarkup, StringComparison.Ordinal);
        Assert.Contains("role=\"status\" aria-live=\"polite\"", listMarkup, StringComparison.Ordinal);
        Assert.Contains("DescriptionId=\"friend-removal-description\"", dialogMarkup, StringComparison.Ordinal);
        Assert.Contains("Keep friend", dialogMarkup, StringComparison.Ordinal);
        Assert.Contains("Remove friend", dialogMarkup, StringComparison.Ordinal);
        Assert.Contains("if (_isRemoving)", dialogMarkup, StringComparison.Ordinal);
        Assert.Equal(2, dialogMarkup.Split("disabled=\"@_isRemoving\"", StringSplitOptions.None).Length - 1);
        Assert.Contains("Wishlists shared directly may remain available.", listMarkup, StringComparison.Ordinal);
        Assert.True(
            listMarkup.IndexOf("_statusMessage = $\"{friendName} removed", StringComparison.Ordinal) <
            listMarkup.IndexOf(
                "await LoadFriends();",
                listMarkup.IndexOf("private async Task ConfirmRemoveFriend", StringComparison.Ordinal),
                StringComparison.Ordinal));
        Assert.Contains("min-height: 2.75rem;", styles, StringComparison.Ordinal);
    }

    [Fact]
    public void Dashboard_ExposesPersonalizedContentLoadingState()
    {
        var markup = ReadComponent("OpenWish.Web", "Components", "Pages", "Home.razor");

        Assert.Contains("@attribute [StreamRendering]", markup, StringComparison.Ordinal);
        Assert.Contains(
            "class=\"dashboard-content\" aria-busy=\"@(_isLoading ? \"true\" : \"false\")\"",
            markup,
            StringComparison.Ordinal);
        Assert.Contains("Loading your dashboard...", markup, StringComparison.Ordinal);
        Assert.Contains("else", markup, StringComparison.Ordinal);
        Assert.Contains("finally", markup, StringComparison.Ordinal);
        Assert.Contains("_isLoading = false;", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void AccountNavigation_IsLabelledAndResponsive()
    {
        var markup = ReadComponent("OpenWish.Web", "Components", "Account", "Shared", "ManageNavMenu.razor");
        var styles = ReadComponent("OpenWish.Web", "Components", "Account", "Shared", "ManageLayout.razor.css");

        Assert.Contains("<nav aria-label=\"Account settings\">", markup, StringComparison.Ordinal);
        Assert.Contains("overflow-x: auto;", styles, StringComparison.Ordinal);
        Assert.Contains("flex-flow: row nowrap !important;", styles, StringComparison.Ordinal);
        Assert.Contains("min-height: 2.75rem;", styles, StringComparison.Ordinal);
    }

    [Fact]
    public void Profile_ExplainsReadOnlyAndOptionalFields()
    {
        var markup = ReadComponent("OpenWish.Web", "Components", "Account", "Pages", "Manage", "Index.razor");

        Assert.Contains("aria-describedby=\"username-help\" readonly", markup, StringComparison.Ordinal);
        Assert.Contains("autocomplete=\"tel\" inputmode=\"tel\" aria-describedby=\"phone-help\"", markup, StringComparison.Ordinal);
        Assert.Contains("cannot be edited from your profile", markup, StringComparison.Ordinal);
        Assert.Contains("not shown on wishlists or events", markup, StringComparison.Ordinal);
        Assert.Contains(">Save profile</button>", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void EmailSettings_ExposeConfirmationStatusAndChangeGuidance()
    {
        var markup = ReadComponent("OpenWish.Web", "Components", "Account", "Pages", "Manage", "Email.razor");

        Assert.Contains("aria-describedby=\"email-status\" readonly", markup, StringComparison.Ordinal);
        Assert.Contains("<span>Confirmed</span>", markup, StringComparison.Ordinal);
        Assert.Contains("Confirmation needed", markup, StringComparison.Ordinal);
        Assert.Contains("Send confirmation email", markup, StringComparison.Ordinal);
        Assert.Contains("aria-describedby=\"new-email-help\"", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void PasswordSettings_ConnectPasswordRequirements()
    {
        var markup = ReadComponent("OpenWish.Web", "Components", "Account", "Pages", "Manage", "ChangePassword.razor");
        var setPasswordMarkup = ReadComponent("OpenWish.Web", "Components", "Account", "Pages", "Manage", "SetPassword.razor");

        Assert.Contains("id=\"password-guidance\"", markup, StringComparison.Ordinal);
        Assert.Contains("6 to 100 characters", markup, StringComparison.Ordinal);
        Assert.Contains("uppercase letter, lowercase letter, number, and symbol", markup, StringComparison.Ordinal);
        Assert.Contains("aria-describedby=\"password-guidance\"", markup, StringComparison.Ordinal);
        Assert.Contains("autocomplete=\"current-password\"", markup, StringComparison.Ordinal);
        Assert.Contains("autocomplete=\"new-password\"", markup, StringComparison.Ordinal);
        Assert.Contains("id=\"password-guidance\"", setPasswordMarkup, StringComparison.Ordinal);
        Assert.Contains("6 to 100 characters", setPasswordMarkup, StringComparison.Ordinal);
        Assert.Contains("uppercase letter, lowercase letter, number, and symbol", setPasswordMarkup, StringComparison.Ordinal);
        Assert.Contains("aria-describedby=\"password-guidance\"", setPasswordMarkup, StringComparison.Ordinal);
    }

    [Fact]
    public void PersonalDataActions_NameTheirOutcomeAndKeepDeletionReversible()
    {
        var markup = ReadComponent("OpenWish.Web", "Components", "Account", "Pages", "Manage", "PersonalData.razor");
        var deletionMarkup = ReadComponent("OpenWish.Web", "Components", "Account", "Pages", "Manage", "DeletePersonalData.razor");

        Assert.Contains("Download personal data", markup, StringComparison.Ordinal);
        Assert.Contains("Review account deletion", markup, StringComparison.Ordinal);
        Assert.Contains("cannot be undone", markup, StringComparison.Ordinal);
        Assert.Contains("Keep my account", deletionMarkup, StringComparison.Ordinal);
        Assert.Contains("Delete my account", deletionMarkup, StringComparison.Ordinal);
        Assert.Contains("d-flex flex-column flex-sm-row gap-2", deletionMarkup, StringComparison.Ordinal);
        Assert.DoesNotContain("flex-column-reverse", deletionMarkup, StringComparison.Ordinal);
    }

    [Fact]
    public void TwoFactorSettings_ExposeStatusAndPrioritizeTheNextAction()
    {
        var markup = ReadComponent("OpenWish.Web", "Components", "Account", "Pages", "Manage", "TwoFactorAuthentication.razor");

        Assert.Contains("Two-factor authentication is on.", markup, StringComparison.Ordinal);
        Assert.Contains("Two-factor authentication is off.", markup, StringComparison.Ordinal);
        Assert.Contains("role=\"status\"", markup, StringComparison.Ordinal);
        Assert.Contains(">Set up authenticator app</a>", markup, StringComparison.Ordinal);
        Assert.Contains("An authenticator key is available.", markup, StringComparison.Ordinal);
        Assert.Contains(">Verify authenticator code</a>", markup, StringComparison.Ordinal);
        Assert.Contains("Your authenticator app is connected.", markup, StringComparison.Ordinal);
        Assert.DoesNotContain("Finish authenticator setup", markup, StringComparison.Ordinal);
        Assert.Contains(">Replace recovery codes</a>", markup, StringComparison.Ordinal);
        Assert.Contains(">Turn off 2FA</a>", markup, StringComparison.Ordinal);
        Assert.DoesNotContain("generate a new set of recovery codes", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void AuthenticatorSetup_ProvidesAUsableManualKeyAndCodeInput()
    {
        var markup = ReadComponent("OpenWish.Web", "Components", "Account", "Pages", "Manage", "EnableAuthenticator.razor");

        Assert.Contains("id=\"shared-key\"", markup, StringComparison.Ordinal);
        Assert.Contains("supports time-based one-time passwords (TOTP)", markup, StringComparison.Ordinal);
        Assert.DoesNotContain("Scan the QR Code", markup, StringComparison.Ordinal);
        Assert.Contains("autocomplete=\"one-time-code\" inputmode=\"numeric\" maxlength=\"11\"", markup, StringComparison.Ordinal);
        Assert.Contains("aria-describedby=\"verification-code-help\"", markup, StringComparison.Ordinal);
        Assert.Contains(">Verify and enable 2FA</button>", markup, StringComparison.Ordinal);
        Assert.Contains("InputModel : IValidatableObject", markup, StringComparison.Ordinal);
        Assert.Contains("normalizedCode.Length != 6", markup, StringComparison.Ordinal);
        Assert.Contains("character is < '0' or > '9'", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void RecoveryCodeReplacement_ExplainsInvalidationAndOffersSafeExit()
    {
        var markup = ReadComponent("OpenWish.Web", "Components", "Account", "Pages", "Manage", "GenerateRecoveryCodes.razor");
        var codesMarkup = ReadComponent("OpenWish.Web", "Components", "Account", "Shared", "ShowRecoveryCodes.razor");

        Assert.Contains("current recovery codes will stop working immediately", markup, StringComparison.Ordinal);
        Assert.Contains(">Keep current recovery codes</a>", markup, StringComparison.Ordinal);
        Assert.Contains(">Replace recovery codes</button>", markup, StringComparison.Ordinal);
        Assert.Contains("aria-label=\"Recovery codes\"", codesMarkup, StringComparison.Ordinal);
        Assert.Contains("They will not be shown again.", codesMarkup, StringComparison.Ordinal);
        Assert.Contains(">Done saving codes</a>", codesMarkup, StringComparison.Ordinal);
    }

    [Fact]
    public void AuthenticatorReset_ExplainsImpactAndOffersSafeExit()
    {
        var markup = ReadComponent("OpenWish.Web", "Components", "Account", "Pages", "Manage", "ResetAuthenticator.razor");

        Assert.Contains("current authenticator codes will stop working immediately", markup, StringComparison.Ordinal);
        Assert.Contains("<PageTitle>Reset authenticator app</PageTitle>", markup, StringComparison.Ordinal);
        Assert.Contains("will turn off until you connect and verify the new key", markup, StringComparison.Ordinal);
        Assert.Contains(">Keep current authenticator</a>", markup, StringComparison.Ordinal);
        Assert.Contains(">Reset authenticator app</button>", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void DisableTwoFactor_ExplainsReducedProtectionAndOffersSafeExit()
    {
        var markup = ReadComponent("OpenWish.Web", "Components", "Account", "Pages", "Manage", "Disable2fa.razor");

        Assert.Contains("rely on your password alone", markup, StringComparison.Ordinal);
        Assert.Contains("existing authenticator key will remain available", markup, StringComparison.Ordinal);
        Assert.Contains(">Keep 2FA on</a>", markup, StringComparison.Ordinal);
        Assert.Contains(">Turn off 2FA</button>", markup, StringComparison.Ordinal);
        Assert.Contains("Two-factor authentication is off.", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Login_ExplainsPersistentSessionsAndGroupsRecoveryActions()
    {
        var markup = ReadComponent("OpenWish.Web", "Components", "Account", "Pages", "Login.razor");

        Assert.Contains("id=\"remember-me\"", markup, StringComparison.Ordinal);
        Assert.Contains("aria-describedby=\"remember-me-help\"", markup, StringComparison.Ordinal);
        Assert.Contains("Avoid this on shared devices.", markup, StringComparison.Ordinal);
        Assert.Contains("<h3 class=\"h6\">Need help signing in?</h3>", markup, StringComparison.Ordinal);
        Assert.Contains(">Reset your password</a>", markup, StringComparison.Ordinal);
        Assert.Contains(">Resend your confirmation email</a>", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Registration_ConnectsPasswordRequirementsAndSignInAction()
    {
        var markup = ReadComponent("OpenWish.Web", "Components", "Account", "Pages", "Register.razor");

        Assert.Contains("id=\"password-guidance\"", markup, StringComparison.Ordinal);
        Assert.Contains("aria-describedby=\"password-guidance\"", markup, StringComparison.Ordinal);
        Assert.Contains("6 to 100 characters", markup, StringComparison.Ordinal);
        Assert.Contains("uppercase letter, lowercase letter, number, and symbol", markup, StringComparison.Ordinal);
        Assert.Contains(">Create account</button>", markup, StringComparison.Ordinal);
        Assert.Contains("GetUriWithQueryParameters(\"Account/Login\"", markup, StringComparison.Ordinal);
        Assert.Contains(">Log in</a>", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void PasswordRecovery_ExplainsPrivateOutcomeAndProvidesSafeNavigation()
    {
        var markup = ReadComponent("OpenWish.Web", "Components", "Account", "Pages", "ForgotPassword.razor");

        Assert.Contains("class=\"auth-page\"", markup, StringComparison.Ordinal);
        Assert.Contains("autocomplete=\"email\" inputmode=\"email\"", markup, StringComparison.Ordinal);
        Assert.Contains("aria-describedby=\"reset-email-help\"", markup, StringComparison.Ordinal);
        Assert.Contains("If an eligible account matches", markup, StringComparison.Ordinal);
        Assert.Contains(">Send reset link</button>", markup, StringComparison.Ordinal);
        Assert.Contains("href=\"Account/Login\">Back to log in</a>", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void PasswordReset_ConnectsRequirementsAndNamesItsOutcome()
    {
        var markup = ReadComponent("OpenWish.Web", "Components", "Account", "Pages", "ResetPassword.razor");

        Assert.Contains("class=\"auth-page\"", markup, StringComparison.Ordinal);
        Assert.Contains("id=\"password-guidance\"", markup, StringComparison.Ordinal);
        Assert.Contains("aria-describedby=\"password-guidance\"", markup, StringComparison.Ordinal);
        Assert.Contains("6 to 100 characters", markup, StringComparison.Ordinal);
        Assert.Contains("uppercase letter, lowercase letter, number, and symbol", markup, StringComparison.Ordinal);
        Assert.Contains(">Save new password</button>", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void ConfirmationEmailRecovery_UsesEmailSemanticsAndPrivacyGuidance()
    {
        var markup = ReadComponent("OpenWish.Web", "Components", "Account", "Pages", "ResendEmailConfirmation.razor");

        Assert.Contains("class=\"auth-page\"", markup, StringComparison.Ordinal);
        Assert.Contains("autocomplete=\"email\" inputmode=\"email\"", markup, StringComparison.Ordinal);
        Assert.Contains("aria-describedby=\"confirmation-email-help\"", markup, StringComparison.Ordinal);
        Assert.Contains("For privacy, the result is the same", markup, StringComparison.Ordinal);
        Assert.Contains(">Send confirmation email</button>", markup, StringComparison.Ordinal);
        Assert.Contains("href=\"Account/Login\">Back to log in</a>", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void ReleaseNotes_LoadFailuresOfferADuplicateSafeRetry()
    {
        var markup = ReadComponent("OpenWish.Web.Client", "Components", "Pages", "WhatsNew.razor");

        Assert.Contains("disabled=\"@_isLoading\"", markup, StringComparison.Ordinal);
        Assert.Contains("@onclick=\"LoadReleaseNotesAsync\"", markup, StringComparison.Ordinal);
        Assert.Contains("@(_isLoading ? \"Trying again...\" : \"Try again\")", markup, StringComparison.Ordinal);
        Assert.Contains("if (_isLoading)", markup, StringComparison.Ordinal);
        Assert.True(
            markup.IndexOf("_releases = (await", StringComparison.Ordinal) <
            markup.IndexOf("_loadError = null;", StringComparison.Ordinal));
    }

    [Fact]
    public void ThemeToggle_NamesTheThemeThatWillBeApplied()
    {
        var markup = ReadComponent("OpenWish.Web.Client", "Components", "Shared", "ThemeToggle.razor");

        Assert.Contains("aria-label=\"@ToggleLabel\"", markup, StringComparison.Ordinal);
        Assert.Contains("\"Use light theme\" : \"Use dark theme\"", markup, StringComparison.Ordinal);
        Assert.DoesNotContain("Toggle dark or light theme", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void EventCreation_NamesBothSupportedPlanningWorkflows()
    {
        var markup = ReadComponent("OpenWish.Web.Client", "Components", "Pages", "Events", "NewEvent.razor");

        Assert.Contains("<PageTitle>Create an event</PageTitle>", markup, StringComparison.Ordinal);
        Assert.Contains("Title=\"Create an event\"", markup, StringComparison.Ordinal);
        Assert.Contains("gift exchange or coordinate wishlists for a celebration", markup, StringComparison.Ordinal);
        Assert.Contains("class=\"btn btn-outline-secondary\" type=\"button\"", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void StandaloneItemCreation_ProvidesContextAndSafeNavigation()
    {
        var markup = ReadComponent("OpenWish.Web.Client", "Components", "Pages", "Wishlists", "AddItem.razor");

        Assert.Contains("<PageTitle>Add an item</PageTitle>", markup, StringComparison.Ordinal);
        Assert.Contains("Title=\"Add an item\"", markup, StringComparison.Ordinal);
        Assert.Contains("href=\"/wishlists/@WishlistId\"", markup, StringComparison.Ordinal);
        Assert.Contains("OnCancel=\"@HandleCancel\"", markup, StringComparison.Ordinal);
        Assert.Contains("NavigationManager.NavigateTo($\"/wishlists/{WishlistId}\")", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void WishlistCreation_UsesTheStandardPageContext()
    {
        var markup = ReadComponent("OpenWish.Web.Client", "Components", "Pages", "Wishlists", "NewWishlist.razor");

        Assert.Contains("<PageTitle>Create a wishlist</PageTitle>", markup, StringComparison.Ordinal);
        Assert.Contains("Title=\"Create a wishlist\"", markup, StringComparison.Ordinal);
        Assert.Contains("choose who can see your gift ideas", markup, StringComparison.Ordinal);
        Assert.Contains("Back to wishlists", markup, StringComparison.Ordinal);
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