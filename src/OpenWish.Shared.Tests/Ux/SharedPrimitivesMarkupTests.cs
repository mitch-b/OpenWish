using Xunit;

namespace OpenWish.Shared.Tests.Ux;

public class SharedPrimitivesMarkupTests
{
    [Fact]
    public void Dialog_ScrollsTallContentWithStickyFooterAndTouchSizedClose()
    {
        var markup = Read("OpenWish.Web.Client", "Components", "Shared", "Dialog.razor");
        var css = Read("OpenWish.Web.Client", "Components", "Shared", "Dialog.razor.css");

        Assert.Contains("<OpenWishIcon Name=\"x-lg\" />", markup, StringComparison.Ordinal);
        Assert.DoesNotContain("&times;", markup, StringComparison.Ordinal);
        Assert.Contains("overflow-y: auto;", css, StringComparison.Ordinal);
        Assert.Contains("position: sticky;", css, StringComparison.Ordinal);
        Assert.Contains("max-height: 100%;", css, StringComparison.Ordinal);
        Assert.Contains("env(safe-area-inset-bottom)", css, StringComparison.Ordinal);
        Assert.Contains("width: 2.75rem;", css, StringComparison.Ordinal);
    }

    [Fact]
    public void PageHeader_LetsLongTitlesWrapBesideActions()
    {
        var markup = Read("OpenWish.Web.Client", "Components", "Shared", "PageHeader.razor");
        var css = Read("OpenWish.Web.Client", "Components", "Shared", "PageHeader.razor.css");

        Assert.Contains("class=\"page-header-text\"", markup, StringComparison.Ordinal);
        Assert.Contains("min-width: 0;", css, StringComparison.Ordinal);
        Assert.Contains("overflow-wrap: anywhere;", css, StringComparison.Ordinal);
    }

    [Fact]
    public void MissingPages_RenderFriendlyNotFoundInsteadOfBlankResponse()
    {
        var routes = Read("OpenWish.Web", "Components", "Routes.razor");
        var program = Read("OpenWish.Web", "Program.cs");
        var notFound = Read("OpenWish.Web", "Components", "Pages", "NotFound.razor");

        Assert.Contains("NotFoundPage=\"typeof(Pages.NotFound)\"", routes, StringComparison.Ordinal);
        Assert.Contains("UseStatusCodePagesWithReExecute(\"/not-found\"", program, StringComparison.Ordinal);
        Assert.Contains("!context.Request.Path.StartsWithSegments(\"/api\")", program, StringComparison.Ordinal);
        Assert.Contains("@page \"/not-found\"", notFound, StringComparison.Ordinal);
        Assert.Contains("We couldn't find that page", notFound, StringComparison.Ordinal);
        Assert.Contains("Go to dashboard", notFound, StringComparison.Ordinal);
    }

    [Fact]
    public void ErrorPage_OffersRecoveryWithoutDeveloperDiagnostics()
    {
        var error = Read("OpenWish.Web", "Components", "Pages", "Error.razor");

        Assert.DoesNotContain("Development Mode", error, StringComparison.Ordinal);
        Assert.DoesNotContain("ASPNETCORE_ENVIRONMENT", error, StringComparison.Ordinal);
        Assert.Contains("Try again", error, StringComparison.Ordinal);
        Assert.Contains("Go to dashboard", error, StringComparison.Ordinal);
    }

    [Fact]
    public void GlobalStyles_KeepFocusKeyboardOnlyAndControlsUsableInDarkModeAndOnTouch()
    {
        var css = Read("OpenWish.Web", "wwwroot", "app.css");

        Assert.Contains(".btn:focus-visible", css, StringComparison.Ordinal);
        Assert.DoesNotContain(".btn:focus,", css, StringComparison.Ordinal);
        Assert.Contains(":root[data-theme='dark'] .btn-close", css, StringComparison.Ordinal);
        Assert.Contains(":root[data-theme='dark'] .form-select", css, StringComparison.Ordinal);
        Assert.Contains("@media (pointer: coarse)", css, StringComparison.Ordinal);
        Assert.Contains(".content .badge.bg-warning", css, StringComparison.Ordinal);
    }

    private static string Read(params string[] pathParts)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "OpenWish.slnx")))
        {
            directory = directory.Parent;
        }

        var root = directory?.FullName ?? throw new DirectoryNotFoundException("Could not locate OpenWish.slnx.");
        return File.ReadAllText(Path.Combine([root, .. pathParts]));
    }
}