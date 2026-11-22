using Xunit;

namespace OpenWish.Shared.Tests.Ux;

public class AuthPagesMarkupTests
{
    [Fact]
    public void Login_UsesVisibleStackedLabelsAndConsistentSignInCopy()
    {
        var markup = ReadAccountPage("Login.razor");

        Assert.DoesNotContain("form-floating", markup, StringComparison.Ordinal);
        Assert.Contains("<label for=\"Input.Email\" class=\"form-label\">", markup, StringComparison.Ordinal);
        Assert.Contains("<label for=\"Input.Password\" class=\"form-label\">", markup, StringComparison.Ordinal);
        Assert.DoesNotContain("placeholder=\"Please enter your password\"", markup, StringComparison.Ordinal);
        Assert.Contains(">Sign in</button>", markup, StringComparison.Ordinal);
        Assert.DoesNotContain(">Log in<", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void LoginAndRegister_OfferTheOtherPathOutsideTheForm()
    {
        var login = ReadAccountPage("Login.razor");
        var register = ReadAccountPage("Register.razor");

        var loginSwitch = login[login.IndexOf("<p class=\"auth-switch\">", StringComparison.Ordinal)..];
        Assert.True(login.IndexOf("</section>", StringComparison.Ordinal) < login.IndexOf("<p class=\"auth-switch\">", StringComparison.Ordinal));
        Assert.Contains("Create an account", loginSwitch, StringComparison.Ordinal);
        Assert.Contains("data-carry-guest-access", loginSwitch, StringComparison.Ordinal);

        var registerSwitchIndex = register.IndexOf("<p class=\"auth-switch\">", StringComparison.Ordinal);
        Assert.True(register.IndexOf("</section>", StringComparison.Ordinal) < registerSwitchIndex);
        Assert.Contains("data-carry-guest-access", register[registerSwitchIndex..], StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("Lockout.razor", "Account/ForgotPassword")]
    [InlineData("InvalidPasswordReset.razor", "Account/ForgotPassword")]
    [InlineData("ForgotPasswordConfirmation.razor", "Account/Login")]
    [InlineData("ResetPasswordConfirmation.razor", "Account/Login")]
    [InlineData("AccessDenied.razor", "href=\"/\"")]
    [InlineData("InvalidUser.razor", "Account/Login")]
    public void StatusPages_UseTheAuthLayoutAndOfferANextStep(string page, string nextStep)
    {
        var markup = ReadAccountPage(page);

        Assert.Contains("class=\"auth-page auth-page-compact\"", markup, StringComparison.Ordinal);
        Assert.Contains("class=\"auth-heading\"", markup, StringComparison.Ordinal);
        var actions = markup[markup.IndexOf("class=\"auth-actions\"", StringComparison.Ordinal)..];
        Assert.Contains(nextStep, actions, StringComparison.Ordinal);
    }

    [Fact]
    public void LoginWith2fa_LabelsTheRememberCheckboxAndHintsOneTimeCodes()
    {
        var markup = ReadAccountPage("LoginWith2fa.razor");

        Assert.Contains("id=\"remember-machine\"", markup, StringComparison.Ordinal);
        Assert.Contains("for=\"remember-machine\"", markup, StringComparison.Ordinal);
        Assert.Contains("autocomplete=\"one-time-code\"", markup, StringComparison.Ordinal);
        Assert.Contains("inputmode=\"numeric\"", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void ConfirmEmailChange_FailuresRenderAsErrors()
    {
        var markup = ReadAccountPage("ConfirmEmailChange.razor");

        Assert.Contains("\"Error: We couldn't find the account for this link.\"", markup, StringComparison.Ordinal);
        Assert.DoesNotContain("\"Unable to find user", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void ExternalProviderButton_IsSecondaryToTheEmailSubmit()
    {
        var picker = ReadAccountShared("ExternalLoginPicker.razor");

        Assert.Contains("class=\"btn btn-lg external-login-button\"", picker, StringComparison.Ordinal);
        Assert.DoesNotContain("btn-primary", picker, StringComparison.Ordinal);
    }

    [Fact]
    public void StatusMessage_PairsTheMessageWithAnIcon()
    {
        var markup = ReadAccountShared("StatusMessage.razor");

        Assert.Contains("auth-status\" role=\"alert\"", markup, StringComparison.Ordinal);
        Assert.Contains("<OpenWishIcon Name=\"@(isError ? \"exclamation-circle\" : \"check-circle\")\" />", markup, StringComparison.Ordinal);
        Assert.Contains("<span>@visibleMessage</span>", markup, StringComparison.Ordinal);
        Assert.Contains("<span class=\"visually-hidden\">Error: </span>", markup, StringComparison.Ordinal);
    }

    private static string ReadAccountPage(string fileName) =>
        ReadComponent("OpenWish.Web", "Components", "Account", "Pages", fileName);

    private static string ReadAccountShared(string fileName) =>
        ReadComponent("OpenWish.Web", "Components", "Account", "Shared", fileName);

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