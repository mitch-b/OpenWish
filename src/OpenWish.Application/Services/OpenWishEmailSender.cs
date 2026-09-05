using System.Text.Encodings.Web;
using FluentEmail.Core;
using FluentEmail.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace OpenWish.Application.Services;

/// <summary>
/// Handles sending emails for account confirmation, password reset, and friend invitations.
/// </summary>
public class OpenWishEmailSender(ILogger<OpenWishEmailSender> logger, IFluentEmailFactory emailFactory) : IAppEmailSender
{
    private readonly ILogger _logger = logger;
    private readonly IFluentEmailFactory _emailFactory = emailFactory;

    public Task SendConfirmationLinkAsync(string toEmail, string confirmationLink) =>
        SendEmailAsync(toEmail, "Confirm your email",
            WrapInHtmlFormattedEmail($"Please confirm your account by <a href='{Encode(confirmationLink)}'>clicking here</a>."));

    public Task SendPasswordResetCodeAsync(string toEmail, string resetCode) =>
        SendEmailAsync(toEmail, "Reset your password",
            WrapInHtmlFormattedEmail($"Please reset your password using the following code: {Encode(resetCode)}"));

    public Task SendPasswordResetLinkAsync(string toEmail, string resetLink) =>
        SendEmailAsync(toEmail, "Reset your password",
            WrapInHtmlFormattedEmail($"Please reset your password by <a href='{Encode(resetLink)}'>clicking here</a>."));

    /// <summary>
    /// Sends a friend invitation email to a non-registered user.
    /// </summary>
    /// <param name="toEmail">Recipient email address</param>
    /// <param name="inviterName">Name of the user sending the invite</param>
    /// <param name="inviteLink">Registration or invitation link</param>
    /// <returns></returns>
    public Task SendFriendInviteEmailAsync(string toEmail, string inviterName, string inviteLink)
    {
        _logger.LogInformation("Sending friend invite email.");
        var safeInviterName = Encode(inviterName);
        var subject = $"{SanitizeSubject(inviterName)} invited you to join OpenWish!";
        var body = WrapInHtmlFormattedEmail($"<p>{safeInviterName} has invited you to join OpenWish to connect and share wishlists!<br/>" +
            $"<a href='{Encode(inviteLink)}'>Click here to join and connect</a>.</p>");
        return SendEmailAsync(toEmail, subject, body);
    }

    /// <summary>
    /// Sends an event invitation email.
    /// </summary>
    /// <param name="toEmail">Recipient email address</param>
    /// <param name="inviterName">Name of the user sending the invite</param>
    /// <param name="eventName">Name of the event</param>
    /// <param name="inviteLink">Event invitation link</param>
    /// <returns></returns>
    public Task SendEventInviteEmailAsync(string toEmail, string inviterName, string eventName, string inviteLink)
    {
        _logger.LogInformation("Sending event invite email for event {EventName}", eventName);
        var subject = $"{SanitizeSubject(inviterName)} invited you to {SanitizeSubject(eventName)}!";
        var body = WrapInHtmlFormattedEmail($"<p>{Encode(inviterName)} has invited you to join the event <strong>{Encode(eventName)}</strong> on OpenWish.<br/>" +
            $"<a href='{Encode(inviteLink)}'>Click here to view and accept the invitation</a>.</p>");
        return SendEmailAsync(toEmail, subject, body);
    }

    /// <summary>
    /// Sends a gift exchange name drawn email notification.
    /// </summary>
    /// <param name="toEmail">Recipient email address</param>
    /// <param name="eventName">Name of the event</param>
    /// <param name="recipientName">Name of the person they're buying for</param>
    /// <param name="eventLink">Link to event details</param>
    /// <returns></returns>
    public Task SendGiftExchangeDrawnEmailAsync(string toEmail, string eventName, string recipientName, string eventLink)
    {
        _logger.LogInformation("Sending gift exchange drawn email for event {EventName}", eventName);
        var subject = $"🎁 Gift Exchange Names Drawn - {SanitizeSubject(eventName)}";
        eventName = Encode(eventName);
        recipientName = Encode(recipientName);
        eventLink = Encode(eventLink);
        var body = WrapInHtmlFormattedEmail($"<div style='font-family: Arial, sans-serif; padding: 20px;'>" +
            $"<h2 style='color: #28a745;'>🎁 Gift Exchange Names Have Been Drawn!</h2>" +
            $"<p>Great news! The names have been drawn for <strong>{eventName}</strong>.</p>" +
            $"<div style='background-color: #f8f9fa; border-left: 4px solid #28a745; padding: 15px; margin: 20px 0;'>" +
            $"<h3 style='margin-top: 0;'>Your Gift Recipient:</h3>" +
            $"<p style='font-size: 18px; margin: 10px 0;'><strong>{recipientName}</strong></p>" +
            $"</div>" +
            $"<p><a href='{eventLink}' style='background-color: #28a745; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px; display: inline-block;'>View Event Details & Wishlist</a></p>" +
            $"<p style='color: #6c757d; font-size: 14px;'>Visit the event page to see {recipientName}'s wishlist and start planning the perfect gift!</p>" +
            $"</div>");
        return SendEmailAsync(toEmail, subject, body);
    }

    /// <summary>
    /// Sends a gift exchange reset/cancelled email notification.
    /// </summary>
    /// <param name="toEmail">Recipient email address</param>
    /// <param name="eventName">Name of the event</param>
    /// <param name="eventLink">Link to event details</param>
    /// <returns></returns>
    public Task SendGiftExchangeResetEmailAsync(string toEmail, string eventName, string eventLink)
    {
        _logger.LogInformation("Sending gift exchange reset email for event {EventName}", eventName);
        var subject = $"Gift Exchange Reset - {SanitizeSubject(eventName)}";
        eventName = Encode(eventName);
        eventLink = Encode(eventLink);
        var body = WrapInHtmlFormattedEmail($"<div style='font-family: Arial, sans-serif; padding: 20px;'>" +
            $"<h2 style='color: #dc3545;'>Gift Exchange Has Been Reset</h2>" +
            $"<p>The gift exchange for <strong>{eventName}</strong> has been reset by the event organizer.</p>" +
            $"<div style='background-color: #fff3cd; border-left: 4px solid #ffc107; padding: 15px; margin: 20px 0;'>" +
            $"<p style='margin: 0;'>All previous gift assignments have been cancelled. If names are drawn again, you will receive a new assignment.</p>" +
            $"</div>" +
            $"<p><a href='{eventLink}' style='background-color: #007bff; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px; display: inline-block;'>View Event Details</a></p>" +
            $"</div>");
        return SendEmailAsync(toEmail, subject, body);
    }

    public async Task SendEmailAsync(string toEmail, string subject, string message)
    {
        var response = await _emailFactory
            .Create()
            .To(toEmail)
            .Subject(subject)
            .Body(message, true)
            .SendAsync();

        _logger.LogInformation(response.Successful
                               ? "Email queued successfully."
                               : "Email delivery failed.");
    }

    private string WrapInHtmlFormattedEmail(string message)
    {
        return $"<html><body>{message}</body></html>";
    }

    private static string Encode(string value) => HtmlEncoder.Default.Encode(value);

    private static string SanitizeSubject(string value) =>
        value.Replace("\r", string.Empty, StringComparison.Ordinal)
            .Replace("\n", string.Empty, StringComparison.Ordinal);
}