using Xunit;

namespace OpenWish.Shared.Tests.Ux;

public class AccountSettingsMarkupTests
{
    [Theory]
    [InlineData("Index.razor", "Save profile")]
    [InlineData("Email.razor", "Change email")]
    [InlineData("ChangePassword.razor", "Update password")]
    [InlineData("SetPassword.razor", "Set password")]
    public void SaveButtons_UseNormalWidthOnDesktopLikeGiftHints(string page, string label)
    {
        var markup = Read("Pages", "Manage", page);

        Assert.Contains($"class=\"btn btn-lg btn-primary account-submit\">{label}</button>", markup, StringComparison.Ordinal);
        Assert.DoesNotContain("w-100 btn btn-lg", markup, StringComparison.Ordinal);
        Assert.Contains("<div class=\"account-form\">", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Layout_SizesSubmitAndActionButtonsForPhones()
    {
        var styles = Read("Shared", "ManageLayout.razor.css");
        var phone = styles[styles.IndexOf("@media (max-width: 640px)", StringComparison.Ordinal)..];

        Assert.Contains("min-height: 2.75rem;", styles, StringComparison.Ordinal);
        Assert.Contains(".account-content ::deep .account-submit,", phone, StringComparison.Ordinal);
        Assert.Contains("width: 100%;", phone, StringComparison.Ordinal);
        Assert.Contains("grid-template-columns: repeat(2, minmax(0, 1fr));", phone, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("Pages", "Manage", "Index.razor", "<h2>Profile</h2>")]
    [InlineData("Pages", "Manage", "Email.razor", "<h2>Email</h2>")]
    [InlineData("Pages", "Manage", "TwoFactorAuthentication.razor", "<h2>Two-factor authentication (2FA)</h2>")]
    [InlineData("Pages", "Manage", "EnableAuthenticator.razor", "<h2>Set up authenticator app</h2>")]
    [InlineData("Shared", "ShowRecoveryCodes.razor", null, "<h2>Save your recovery codes</h2>")]
    public void Pages_ShareOneHeadingLevelUnderTheAccountTitle(string folder, string a, string? b, string heading)
    {
        var markup = b is null ? Read(folder, a) : Read(folder, a, b);

        Assert.Contains(heading, markup, StringComparison.Ordinal);
        Assert.DoesNotContain("<h4", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void FieldHelp_StaysWithItsField()
    {
        var profile = Read("Pages", "Manage", "Index.razor");
        var email = Read("Pages", "Manage", "Email.razor");

        Assert.Equal(2, profile.Split("<div class=\"account-field\">", StringSplitOptions.None).Length - 1);
        Assert.Contains("<div class=\"account-field\">", email, StringComparison.Ordinal);
        Assert.DoesNotContain("form-text mb-3", profile, StringComparison.Ordinal);
        Assert.Contains("<OpenWishIcon Name=\"exclamation-circle\" />", email, StringComparison.Ordinal);
        Assert.DoesNotContain("class=\"text-warning mb-2\"", email, StringComparison.Ordinal);
    }

    [Fact]
    public void DeleteAccount_ButtonsDoNotStretchAcrossDesktop()
    {
        var markup = Read("Pages", "Manage", "DeletePersonalData.razor");

        Assert.DoesNotContain("flex-fill", markup, StringComparison.Ordinal);
        Assert.Contains("account-actions", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void ExternalLogins_UseSentenceCaseAndNameTheRemovedService()
    {
        var markup = Read("Pages", "Manage", "ExternalLogins.razor");

        Assert.Contains("<h2>External logins</h2>", markup, StringComparison.Ordinal);
        Assert.Contains("Add another sign-in service", markup, StringComparison.Ordinal);
        Assert.DoesNotContain("Registered Logins", markup, StringComparison.Ordinal);
        Assert.Contains("class=\"btn btn-outline-danger\" aria-label=\"Remove @login.ProviderDisplayName login\"", markup, StringComparison.Ordinal);
        Assert.Contains("Your only way to sign in", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void PersonalData_GroupsTasksIntoLabelledSections()
    {
        var markup = Read("Pages", "Manage", "PersonalData.razor");

        Assert.Contains("aria-labelledby=\"wishlist-import-heading\"", markup, StringComparison.Ordinal);
        Assert.Contains("aria-labelledby=\"delete-account-heading\"", markup, StringComparison.Ordinal);
        Assert.DoesNotContain("style=\"width: 22rem\"", markup, StringComparison.Ordinal);
    }

    private static string Read(params string[] segments)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "OpenWish.slnx")))
        {
            directory = directory.Parent;
        }

        var root = directory?.FullName
            ?? throw new DirectoryNotFoundException("Could not locate OpenWish.slnx.");
        return File.ReadAllText(Path.Combine([root, "OpenWish.Web", "Components", "Account", .. segments]));
    }
}