using Xunit;

namespace OpenWish.Shared.Tests.Ux;

public class GiftExchangeMatchMarkupTests
{
    [Fact]
    public void GiftBox_WaitsForHydrationAndPutsTheGiftBeforeThePrivacyNoteOnPhones()
    {
        var markup = ReadComponent("GiftExchangeDisplay.razor");
        var styles = ReadComponent("GiftExchangeDisplay.razor.css");

        Assert.Contains("class=\"gift-box-button\" disabled=\"@(_opening || !RendererInfo.IsInteractive)\"", markup, StringComparison.Ordinal);
        Assert.Contains("gift-match-opening", markup, StringComparison.Ordinal);
        Assert.Contains("\"invite\"\n            \"box\"\n            \"help\"\n            \"error\"\n            \"privacy\"", styles.ReplaceLineEndings("\n"), StringComparison.Ordinal);
        Assert.Contains(".gift-match-opening ::deep .gift-box-lid", styles, StringComparison.Ordinal);
        Assert.Contains("transform: none !important;", styles, StringComparison.Ordinal);
    }

    [Fact]
    public void MatchCountdown_SitsOnItsOwnLineInsteadOfGluedToTheDate()
    {
        var markup = ReadComponent("GiftExchangeDisplay.razor");
        var styles = ReadComponent("GiftExchangeDisplay.razor.css");

        Assert.Contains("<span class=\"gift-match-countdown\" data-testid=\"exchange-countdown\">", markup, StringComparison.Ordinal);
        Assert.DoesNotContain("· @countdown", markup, StringComparison.Ordinal);
        Assert.Contains(".gift-match-countdown {\n    display: block;", styles.ReplaceLineEndings("\n"), StringComparison.Ordinal);
    }

    [Fact]
    public void Reveal_AsksForConfirmationAndAnnouncesPairingsAccessibly()
    {
        var markup = ReadComponent("ExchangeReveal.razor");

        Assert.Contains("@onclick=\"StartReveal\"", markup, StringComparison.Ordinal);
        Assert.Contains("Show every match to the whole group?", markup, StringComparison.Ordinal);
        Assert.Contains("Keep private", markup, StringComparison.Ordinal);
        Assert.Contains("Reveal to everyone", markup, StringComparison.Ordinal);
        Assert.Contains("<FocusOnFirstRender ElementId=\"exchange-reveal-cancel\" />", markup, StringComparison.Ordinal);
        Assert.Contains("<span class=\"visually-hidden\">drew</span>", markup, StringComparison.Ordinal);
        Assert.DoesNotContain("aria-label=\"drew\"", markup, StringComparison.Ordinal);
        Assert.DoesNotContain("disabled=\"@_busy\"", markup, StringComparison.Ordinal);
        Assert.Contains("!RendererInfo.IsInteractive)\" @onclick=\"LoadPairings\"", markup, StringComparison.Ordinal);
    }

    private static string ReadComponent(string fileName)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "OpenWish.slnx")))
        {
            directory = directory.Parent;
        }

        var solutionDirectory = directory?.FullName
            ?? throw new DirectoryNotFoundException("Could not locate OpenWish.slnx.");
        return File.ReadAllText(Path.Combine(solutionDirectory, "OpenWish.Web.Client", "Components", "Event", fileName));
    }
}