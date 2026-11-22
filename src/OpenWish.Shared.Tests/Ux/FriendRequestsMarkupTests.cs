using Xunit;

namespace OpenWish.Shared.Tests.Ux;

public class FriendRequestsMarkupTests
{
    private const string HydrationGuard = "!RendererInfo.IsInteractive";

    [Fact]
    public void FriendRequestList_GuardsActionsUntilHydrationAndNamesEachBusyState()
    {
        var markup = ReadSocial("FriendRequestList.razor");

        Assert.Equal(6, CountOccurrences(markup, "disabled=\"@(!RendererInfo.IsInteractive || _processingRequestId.HasValue)\""));
        Assert.Contains("IsBusy(\"accept\", request.Id) ? \"Accepting...\" : \"Accept\"", markup, StringComparison.Ordinal);
        Assert.Contains("IsBusy(\"decline\", request.Id) ? \"Declining...\" : \"Decline\"", markup, StringComparison.Ordinal);
        Assert.Contains("IsBusy(\"cancel\", request.Id) ? \"Canceling...\" : \"Cancel\"", markup, StringComparison.Ordinal);
        Assert.Contains("IsBusy(\"cancel-invite\", invite.Id) ? \"Canceling...\" : \"Cancel\"", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void FriendRequestList_DistinguishesIncomingFromSentAndOffersNextStepWhenEmpty()
    {
        var markup = ReadSocial("FriendRequestList.razor");
        var css = ReadSocial("FriendRequestList.razor.css");

        Assert.Contains("<small>Wants to be friends</small>", markup, StringComparison.Ordinal);
        Assert.Contains("<small>Request sent · waiting for a reply</small>", markup, StringComparison.Ordinal);
        Assert.Contains("<small>Invited by email · hasn't joined yet</small>", markup, StringComparison.Ordinal);
        Assert.Contains("data-testid=\"friend-requests-empty\"", markup, StringComparison.Ordinal);
        Assert.Contains("href=\"/friends?section=invite\"", markup, StringComparison.Ordinal);
        Assert.DoesNotContain("alert alert-info", markup, StringComparison.Ordinal);
        Assert.Contains("@media (max-width: 575.98px)", css, StringComparison.Ordinal);
        Assert.Contains("min-height: 2.75rem;", css, StringComparison.Ordinal);
    }

    [Fact]
    public void FriendSearch_ExplainsWhatAnInviteDoesAndWaitsForHydration()
    {
        var markup = ReadSocial("FriendSearch.razor");
        var css = ReadSocial("FriendSearch.razor.css");

        Assert.Contains("Anyone already on OpenWish gets a friend request right away", markup, StringComparison.Ordinal);
        Assert.Contains($"disabled=\"@({HydrationGuard} || _sendingInvites", markup, StringComparison.Ordinal);
        Assert.Contains("<span class=\"invite-shortcut-hint\">", markup, StringComparison.Ordinal);
        Assert.Contains("@media (pointer: coarse)", css, StringComparison.Ordinal);
    }

    [Fact]
    public void FriendSuggestions_ExplainWhySuggestedAndWaitForHydration()
    {
        var markup = ReadSocial("FriendSuggestions.razor");

        Assert.Contains("<small><OpenWishIcon Name=\"link-45deg\" /> @suggestion.Reason</small>", markup, StringComparison.Ordinal);
        Assert.Contains($"disabled=\"@({HydrationGuard} || _sendingTo is not null)\"", markup, StringComparison.Ordinal);
        Assert.Contains("<span>Sending request...</span>", markup, StringComparison.Ordinal);
        Assert.DoesNotContain("class=\"card friends-panel", markup, StringComparison.Ordinal);
    }

    private static int CountOccurrences(string text, string value)
    {
        var count = 0;
        for (var index = text.IndexOf(value, StringComparison.Ordinal); index >= 0; index = text.IndexOf(value, index + value.Length, StringComparison.Ordinal))
        {
            count++;
        }

        return count;
    }

    private static string ReadSocial(string fileName) =>
        ReadComponent("OpenWish.Web.Client", "Components", "Social", fileName);

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