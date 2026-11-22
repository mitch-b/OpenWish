using Xunit;

namespace OpenWish.Shared.Tests.Ux;

public class ConnectedAppsMarkupTests
{
    [Fact]
    public void Consent_ListsWhatTheAppCanDoAndWhatItNeverSees()
    {
        var consent = ReadComponent("OpenWish.Web", "Components", "Pages", "Integrations", "ConnectApp.razor");

        Assert.Contains("@(consent.IsReconnecting ? \"Reconnect\" : \"Connect\") @app.DisplayName", consent, StringComparison.Ordinal);
        Assert.Contains("<ul class=\"consent-can\"", consent, StringComparison.Ordinal);
        Assert.Contains("class=\"consent-never\" role=\"group\" aria-labelledby=\"consent-never-title\"", consent, StringComparison.Ordinal);
        Assert.Contains("@app.DisplayName never sees", consent, StringComparison.Ordinal);
        Assert.Contains("Reservations, purchases, or comments", consent, StringComparison.Ordinal);
        Assert.Contains("Hidden surprises others add for you", consent, StringComparison.Ordinal);
        Assert.Contains("Lists you haven't allowed", consent, StringComparison.Ordinal);
    }

    [Fact]
    public void Consent_HidesTheNewListNameUntilRequestedAndUsesLargeToggleRows()
    {
        var consent = ReadComponent("OpenWish.Web", "Components", "Pages", "Integrations", "ConnectApp.razor");
        var css = ReadComponent("OpenWish.Web", "Components", "Pages", "Integrations", "ConnectApp.razor.css");

        Assert.Contains("aria-controls=\"consent-new-list-field\"", consent, StringComparison.Ordinal);
        Assert.Contains("id=\"consent-new-list-field\"", consent, StringComparison.Ordinal);
        Assert.Contains(".consent-new-list:has(#consent-new-list:not(:checked)) .consent-new-list-field", css, StringComparison.Ordinal);
        Assert.Contains("form-check consent-toggle", consent, StringComparison.Ordinal);
        Assert.Contains("min-height: 2.75rem;", css, StringComparison.Ordinal);
    }

    [Fact]
    public void ConnectionRows_GroupFactsAndActionsAndFlagUnverifiedAssistants()
    {
        var page = ReadComponent("OpenWish.Web", "Components", "Account", "Pages", "Manage", "ConnectedApps.razor");
        var css = ReadComponent("OpenWish.Web", "Components", "Account", "Pages", "Manage", "ConnectedApps.razor.css");

        Assert.Contains("<dl class=\"connected-app-routing\">", page, StringComparison.Ordinal);
        Assert.Contains("<dt>Ideas added</dt>", page, StringComparison.Ordinal);
        Assert.Contains("<div class=\"connected-app-row-actions\">", page, StringComparison.Ordinal);
        Assert.Contains("connected-app-unverified\"><OpenWishIcon Name=\"exclamation-triangle\" />", page, StringComparison.Ordinal);
        Assert.Contains("form-check connected-app-toggle", page, StringComparison.Ordinal);
        Assert.Contains("<legend>What it can do</legend>", page, StringComparison.Ordinal);
        Assert.Contains("@connection.AppName never sees reservations, purchases, comments", page, StringComparison.Ordinal);
        Assert.Contains(".connected-app-toggle .form-check-label {", css, StringComparison.Ordinal);
    }

    [Fact]
    public void WishlistPicker_UsesDotSeparatorAndLargeCheckboxes()
    {
        var picker = ReadComponent("OpenWish.Web", "Components", "Shared", "IntegrationWishlistPicker.razor");
        var css = ReadComponent("OpenWish.Web", "Components", "Shared", "IntegrationWishlistPicker.razor.css");

        Assert.Contains("$\" \u00b7 {wishlist.EventName}\"", picker, StringComparison.Ordinal);
        Assert.DoesNotContain("$\" - {wishlist.EventName}\"", picker, StringComparison.Ordinal);
        Assert.Contains(".integration-picker-selected .form-check-input,", css, StringComparison.Ordinal);
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