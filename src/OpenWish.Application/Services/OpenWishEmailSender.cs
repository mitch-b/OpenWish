using FluentEmail.Core;
using Microsoft.Extensions.Logging;

namespace OpenWish.Application.Services;

/// <summary>
/// Handles sending emails for account confirmation, password reset, and friend invitations.
/// </summary>
public class OpenWishEmailSender(ILogger<OpenWishEmailSender> logger, IFluentEmail fluentEmail) : IAppEmailSender
{
    private readonly ILogger _logger = logger;
    private readonly IFluentEmail _fluentEmail = fluentEmail;

    public Task SendConfirmationLinkAsync(string toEmail, string confirmationLink) =>
        SendEmailAsync(toEmail, "Confirm your email",
            WrapInHtmlFormattedEmail($"Please confirm your account by <a href='{confirmationLink}'>clicking here</a>."));

    public Task SendPasswordResetCodeAsync(string toEmail, string resetCode) =>
        SendEmailAsync(toEmail, "Reset your password",
            WrapInHtmlFormattedEmail($"Please reset your password using the following code: {resetCode}"));

    public Task SendPasswordResetLinkAsync(string toEmail, string resetLink) =>
        SendEmailAsync(toEmail, "Reset your password",
            WrapInHtmlFormattedEmail($"Please reset your password by <a href='{resetLink}'>clicking here</a>."));

    /// <summary>
    /// Sends a friend invitation email to a non-registered user.
    /// </summary>
    /// <param name="toEmail">Recipient email address</param>
    /// <param name="inviterName">Name of the user sending the invite</param>
    /// <param name="inviteLink">Registration or invitation link</param>
    /// <returns></returns>
    public Task SendFriendInviteEmailAsync(string toEmail, string inviterName, string inviteLink)
    {
        _logger.LogInformation("Sending friend invite email to {Email} with link {InviteLink}", toEmail, inviteLink);
        var subject = $"{inviterName} invited you to join OpenWish!";
        var body = WrapInHtmlFormattedEmail($"<p>{inviterName} has invited you to join OpenWish to connect and share wishlists!<br/>" +
            $"<a href='{inviteLink}'>Click here to join and connect</a>.</p>");
        return SendEmailAsync(toEmail, subject, body);
    }

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