using Xunit;

namespace OpenWish.Shared.Tests.Ux;

public class GiftExchangeOrganizerMarkupTests
{
    [Fact]
    public void SetupSteps_HighlightOnlyTheSingleNextAction()
    {
        var setup = ReadEventComponent("SecretSantaSetup.razor");
        var step = ReadEventComponent("SetupStep.razor");

        Assert.Equal(3, setup.Split("IsNext=\"@(NextStepNumber ==", StringSplitOptions.None).Length - 1);
        Assert.Contains("Next: <strong>@NextStepTitle</strong>", setup, StringComparison.Ordinal);
        Assert.Contains("aria-current=\"@(IsNext ? \"step\" : null)\"", step, StringComparison.Ordinal);
        Assert.Contains("IsNext\n        ? \"btn-primary\"", step.ReplaceLineEndings("\n"), StringComparison.Ordinal);
    }

    [Fact]
    public void SetupStep_QuietActionsKeepMobileTouchTargets()
    {
        var styles = ReadEventComponent("SetupStep.razor.css");

        Assert.Contains(".setup-step .setup-step-quiet-action {\n    min-width: 2.75rem;", styles.ReplaceLineEndings("\n"), StringComparison.Ordinal);
    }

    [Fact]
    public void DrawReadiness_ExplainsBlockersAndAdvisoriesInPlainLanguage()
    {
        var markup = ReadEventComponent("GiftExchangeManager.razor");

        Assert.Contains("aria-label=\"Draw readiness\"", markup, StringComparison.Ordinal);
        Assert.Contains("readiness-item readiness-blocker", markup, StringComparison.Ordinal);
        Assert.Contains("At least one invitee has to accept before you can draw.", markup, StringComparison.Ordinal);
        Assert.Contains("Not required to draw.", markup, StringComparison.Ordinal);
        Assert.DoesNotContain("readiness-stat", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void DrawAndReset_ConfirmConsequencesBehindHydratedButtons()
    {
        var markup = ReadEventComponent("GiftExchangeManager.razor");

        Assert.Contains("@onclick=\"OpenDrawConfirmation\"", markup, StringComparison.Ordinal);
        Assert.Contains("@onclick=\"OpenResetConfirmation\"", markup, StringComparison.Ordinal);
        Assert.Equal(2, markup.Split("disabled=\"@(!RendererInfo.IsInteractive || _eventStateChanged)\"", StringSplitOptions.None).Length - 1);
        Assert.Contains("Everyone in the draw gets an email that names are ready.", markup, StringComparison.Ordinal);
        Assert.Contains("Guest private links stop working until you draw names again.", markup, StringComparison.Ordinal);
        Assert.Contains("@ref=\"_resetCancelButton\" @onclick=\"CancelReset\"", markup, StringComparison.Ordinal);
        Assert.Contains("\"Drawing names...\"", markup, StringComparison.Ordinal);
        Assert.DoesNotContain("_showFinalConfirm", markup, StringComparison.Ordinal);
        Assert.DoesNotContain("role=\"alert\">\n                                <p class=\"mb-2\"><strong>@ConfirmationPrompt", markup.ReplaceLineEndings("\n"), StringComparison.Ordinal);
    }

    private static string ReadEventComponent(string fileName) =>
        File.ReadAllText(Path.Combine(FindSolutionDirectory(), "OpenWish.Web.Client", "Components", "Event", fileName));

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