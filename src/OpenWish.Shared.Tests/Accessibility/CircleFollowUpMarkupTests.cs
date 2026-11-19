using Xunit;

namespace OpenWish.Shared.Tests.Accessibility;

public class CircleFollowUpMarkupTests
{
    [Fact]
    public void ManageWishlist_LabelsCircleAccessAndDoesNotClaimARemovalThatDidNothing()
    {
        var markup = Read("Pages", "Wishlists", "ManageWishlist.razor");

        Assert.Contains("data-testid=\"friend-via-circle\"", markup, StringComparison.Ordinal);
        Assert.Contains("CircleRules.GetSharingCircleNames(_circles, WishlistId, friend.Id)", markup, StringComparison.Ordinal);
        Assert.Contains("var removed = await WishlistService.RemoveWishlistPermissionByPublicIdAsync", markup, StringComparison.Ordinal);
        Assert.Contains("sees this list through", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void HttpClient_ReportsWhetherADirectGrantWasActuallyRemoved()
    {
        var source = Read("Services", "WishlistHttpClientService.cs");

        Assert.Contains("response.IsSuccessStatusCode && await response.Content.ReadFromJsonAsync<bool>()", source, StringComparison.Ordinal);
    }

    [Fact]
    public void EventInvitations_ShowHowManyCircleMembersAreStillToInvite()
    {
        var markup = Read("Event", "EventInvitations.razor");

        Assert.Contains("InvitableCircleMembers(circle).Count", markup, StringComparison.Ordinal);
        Assert.Contains("disabled=\"@(_isSending || invitable == 0)\"", markup, StringComparison.Ordinal);
        Assert.Contains("all invited", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void CirclesPanel_LinksSharedListsToTheirSharingPage()
    {
        var markup = Read("Social", "CirclesPanel.razor");

        Assert.Contains("/wishlists/@Uri.EscapeDataString(list.PublicId)/manage", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void FriendsWishlists_ExplainAnEmptyCircle()
    {
        var markup = Read("Pages", "Wishlists", "Index.razor");

        Assert.Contains("data-testid=\"circle-empty-members\"", markup, StringComparison.Ordinal);
        Assert.Contains("data-testid=\"circle-empty-lists\"", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void FriendList_OffersTheSelectedCirclesLists()
    {
        var markup = Read("Social", "FriendList.razor");

        Assert.Contains("data-testid=\"circle-filter-lists\"", markup, StringComparison.Ordinal);
        Assert.Contains("View lists from @activeCircle.Name", markup, StringComparison.Ordinal);
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
        var relative = segments[0] is "Services" ? segments : ["Components", .. segments];
        return File.ReadAllText(Path.Combine([root, "OpenWish.Web.Client", .. relative]));
    }
}