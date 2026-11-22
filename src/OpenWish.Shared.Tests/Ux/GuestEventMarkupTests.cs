using Xunit;

namespace OpenWish.Shared.Tests.Ux;

public class GuestEventMarkupTests
{
    private static readonly string[] _guestPath = ["OpenWish.Web", "Components", "Pages", "GuestEvent.razor"];
    private static readonly string[] _guestStylesPath = ["OpenWish.Web", "Components", "Pages", "GuestEvent.razor.css"];
    private static readonly string[] _runAgainPath = ["OpenWish.Web.Client", "Components", "Event", "RunEventAgain.razor"];

    [Fact]
    public void GuestPage_ExplainsTheInvitationBeforeAskingForAResponse()
    {
        var markup = ReadComponent(_guestPath);
        var header = markup.IndexOf("<header class=\"guest-header\">", StringComparison.Ordinal);
        var invitedBy = markup.IndexOf("invited you to this gift exchange.", StringComparison.Ordinal);
        var facts = markup.IndexOf("class=\"guest-facts\"", StringComparison.Ordinal);
        var step = markup.IndexOf("class=\"guest-step\"", StringComparison.Ordinal);

        Assert.True(header >= 0 && header < invitedBy && invitedBy < facts && facts < step,
            "Guests should see who invited them, the date and the budget before the response form.");
        Assert.Contains("<h2 id=\"guest-step-title\">Join the exchange</h2>", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void ExpiredLink_HasItsOwnStateThatGivesWayToANewerLink()
    {
        var markup = ReadComponent(_guestPath);
        var styles = ReadComponent(_guestStylesPath);

        Assert.Contains("<h1 id=\"guest-unavailable-title\">This link isn't working anymore</h1>", markup, StringComparison.Ordinal);
        Assert.Contains("aria-labelledby=\"guest-unavailable-title\" data-guest-idle", markup, StringComparison.Ordinal);
        Assert.Contains("guest-state guest-connect-only", markup, StringComparison.Ordinal);
        Assert.Contains(".guest-event:has(form[data-guest-access]:not([hidden])) [data-guest-idle]", styles, StringComparison.Ordinal);
        Assert.Contains(".guest-event:not(:has(form[data-guest-access]:not([hidden]))) .guest-connect-only", styles, StringComparison.Ordinal);
    }

    [Fact]
    public void RunAgain_WaitsForInteractivityAndKeepsNameErrorsBesideTheField()
    {
        var markup = ReadComponent(_runAgainPath);
        var name = markup.IndexOf("id=\"run-again-name\"", StringComparison.Ordinal);
        var nameError = markup.IndexOf("id=\"run-again-error\" class=\"invalid-feedback d-block\"", StringComparison.Ordinal);
        var date = markup.IndexOf("id=\"run-again-date\"", StringComparison.Ordinal);

        Assert.Contains("disabled=\"@(!RendererInfo.IsInteractive)\"", markup, StringComparison.Ordinal);
        Assert.Contains("disabled=\"@(_busy || !RendererInfo.IsInteractive)\" @onclick=\"Close\"", markup, StringComparison.Ordinal);
        Assert.True(name < nameError && nameError < date, "The missing-name error should sit directly under the name field.");
        Assert.Contains("aria-describedby=\"@(_nameError ? \"run-again-error\" : null)\"", markup, StringComparison.Ordinal);
        Assert.DoesNotContain("(copy)", markup, StringComparison.Ordinal);
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

        return directory?.FullName ?? throw new InvalidOperationException("Could not find OpenWish.slnx.");
    }
}