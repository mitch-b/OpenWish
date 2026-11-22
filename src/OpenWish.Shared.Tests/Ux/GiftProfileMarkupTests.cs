using Xunit;

namespace OpenWish.Shared.Tests.Ux;

public class GiftProfileMarkupTests
{
    [Fact]
    public void GiftHintsPanel_UsesSentenceCaseLabelsAndHighlightsWhatToAvoidWithText()
    {
        var markup = Read("OpenWish.Web.Client", "Components", "Wishlist", "GiftProfilePanel.razor");
        var styles = Read("OpenWish.Web.Client", "Components", "Wishlist", "GiftProfilePanel.razor.css");

        Assert.Contains("@if (_profile is { HasContent: true })", markup, StringComparison.Ordinal);
        Assert.Contains("data-testid=\"gift-profile\"", markup, StringComparison.Ordinal);
        Assert.Contains("gift-profile-hint--avoid", markup, StringComparison.Ordinal);
        Assert.Contains("Please avoid</dt>", markup, StringComparison.Ordinal);
        Assert.DoesNotContain("text-transform: uppercase", styles, StringComparison.Ordinal);
        Assert.Contains("repeat(auto-fit, minmax(14rem, 1fr))", styles, StringComparison.Ordinal);
        Assert.Contains("var(--color-warning-text)", styles, StringComparison.Ordinal);
    }

    [Fact]
    public void GiftHintsForm_KeepsVisibleLabelsHelpAndTheLengthLimit()
    {
        var page = Read("OpenWish.Web", "Components", "Account", "Pages", "Manage", "GiftProfile.razor");

        Assert.DoesNotContain("form-floating mb-3\">\n                <InputTextArea", page, StringComparison.Ordinal);
        Assert.DoesNotContain("style=\"height", page, StringComparison.Ordinal);
        foreach (var field in new[] { "gift-sizes", "gift-favorites", "gift-avoid" })
        {
            Assert.Contains($"<label for=\"{field}\" class=\"form-label\">", page, StringComparison.Ordinal);
            Assert.Contains($"aria-describedby=\"{field}-help hints-help\"", page, StringComparison.Ordinal);
        }

        Assert.Equal(3, page.Split("maxlength=\"@GiftProfileModel.MaxLength\"").Length - 1);
        Assert.Contains("id=\"save-gift-hints\"", page, StringComparison.Ordinal);
    }

    [Fact]
    public void GiftProfileSettings_ButtonsFitDesktopAndFillPhones()
    {
        var page = Read("OpenWish.Web", "Components", "Account", "Pages", "Manage", "GiftProfile.razor");
        var styles = Read("OpenWish.Web", "Components", "Account", "Pages", "Manage", "GiftProfile.razor.css");

        Assert.DoesNotContain("w-100 btn btn-lg", page, StringComparison.Ordinal);
        Assert.Equal(2, page.Split("gift-profile-submit").Length - 1);
        Assert.Contains("@media (max-width: 640px)", styles, StringComparison.Ordinal);
        Assert.Contains("min-height: 2.75rem;", styles, StringComparison.Ordinal);
    }

    private static string Read(params string[] parts)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "OpenWish.slnx")))
        {
            directory = directory.Parent;
        }

        var root = directory?.FullName ?? throw new DirectoryNotFoundException("Could not locate OpenWish.slnx.");
        return File.ReadAllText(Path.Combine([root, .. parts])).Replace("\r\n", "\n", StringComparison.Ordinal);
    }
}