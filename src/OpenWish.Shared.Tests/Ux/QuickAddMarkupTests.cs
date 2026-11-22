using Xunit;

namespace OpenWish.Shared.Tests.Ux;

public class QuickAddMarkupTests
{
    [Fact]
    public void QuickAddField_KeepsTheSpaceBarAndShowsHelpAndErrorsBesideTheField()
    {
        var markup = ReadComponent("Pages", "Wishlists", "QuickAdd.razor");
        var styles = ReadComponent("Pages", "Wishlists", "QuickAdd.razor.css");
        var field = markup[markup.IndexOf("<input id=\"quick-add-text\"", StringComparison.Ordinal)..];
        field = field[..field.IndexOf("/>", StringComparison.Ordinal)];

        // Typed idea names need a space bar, which phone URL keyboards drop.
        Assert.DoesNotContain("inputmode=\"url\"", field, StringComparison.Ordinal);
        Assert.Contains("aria-invalid=\"@(_formError is null ? null : \"true\")\"", field, StringComparison.Ordinal);

        var row = markup[markup.IndexOf("<div class=\"quick-add-row\">", StringComparison.Ordinal)..markup.IndexOf("</form>", StringComparison.Ordinal)];
        Assert.Contains("id=\"quick-add-help\"", row, StringComparison.Ordinal);
        Assert.Contains("class=\"quick-add-message\" role=\"alert\"", row, StringComparison.Ordinal);
        Assert.Contains("\"message\"\n            \"action\";", styles.Replace("\r\n", "\n"), StringComparison.Ordinal);
    }

    [Fact]
    public void QuickAddWays_GroupIntoOneSurfaceAndLeadWithThePhoneOnPhones()
    {
        var markup = ReadComponent("Pages", "Wishlists", "QuickAdd.razor");
        var styles = ReadComponent("Pages", "Wishlists", "QuickAdd.razor.css");

        Assert.Contains("<article class=\"way way-phone\">", markup, StringComparison.Ordinal);
        Assert.Contains(".way + .way {", styles, StringComparison.Ordinal);
        var phoneStyles = styles[styles.IndexOf("@media (max-width: 575.98px)", StringComparison.Ordinal)..];
        Assert.Contains(".way-phone {\n        order: -1;", phoneStyles.Replace("\r\n", "\n"), StringComparison.Ordinal);
    }

    [Fact]
    public void QuickAddCapture_MovesStartOverBelowTheChoices()
    {
        var markup = ReadComponent("Pages", "Wishlists", "QuickAdd.razor");

        Assert.DoesNotContain("<Actions>", markup, StringComparison.Ordinal);
        var choices = markup.IndexOf("<ul class=\"wishlist-choices\">", StringComparison.Ordinal);
        var startOver = markup.IndexOf("Start over with a different idea", StringComparison.Ordinal);
        Assert.InRange(choices, 0, startOver);
        Assert.Contains("<p class=\"quick-add-reset\">", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void ProductImportMessages_AreCompactAndPairColorWithAnIcon()
    {
        var importer = ReadComponent("Wishlist", "ProductLinkImporter.razor");
        var styles = ReadComponent("Wishlist", "ProductLinkImporter.razor.css");

        Assert.Contains("<p class=\"product-import-message @MessageClass\" role=\"@(_messageIsError ? \"alert\" : \"status\")\">", importer, StringComparison.Ordinal);
        Assert.Contains("<OpenWishIcon Name=\"@MessageIcon\" />", importer, StringComparison.Ordinal);
        Assert.Contains("? \"exclamation-circle\"", importer, StringComparison.Ordinal);
        Assert.DoesNotContain("class=\"alert ", importer, StringComparison.Ordinal);
        // Only one spinner while importing: the button carries it and the status line explains it.
        var pending = importer[importer.IndexOf("product-import-message is-pending", StringComparison.Ordinal)..];
        pending = pending[..pending.IndexOf("</p>", StringComparison.Ordinal)];
        Assert.DoesNotContain("spinner-border", pending, StringComparison.Ordinal);
        // Phones keep the Import button beside the field rather than on a row of its own.
        Assert.DoesNotContain("flex-direction: column", styles, StringComparison.Ordinal);
    }

    [Fact]
    public void DuplicateIdeaNotice_UsesAnInsetWarningSurface()
    {
        var styles = ReadComponent("Wishlist", "DuplicateIdeaNotice.razor.css");

        Assert.Contains("border: 1px solid var(--color-warning-border);", styles, StringComparison.Ordinal);
        Assert.Contains("border-radius: 0.75rem;", styles, StringComparison.Ordinal);
        Assert.Contains(".duplicate-idea-note ::deep .ow-icon", styles, StringComparison.Ordinal);
    }

    private static string ReadComponent(params string[] parts) =>
        File.ReadAllText(Path.Combine([FindSolutionDirectory(), "OpenWish.Web.Client", "Components", .. parts]));

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