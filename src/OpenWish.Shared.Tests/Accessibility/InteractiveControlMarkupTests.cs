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
    public void EventCards_UseExplicitNamedNavigationLinks()
    {
        var markup = ReadComponent("OpenWish.Web.Client", "Components", "Event", "EventCard.razor");

        Assert.Contains("class=\"event-card-link\" href=\"/events/@Event.PublicId\"", markup, StringComparison.Ordinal);
        Assert.Contains("<span class=\"visually-hidden\">: @Event.Name</span>", markup, StringComparison.Ordinal);
        Assert.DoesNotContain("@onclick=\"NavigateToEvent\"", markup, StringComparison.Ordinal);
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
        Assert.Contains("disabled=\"@IsSubmitting\"", formMarkup, StringComparison.Ordinal);
        Assert.Contains("Adding item...", formMarkup, StringComparison.Ordinal);
        Assert.Contains("aria-busy=\"@IsSubmitting\"", formMarkup, StringComparison.Ordinal);
        Assert.DoesNotContain("<EditForm Enhance", formMarkup, StringComparison.Ordinal);
    }

    [Fact]
    public void GiftExchangeDisplay_DistinguishesLoadingFailuresFromNoAssignment()
    {
        var markup = ReadComponent("OpenWish.Web.Client", "Components", "Event", "GiftExchangeDisplay.razor");

        Assert.Contains("aria-busy=\"@_loading\"", markup, StringComparison.Ordinal);
        Assert.Contains("role=\"alert\"", markup, StringComparison.Ordinal);
        Assert.Contains("We couldn't load your Secret Santa match. Try again.", markup, StringComparison.Ordinal);
        Assert.Contains("@onclick=\"LoadGiftExchange\"", markup, StringComparison.Ordinal);
        Assert.Contains("Logger.LogError(ex", markup, StringComparison.Ordinal);
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