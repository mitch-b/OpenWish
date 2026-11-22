using Xunit;

namespace OpenWish.Shared.Tests.Ux;

public class EventsIndexMarkupTests
{
    [Fact]
    public void EventsIndex_GroupsUpcomingBeforePastEvents()
    {
        var markup = ReadComponent("OpenWish.Web.Client", "Components", "Pages", "Events", "Index.razor");

        Assert.Contains(".OrderBy(evt => evt.Date)", markup, StringComparison.Ordinal);
        Assert.Contains(".OrderByDescending(evt => evt.Date)", markup, StringComparison.Ordinal);
        Assert.True(
            markup.IndexOf("id=\"events-upcoming-title\"", StringComparison.Ordinal) <
            markup.IndexOf("id=\"events-past-title\"", StringComparison.Ordinal));
        Assert.Contains("Nothing coming up.", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void EventsIndex_HasOnePrimaryNewEventAction()
    {
        var markup = ReadComponent("OpenWish.Web.Client", "Components", "Pages", "Events", "Index.razor");
        var styles = ReadComponent("OpenWish.Web.Client", "Components", "Pages", "Events", "Index.razor.css");

        Assert.Contains("class=\"btn btn-primary events-new-action\"", markup, StringComparison.Ordinal);
        Assert.Contains("class=\"btn btn-outline-secondary\" data-testid=\"events-download\"", markup, StringComparison.Ordinal);
        Assert.Contains("<a href=\"/events/new\" class=\"btn btn-outline-secondary\">", markup, StringComparison.Ordinal);
        Assert.DoesNotContain("btn-light", markup, StringComparison.Ordinal);
        Assert.Contains("flex-direction: column-reverse;", styles, StringComparison.Ordinal);
    }

    [Fact]
    public void EventCard_ShowsRoleBudgetAndPastState()
    {
        var markup = ReadComponent("OpenWish.Web.Client", "Components", "Event", "EventCard.razor");

        Assert.Contains("<span class=\"event-role\">Organizer</span>", markup, StringComparison.Ordinal);
        Assert.Contains("Hosted by @Event.CreatedBy?.UserName", markup, StringComparison.Ordinal);
        Assert.Contains("@Event.Budget.Value.ToString(\"C\") budget", markup, StringComparison.Ordinal);
        Assert.Contains("event-card-past", markup, StringComparison.Ordinal);
        Assert.Contains("!string.IsNullOrEmpty(_currentUserId) && _currentUserId == Event?.CreatedBy?.Id", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void PendingInvitations_UseLargeHydrationGuardedActions()
    {
        var markup = ReadComponent("OpenWish.Web.Client", "Components", "Event", "PendingInvitations.razor");
        var styles = ReadComponent("OpenWish.Web.Client", "Components", "Event", "PendingInvitations.razor.css");

        Assert.DoesNotContain("btn-group-sm", markup, StringComparison.Ordinal);
        Assert.Equal(4, CountOccurrences(markup, "disabled=\"@(!RendererInfo.IsInteractive || IsProcessing(invitation.PublicId))\""));
        Assert.Contains("<h2 class=\"pending-invitations-title\" id=\"pending-invitations-title\">", markup, StringComparison.Ordinal);
        Assert.Contains("<ul class=\"pending-invitation-list\">", markup, StringComparison.Ordinal);
        Assert.Contains("min-height: 2.75rem;", styles, StringComparison.Ordinal);
        Assert.DoesNotContain("--bs-warning", styles, StringComparison.Ordinal);
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