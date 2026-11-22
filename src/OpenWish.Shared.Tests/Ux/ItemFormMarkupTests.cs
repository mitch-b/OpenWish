using Xunit;

namespace OpenWish.Shared.Tests.Ux;

public class ItemFormMarkupTests
{
    private static readonly string[] _wishlistComponents = ["OpenWish.Web.Client", "Components", "Wishlist"];

    [Fact]
    public void IdeaFieldsValidatorExplainsBlankNamesAndNegativePrices()
    {
        var validator = ReadComponent([.. _wishlistComponents, "IdeaFieldsValidator.cs"]);

        Assert.Contains("Enter a name for this idea.", validator);
        Assert.Contains("Estimated price can't be negative.", validator);
        Assert.Contains("OnFieldChanged", validator);
    }

    [Theory]
    [InlineData("WishlistItemForm.razor")]
    [InlineData("WishlistItemModal.razor")]
    public void IdeaFormsValidateInlineAndRevealTheFirstProblem(string component)
    {
        var markup = ReadComponent([.. _wishlistComponents, component]);

        Assert.Contains("<IdeaFieldsValidator />", markup);
        Assert.DoesNotContain("<ValidationSummary", markup);
        Assert.Contains("OnInvalidSubmit=\"@RevealFirstProblem\"", markup);
        Assert.Contains("<ValidationMessage For=\"@(() => Model.Name)\" role=\"alert\" />", markup);
        Assert.Contains("<ValidationMessage For=\"@(() => Model.Price)\" role=\"alert\" />", markup);
        Assert.Contains("inputmode=\"decimal\"", markup);
        Assert.Contains("type=\"url\" inputmode=\"url\"", markup);
        Assert.Contains("<option value=\"\">No priority</option>", markup);
    }

    [Fact]
    public void ItemDialogBecomesAPhoneSheetWithCompactActions()
    {
        var css = ReadComponent([.. _wishlistComponents, "WishlistItemModal.razor.css"]);

        Assert.Contains("@media (max-width: 575.98px)", css);
        Assert.Contains("calc(100dvh - 0.5rem)", css);
        Assert.Contains("env(safe-area-inset-bottom", css);
        Assert.Contains(".modal-footer:has([data-testid=\"add-another-item\"])", css);
        Assert.Contains("prefers-reduced-motion", css);
    }

    [Fact]
    public void ItemDialogFocusesNameWhenEditingAndScrollsProblemsAboveActions()
    {
        var markup = ReadComponent([.. _wishlistComponents, "WishlistItemModal.razor"]);

        Assert.Contains("\"openWishFocusElement\", \"name\"", markup);
        Assert.Contains("\"wishlist-item-name-field\" : \"wishlist-item-price-field\"", markup);
        Assert.Contains("id=\"wishlist-item-save-error\"", markup);
    }

    [Fact]
    public void AddItemPageShowsSaveErrorsBesideTheActions()
    {
        var page = ReadComponent("OpenWish.Web.Client", "Components", "Pages", "Wishlists", "AddItem.razor");

        Assert.True(
            page.IndexOf("role=\"alert\">@_errorMessage", StringComparison.Ordinal)
                > page.IndexOf("<WishlistItemForm", StringComparison.Ordinal),
            "The save error should render after the form so it appears beside the submit buttons.");
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