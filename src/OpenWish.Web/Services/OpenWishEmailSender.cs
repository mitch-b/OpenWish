using FluentEmail.Core;
using Microsoft.AspNetCore.Identity;
using OpenWish.Data.Entities;

namespace OpenWish.Web.Services;

public class OpenWishEmailSender(ILogger<OpenWishEmailSender> logger, IFluentEmail fluentEmail) : IEmailSender<ApplicationUser>
{
    private readonly ILogger _logger = logger;
    private readonly IFluentEmail _fluentEmail = fluentEmail;

    public Task SendConfirmationLinkAsync(ApplicationUser user, string email, string confirmationLink) =>
        SendEmailAsync(email, "Confirm your email",
            WrapInHtmlFormattedEmail($"Please confirm your account by <a href='{confirmationLink}'>clicking here</a>."));

    public Task SendPasswordResetCodeAsync(ApplicationUser user, string email, string resetCode) =>
        SendEmailAsync(email, "Reset your password",
            WrapInHtmlFormattedEmail($"Please reset your password using the following code: {resetCode}"));

    public Task SendPasswordResetLinkAsync(ApplicationUser user, string email, string resetLink) =>
        SendEmailAsync(email, "Reset your password",
            WrapInHtmlFormattedEmail($"Please reset your password by <a href='{resetLink}'>clicking here</a>."));

    public async Task SendEmailAsync(string toEmail, string subject, string message)
    {
        var response = await _fluentEmail
            .To(toEmail)
            .Subject(subject)
            .Body(WrapInHtmlFormattedEmail(message), true)
            .SendAsync();

        _logger.LogInformation(response.Successful
                               ? $"Email to {toEmail} queued successfully!"
                               : $"Failure sending email to {toEmail}");
    }

    private string WrapInHtmlFormattedEmail(string message)
    {
        return $"<html><body>{message}</body></html>";
    }
}