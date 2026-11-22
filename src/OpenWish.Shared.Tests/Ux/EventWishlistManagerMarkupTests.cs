using Xunit;

namespace OpenWish.Shared.Tests.Ux;

public class EventWishlistManagerMarkupTests
{
    [Fact]
    public void EventWishlistManager_PutsNoListActionsInsideTheEmptyStateAndMakesCreatePrimaryWhenNothingCanBeShared()
    {
        var markup = ReadManager();

        Assert.Contains("data-testid=\"@(hasOwnWishlist ? null : \"event-wishlist-empty\")\"", markup, StringComparison.Ordinal);
        Assert.True(
            markup.IndexOf("Give your gift giver a few clues.", StringComparison.Ordinal) <
            markup.IndexOf("<div class=\"event-wishlist-tools\">", StringComparison.Ordinal),
            "The share/create actions should sit inside the empty state they resolve.");
        Assert.Contains(
            "hasOwnWishlist ? \"btn-link\" : attachableCount > 0 ? \"btn-outline-primary\" : \"btn-primary\"",
            markup,
            StringComparison.Ordinal);
        Assert.Contains("class=\"btn @(hasOwnWishlist ? \"btn-outline-primary\" : \"btn-primary\") event-wishlist-share-button\"", markup, StringComparison.Ordinal);
        Assert.DoesNotContain("All of your available wishlists are already attached.", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void EventWishlistManager_ShowsOneConcisePrivacyNoteThatGivesWayToTheCreateForm()
    {
        var markup = ReadManager();
        var privacyIndex = markup.IndexOf("class=\"event-wishlist-privacy\"", StringComparison.Ordinal);

        Assert.True(privacyIndex > 0);
        Assert.Contains("Reservations stay hidden from you.", markup, StringComparison.Ordinal);
        Assert.Contains("@if (!_showCreateForm)", markup[..privacyIndex][^200..], StringComparison.Ordinal);
    }

    [Fact]
    public void EventWishlistManager_KeepsParticipantRowsScannable()
    {
        var markup = ReadManager();
        var styles = ReadManager(".css");

        Assert.Contains("@if (ShouldShowRole(participant))", markup, StringComparison.Ordinal);
        Assert.Contains("@if (ShouldShowStatus(participant))", markup, StringComparison.Ordinal);
        Assert.Contains("!string.Equals(participant.Status, \"Accepted\", StringComparison.OrdinalIgnoreCase)", markup, StringComparison.Ordinal);
        Assert.Contains("CanDetach(wishlist) && _isEventOwner && !participant.IsCurrentUser", markup, StringComparison.Ordinal);
        Assert.Contains("0 => \"No ideas yet\"", markup, StringComparison.Ordinal);
        Assert.Contains("aria-label=\"@($\"View {wishlist.Name}\")\"", markup, StringComparison.Ordinal);
        Assert.DoesNotContain("badge bg-light text-dark", markup, StringComparison.Ordinal);
        Assert.Contains("flex: 1 1 7rem;", styles, StringComparison.Ordinal);
    }

    [Fact]
    public void EventWishlistManager_GuardsActionsUntilInteractiveAndAnnouncesRefreshes()
    {
        var markup = ReadManager();

        Assert.Contains("disabled=\"@(_isSubmitting || !RendererInfo.IsInteractive)\"\n                                                @onclick=\"() => PromptWishlistRemoval(wishlist.Id)\"", markup.Replace("\r\n", "\n"), StringComparison.Ordinal);
        Assert.Contains("disabled=\"@(!RendererInfo.IsInteractive)\" @onclick=\"LoadWishlistsAsync\"", markup, StringComparison.Ordinal);
        Assert.Contains("class=\"event-wishlist-refreshing small\" role=\"status\"", markup, StringComparison.Ordinal);
        Assert.Contains("aria-busy=\"@(_isLoading ? \"true\" : \"false\")\"", markup, StringComparison.Ordinal);
    }

    private static string ReadManager(string extension = "") =>
        ReadComponent("OpenWish.Web.Client", "Components", "Event", $"EventWishlistManager.razor{extension}");

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