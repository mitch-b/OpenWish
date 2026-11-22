using Xunit;

namespace OpenWish.Shared.Tests.Ux;

public class WishlistFormMarkupTests
{
    private static readonly string[] _wishlistComponents = ["OpenWish.Web.Client", "Components", "Wishlist"];
    private static readonly string[] _wishlistPages = ["OpenWish.Web.Client", "Components", "Pages", "Wishlists"];

    [Fact]
    public void WishlistFormRejectsBlankNamesInlineWithoutADuplicateSummary()
    {
        var form = ReadComponent([.. _wishlistComponents, "WishlistForm.razor"]);
        var validator = ReadComponent([.. _wishlistComponents, "WishlistFormValidator.cs"]);

        Assert.Contains("<WishlistFormValidator />", form);
        Assert.DoesNotContain("<ValidationSummary", form);
        Assert.Contains("aria-describedby=\"wishlist-name-help\"", form);
        Assert.Contains("Enter a name for this wishlist.", validator);
        Assert.Contains("string.IsNullOrWhiteSpace", validator);
        Assert.Contains("OnFieldChanged", validator);
    }

    [Fact]
    public void WishlistFormControlsWaitForHydration()
    {
        var form = ReadComponent([.. _wishlistComponents, "WishlistForm.razor"]);

        Assert.Contains("disabled=\"@(IsSubmitting || !RendererInfo.IsInteractive)\"", form);
        Assert.True(CountOccurrences(form, "!RendererInfo.IsInteractive") >= 4);
    }

    [Fact]
    public void NewWishlistTrimsTheNameAndKeepsActionsReachableOnPhones()
    {
        var page = ReadComponent([.. _wishlistPages, "NewWishlist.razor"]);
        var css = ReadComponent([.. _wishlistPages, "NewWishlist.razor.css"]);

        Assert.Contains("submittedWishlist.Name = submittedWishlist.Name.Trim();", page);
        Assert.Contains("::deep .wishlist-form-actions", css);
        Assert.Contains("position: sticky;", css);
        Assert.Contains("env(safe-area-inset-bottom)", css);
    }

    [Fact]
    public void ManageWishlistSaveShowsBusySuccessAndFailure()
    {
        var page = ReadComponent([.. _wishlistPages, "ManageWishlist.razor"]);

        Assert.Contains("IsSubmitting=\"@_savingDetails\"", page);
        Assert.Contains("<StatusContent>", page);
        Assert.Contains("_detailsStatusMessage = \"Changes saved.\";", page);
        Assert.Contains("We couldn't save your changes. Check your connection and try again.", page);
        Assert.Contains("updatedWishlist.Name = updatedWishlist.Name.Trim();", page);
        Assert.Contains("_savingDetails = false;", page);
    }

    [Fact]
    public void ManageWishlistDeleteStaysOpenWhileBusyAndExplainsFailures()
    {
        var page = ReadComponent([.. _wishlistPages, "ManageWishlist.razor"]);

        Assert.Contains("CloseOnConfirm=\"false\"", page);
        Assert.Contains("IsBusy=\"@_isDeleting\"", page);
        Assert.Contains("ErrorMessage=\"@_deleteErrorMessage\"", page);
        Assert.Contains("return deleteDialog.ShowAsync(", page);
        Assert.Contains("\"Delete wishlist\",", page);
        Assert.Contains("\"manage-wishlist-delete\"", page);
        Assert.Contains("We couldn't delete this wishlist. Check your connection and try again.", page);
        Assert.DoesNotContain("Are you sure", page);
    }

    [Fact]
    public void PrivateListsExplainWhyTheyCannotJoinAnEvent()
    {
        var page = ReadComponent([.. _wishlistPages, "ManageWishlist.razor"]);

        var privateNote = page.IndexOf("else if (_eventLoadErrorMessage is null && _wishlist.IsPrivate)", StringComparison.Ordinal);
        var attachForm = page.IndexOf("else if (_eventLoadErrorMessage is null && _availableEvents.Any())", StringComparison.Ordinal);

        Assert.True(privateNote >= 0);
        Assert.True(privateNote < attachForm, "The private note must replace the attach form for private lists.");
        Assert.Contains("data-testid=\"private-event-note\"", page);
        Assert.Contains("Create an event", page);
    }

    [Fact]
    public void SharingPanelSummarizesVisibilityAndLinksToChangeIt()
    {
        var page = ReadComponent([.. _wishlistPages, "ManageWishlist.razor"]);
        var css = ReadComponent([.. _wishlistPages, "ManageWishlist.razor.css"]);

        Assert.Contains("data-testid=\"sharing-summary\"", page);
        Assert.Contains("Only you can see this list.", page);
        Assert.Contains("href=\"@DetailsSectionHref\"", page);
        Assert.Contains("<ul class=\"friends-grid\"", page);
        Assert.DoesNotContain(".btn-group", css);
        Assert.Contains("@media (prefers-reduced-motion: reduce)", css);
    }

    private static int CountOccurrences(string text, string value)
    {
        var count = 0;
        var index = 0;
        while ((index = text.IndexOf(value, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += value.Length;
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