using Xunit;

namespace OpenWish.Shared.Tests.Ux;

public class EventDetailsFrameMarkupTests
{
    private static readonly string[] _pagePath = ["OpenWish.Web.Client", "Components", "Pages", "Events", "EventDetails.razor"];

    [Fact]
    public void Header_SummarizesBudgetAndHostBeforeSections()
    {
        var markup = ReadComponent(_pagePath);
        var header = markup[markup.IndexOf("<header class=\"event-header\"", StringComparison.Ordinal)..markup.IndexOf("</header>", StringComparison.Ordinal)];

        Assert.Contains("data-testid=\"event-countdown\"", header, StringComparison.Ordinal);
        Assert.Contains("data-testid=\"event-header-budget\"", header, StringComparison.Ordinal);
        Assert.Contains("Hosted by <strong>@GetOwnerDisplayName()</strong>", header, StringComparison.Ordinal);
        Assert.Contains("event-countdown-badge @(isPast ? \"is-past\" : null)", header, StringComparison.Ordinal);
        Assert.DoesNotContain("display-4", markup, StringComparison.Ordinal);
        Assert.True(
            markup.IndexOf("</header>", StringComparison.Ordinal) < markup.IndexOf("<PageSections", StringComparison.Ordinal),
            "The event summary should precede the section navigation.");
    }

    [Fact]
    public void UnavailableEvent_DoesNotBlameTheConnectionOrOfferRetry()
    {
        var markup = ReadComponent(_pagePath);

        Assert.Contains("ex is KeyNotFoundException or UnauthorizedAccessException", markup, StringComparison.Ordinal);
        Assert.Contains("HttpStatusCode.NotFound or System.Net.HttpStatusCode.Forbidden", markup, StringComparison.Ordinal);
        Assert.Contains("@if (!_isUnavailable)", markup, StringComparison.Ordinal);
        Assert.Contains("@if (!string.IsNullOrWhiteSpace(_loadError))\n        {\n            <div class=\"alert alert-danger\" role=\"alert\">@_loadError</div>",
            markup.ReplaceLineEndings("\n"), StringComparison.Ordinal);
    }

    [Fact]
    public void PendingInvitee_GetsALabelledPrimaryCallout()
    {
        var markup = ReadComponent(_pagePath);
        var styles = ReadComponent([.. _pagePath[..^1], "EventDetails.razor.css"]);

        Assert.Contains("<section class=\"event-invite-callout\" aria-labelledby=\"event-invite-title\">", markup, StringComparison.Ordinal);
        Assert.Contains("<a class=\"btn btn-primary\" href=\"@PendingInvitationLink\">Review invitation</a>", markup, StringComparison.Ordinal);
        Assert.DoesNotContain("btn-success", markup, StringComparison.Ordinal);
        Assert.Contains("min-height: 44px;", styles, StringComparison.Ordinal);
        Assert.Contains("grid-template-columns: repeat(auto-fit, minmax(9.5rem, 1fr));", styles, StringComparison.Ordinal);
    }

    private static string ReadComponent(params string[] pathParts)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "OpenWish.slnx")))
        {
            directory = directory.Parent;
        }

        var solutionDirectory = directory?.FullName ?? throw new DirectoryNotFoundException("Could not locate OpenWish.slnx.");
        return File.ReadAllText(Path.Combine([solutionDirectory, .. pathParts]));
    }
}