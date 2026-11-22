using Xunit;

namespace OpenWish.Shared.Tests.Ux;

public class EventFormMarkupTests
{
    private static readonly string[] _formPath = ["OpenWish.Web.Client", "Components", "Event", "EventForm.razor"];
    private static readonly string[] _managePath = ["OpenWish.Web.Client", "Components", "Pages", "Events", "ManageEvent.razor"];

    [Fact]
    public void Form_AsksForNameDateAndBudgetBeforeOptionalDetails()
    {
        var markup = ReadComponent(_formPath);
        var name = markup.IndexOf("id=\"name\"", StringComparison.Ordinal);
        var date = markup.IndexOf("id=\"date\"", StringComparison.Ordinal);
        var budget = markup.IndexOf("id=\"budget\"", StringComparison.Ordinal);
        var note = markup.IndexOf("id=\"description\"", StringComparison.Ordinal);
        var reveal = markup.IndexOf("id=\"gift-box-reveal\"", StringComparison.Ordinal);

        Assert.True(name < date && date < budget && budget < note && note < reveal,
            "Required details should come before the optional note and match reveal.");
        Assert.Contains(">Event name</label>", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Budget_ShowsItsCurrencyAndUsesADecimalKeyboard()
    {
        var markup = ReadComponent(_formPath);

        Assert.Contains("<span class=\"input-group-text\" aria-hidden=\"true\">$</span>", markup, StringComparison.Ordinal);
        Assert.Contains("inputmode=\"decimal\"", markup, StringComparison.Ordinal);
        Assert.DoesNotContain("min=\"0\"", markup, StringComparison.Ordinal);
        Assert.Contains("aria-describedby=\"date-help\"", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Choices_AndSubmitWaitForInteractivity()
    {
        var markup = ReadComponent(_formPath);

        Assert.Equal(4, markup.Split("disabled=\"@(!RendererInfo.IsInteractive)\"", StringSplitOptions.None).Length - 1);
        Assert.Contains("disabled=\"@(IsSubmitting || !RendererInfo.IsInteractive)\"", markup, StringComparison.Ordinal);
        Assert.Contains("id=\"event-type-locked-help\"", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Management_UsesTheSharedDialogForDeletionAndLinksBack()
    {
        var markup = ReadComponent(_managePath);

        Assert.DoesNotContain("modal fade show d-block", markup, StringComparison.Ordinal);
        Assert.Contains("ReturnFocusElementId=\"manage-event-delete\"", markup, StringComparison.Ordinal);
        Assert.Contains("CanClose=\"@(!_isDeleting)\"", markup, StringComparison.Ordinal);
        Assert.Contains("<span>Deleting event...</span>", markup, StringComparison.Ordinal);
        Assert.Contains("We couldn't delete the event.", markup, StringComparison.Ordinal);
        Assert.Contains("<a class=\"btn btn-outline-secondary\" href=\"/events/@EventId\">", markup, StringComparison.Ordinal);
        Assert.DoesNotContain("ShowValidationSummary=\"true\"", markup, StringComparison.Ordinal);
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