using System.Net.Mail;
using System.Text.Encodings.Web;
using FluentEmail.Core;
using FluentEmail.Core.Interfaces;
using FluentEmail.Core.Models;
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
            BuildEmail(
                preheader: "Confirm your email to finish setting up OpenWish.",
                title: "Confirm your email",
                bodyHtml: Paragraph("Confirm this address to finish setting up your OpenWish account."),
                actionText: "Confirm my email",
                actionUrl: confirmationLink,
                footnoteHtml: "If you didn't create an OpenWish account, you can ignore this email."));

    public Task SendPasswordResetCodeAsync(string toEmail, string resetCode) =>
        SendEmailAsync(toEmail, "Reset your password",
            BuildEmail(
                preheader: "Your OpenWish password reset code.",
                title: "Reset your password",
                bodyHtml: Paragraph("Use this code to reset your password:") + Highlight("Reset code", Encode(resetCode)),
                footnoteHtml: "If you didn't ask to reset your password, you can ignore this email."));

    public Task SendPasswordResetLinkAsync(string toEmail, string resetLink) =>
        SendEmailAsync(toEmail, "Reset your password",
            BuildEmail(
                preheader: "Choose a new password for OpenWish.",
                title: "Reset your password",
                bodyHtml: Paragraph("Someone asked to reset the password for this OpenWish account. Choose a new password to continue."),
                actionText: "Choose a new password",
                actionUrl: resetLink,
                footnoteHtml: "If you didn't ask to reset your password, you can ignore this email."));

    /// <summary>
    /// Sends a friend invitation email to a non-registered user.
    /// </summary>
    /// <param name="toEmail">Recipient email address</param>
    /// <param name="inviterName">Name of the user sending the invite</param>
    /// <param name="inviteLink">Registration or invitation link</param>
    /// <returns></returns>
    public async Task SendFriendInviteEmailAsync(string toEmail, string inviterName, string inviteLink)
    {
        _logger.LogInformation("Sending friend invite email.");
        var safeInviterName = Encode(inviterName);
        var subject = $"{SanitizeSubject(inviterName)} invited you to join OpenWish";
        var body = BuildEmail(
            preheader: $"{inviterName} wants to share wishlists with you on OpenWish.",
            title: $"{safeInviterName} invited you to OpenWish",
            bodyHtml: Paragraph($"{safeInviterName} has invited you to join OpenWish to connect and share wishlists."),
            actionText: "Join and connect",
            actionUrl: inviteLink,
            footnoteHtml: "OpenWish keeps gift ideas, reservations, and surprises in one place.");
        try
        {
            if (!await TrySendEmailAsync(toEmail, subject, body))
            {
                throw new HttpRequestException("Friend invitation email could not be delivered.");
            }
        }
        catch (SmtpException ex)
        {
            throw new HttpRequestException("Friend invitation email could not be delivered.", ex);
        }
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
        var subject = $"{SanitizeSubject(inviterName)} invited you to {SanitizeSubject(eventName)}";
        var body = BuildEmail(
            preheader: $"{inviterName} invited you to {eventName}.",
            eyebrow: "Event invitation",
            title: Encode(eventName),
            bodyHtml: Paragraph($"<strong>{Encode(inviterName)}</strong> has invited you to join <strong>{Encode(eventName)}</strong> on OpenWish."),
            actionText: "View and accept the invitation",
            actionUrl: inviteLink,
            footnoteHtml: "Sign in or create an account with this email address and we'll bring you straight back to the invitation.");
        return SendEmailAsync(toEmail, subject, body);
    }

    /// <summary>
    /// Sends a gift exchange name drawn email notification.
    /// </summary>
    /// <param name="toEmail">Recipient email address</param>
    /// <param name="eventName">Name of the event</param>
    /// <param name="recipientName">Name of the person they're buying for, or null to preserve the gift-box reveal</param>
    /// <param name="eventLink">Link to event details</param>
    /// <returns></returns>
    public Task SendGiftExchangeDrawnEmailAsync(string toEmail, string eventName, string? recipientName, string eventLink)
    {
        _logger.LogInformation("Sending gift exchange drawn email for event {EventName}", eventName);
        if (recipientName is null)
        {
            return SendEmailAsync(toEmail, $"Your gift exchange match is ready - {SanitizeSubject(eventName)}",
                BuildEmail(
                    preheader: $"Names have been drawn for {eventName}.",
                    eyebrow: "Names drawn",
                    title: "Your match is ready",
                    bodyHtml: Paragraph($"The names have been drawn for <strong>{Encode(eventName)}</strong>.") +
                        Paragraph("Open your gift to see who you're shopping for."),
                    actionText: "Open your gift",
                    actionUrl: eventLink,
                    footnoteHtml: "Your match is private. Only you can see who you're shopping for."));
        }

        var subject = $"Gift exchange names drawn - {SanitizeSubject(eventName)}";
        var safeRecipientName = Encode(recipientName);
        var body = BuildEmail(
            preheader: $"Names have been drawn for {eventName}.",
            eyebrow: "Names drawn",
            title: "Your match is ready",
            bodyHtml: Paragraph($"The names have been drawn for <strong>{Encode(eventName)}</strong>.") +
                Highlight("Your gift recipient", safeRecipientName),
            actionText: "View event and wishlist",
            actionUrl: eventLink,
            footnoteHtml: $"Visit the event to see {safeRecipientName}'s wishlist and start planning the perfect gift.");
        return SendEmailAsync(toEmail, subject, body);
    }

    public async Task SendGuestEventAccessEmailAsync(string toEmail, string eventName, string accessLink, bool matchReady = false,
        string? accountLink = null, string? inviterName = null)
    {
        var offersChoice = !matchReady && accountLink is not null;
        var subject = matchReady
            ? $"Your private match is ready - {SanitizeSubject(eventName)}"
            : inviterName is null
                ? $"Your private invitation - {SanitizeSubject(eventName)}"
                : $"{SanitizeSubject(inviterName)} invited you to {SanitizeSubject(eventName)}";
        var introduction = matchReady
            ? Paragraph($"The names have been drawn for <strong>{Encode(eventName)}</strong>. Your own match is ready.")
            : offersChoice
                ? Paragraph(inviterName is null
                      ? $"You're invited to <strong>{Encode(eventName)}</strong>."
                      : $"<strong>{Encode(inviterName)}</strong> invited you to <strong>{Encode(eventName)}</strong>.") +
                  Paragraph("Join with an OpenWish account to share your own wishlist and keep everything in one place, or continue without an account. Either way works.")
                : Paragraph($"You're invited to <strong>{Encode(eventName)}</strong>. No account is needed.");
        const string privacyNote = "Keep this email private: anyone you forward it to can use its links. " +
            "The links expire in 30 days. Ask the organizer for a fresh email if they expire or stop working.";
        var body = offersChoice
            ? BuildEmail(
                preheader: $"You're invited to {eventName}. Join with an account or continue without one.",
                eyebrow: "Event invitation",
                title: Encode(eventName),
                bodyHtml: introduction,
                actionText: "Join with an OpenWish account",
                actionUrl: accountLink,
                secondaryLeadHtml: "Rather not make an account? You can still accept and see your match.",
                secondaryActionText: "Continue without an account",
                secondaryActionUrl: accessLink,
                footnoteHtml: privacyNote)
            : BuildEmail(
                preheader: matchReady ? $"Your private match for {eventName} is ready." : $"You're invited to {eventName}.",
                eyebrow: matchReady ? "Names drawn" : "Private invitation",
                title: Encode(eventName),
                bodyHtml: introduction,
                actionText: matchReady ? "View my private match" : "Open my private invitation",
                actionUrl: accessLink,
                footnoteHtml: "This link gives access to your invitation and your own match. Keep it private: anyone you forward it to can use it. " +
                    "The link expires in 30 days. Ask the organizer for a fresh email if it expires or stops working.");
        try
        {
            if (!await TrySendEmailAsync(toEmail, subject, body))
            {
                throw new HttpRequestException("Guest access email could not be delivered. Resend access from the event.");
            }
        }
        catch (SmtpException ex)
        {
            throw new HttpRequestException("Guest access email could not be delivered. Resend access from the event.", ex);
        }
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
        var subject = $"Gift exchange reset - {SanitizeSubject(eventName)}";
        var body = BuildEmail(
            preheader: $"The gift exchange for {eventName} was reset.",
            eyebrow: "Gift exchange reset",
            title: Encode(eventName),
            bodyHtml: Paragraph($"The organizer reset the gift exchange for <strong>{Encode(eventName)}</strong>.") +
                Paragraph("All previous matches have been cancelled. If names are drawn again, you'll get a new match."),
            actionText: "View event",
            actionUrl: eventLink);
        return SendEmailAsync(toEmail, subject, body);
    }

    public async Task SendEmailAsync(string toEmail, string subject, string message)
    {
        await TrySendEmailAsync(toEmail, subject, message);
    }

    private async Task<bool> TrySendEmailAsync(string toEmail, string subject, string message)
    {
        SendResponse response;
        try
        {
            response = await _emailFactory
                .Create()
                .To(toEmail)
                .Subject(subject)
                .Body(message, true)
                .SendAsync();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Email is a best-effort side effect; an unconfigured or failing sender must not fail the request.
            _logger.LogWarning(ex, "Email delivery failed.");
            return false;
        }

        if (!response.Successful)
        {
            _logger.LogError("Email delivery failed.");
            return false;
        }

        _logger.LogInformation("Email queued successfully.");
        return true;
    }

    private const string FontStack = "Manrope, 'Segoe UI', -apple-system, BlinkMacSystemFont, Roboto, Helvetica, Arial, sans-serif";

    private static string Paragraph(string html) =>
        $"<p style=\"margin:0 0 16px;font-size:16px;line-height:1.6;color:#1c2033;\">{html}</p>";

    private static string Highlight(string label, string valueHtml) =>
        "<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\" style=\"margin:4px 0 20px;\"><tr>" +
        "<td style=\"background-color:#eeecff;border-radius:12px;padding:16px 18px;\">" +
        $"<p style=\"margin:0 0 4px;font-size:13px;font-weight:700;color:#5c6475;\">{Encode(label)}</p>" +
        $"<p style=\"margin:0;font-size:20px;font-weight:700;line-height:1.3;color:#1c2033;\">{valueHtml}</p>" +
        "</td></tr></table>";

    /// <summary>
    /// Builds a responsive, inline-styled email that mirrors the OpenWish page layout.
    /// Text arguments other than <paramref name="preheader"/>, <paramref name="actionText"/>,
    /// and <paramref name="actionUrl"/> must already be HTML-encoded.
    /// </summary>
    internal static string BuildEmail(
        string preheader,
        string title,
        string bodyHtml,
        string? eyebrow = null,
        string? actionText = null,
        string? actionUrl = null,
        string? footnoteHtml = null,
        string? secondaryActionText = null,
        string? secondaryActionUrl = null,
        string? secondaryLeadHtml = null)
    {
        var eyebrowHtml = eyebrow is null
            ? string.Empty
            : $"<p style=\"margin:0 0 6px;font-size:14px;font-weight:700;color:#5b4dcc;\">{Encode(eyebrow)}</p>";
        var actionHtml = actionText is null || actionUrl is null
            ? string.Empty
            : "<table role=\"presentation\" class=\"ow-button\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\" style=\"margin:8px 0 8px;\"><tr>" +
              "<td align=\"center\" style=\"border-radius:12px;background-color:#5b4dcc;\">" +
              $"<a href=\"{Encode(actionUrl)}\" style=\"display:inline-block;padding:14px 24px;font-family:{FontStack};font-size:16px;font-weight:700;line-height:1.2;color:#ffffff;text-decoration:none;border-radius:12px;\">{Encode(actionText)}</a>" +
              "</td></tr></table>" +
              $"<p style=\"margin:12px 0 0;font-size:13px;line-height:1.5;color:#5c6475;word-break:break-all;\">Button not working? Paste this link into your browser:<br><a href=\"{Encode(actionUrl)}\" style=\"color:#5749c5;\">{Encode(actionUrl)}</a></p>";
        var secondaryHtml = secondaryActionText is null || secondaryActionUrl is null
            ? string.Empty
            : (secondaryLeadHtml is null
                  ? string.Empty
                  : $"<p style=\"margin:20px 0 8px;font-size:15px;line-height:1.55;color:#5c6475;\">{secondaryLeadHtml}</p>") +
              "<table role=\"presentation\" class=\"ow-button\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\" style=\"margin:4px 0 8px;\"><tr>" +
              "<td align=\"center\" style=\"border-radius:12px;border:2px solid #5b4dcc;background-color:#ffffff;\">" +
              $"<a href=\"{Encode(secondaryActionUrl)}\" style=\"display:inline-block;padding:12px 22px;font-family:{FontStack};font-size:16px;font-weight:700;line-height:1.2;color:#4b3fb8;text-decoration:none;border-radius:12px;\">{Encode(secondaryActionText)}</a>" +
              "</td></tr></table>";
        var footnote = footnoteHtml is null
            ? string.Empty
            : $"<p style=\"margin:20px 0 0;padding-top:16px;border-top:1px solid #e1e3eb;font-size:14px;line-height:1.55;color:#5c6475;\">{footnoteHtml}</p>";

        return "<!DOCTYPE html><html lang=\"en\"><head><meta charset=\"utf-8\">" +
            "<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">" +
            "<meta name=\"color-scheme\" content=\"light\"><meta name=\"supported-color-schemes\" content=\"light\">" +
            $"<title>{title}</title>" +
            "<style>@media only screen and (max-width:600px){.ow-shell{padding:16px 8px !important}.ow-card{padding:24px 20px !important}" +
            ".ow-title{font-size:24px !important}.ow-button,.ow-button td,.ow-button a{width:100% !important;display:block !important;box-sizing:border-box}}</style>" +
            "</head>" +
            $"<body style=\"margin:0;padding:0;background-color:#f7f7fb;font-family:{FontStack};color:#1c2033;-webkit-text-size-adjust:100%;\">" +
            $"<div style=\"display:none;max-height:0;overflow:hidden;opacity:0;\">{Encode(preheader)}</div>" +
            "<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\" style=\"background-color:#f7f7fb;\"><tr>" +
            "<td class=\"ow-shell\" align=\"center\" style=\"padding:32px 16px;\">" +
            "<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\" style=\"max-width:560px;\">" +
            "<tr><td style=\"padding:0 4px 16px;\">" +
            $"<span style=\"display:inline-block;width:12px;height:12px;border-radius:4px;background-color:#5b4dcc;margin-right:8px;vertical-align:middle;\"></span><span style=\"font-size:18px;font-weight:800;color:#1c2033;vertical-align:middle;\">OpenWish</span>" +
            "</td></tr>" +
            "<tr><td class=\"ow-card\" style=\"background-color:#ffffff;border:1px solid #e1e3eb;border-radius:16px;padding:32px;\">" +
            eyebrowHtml +
            $"<h1 class=\"ow-title\" style=\"margin:0 0 16px;font-size:28px;font-weight:700;line-height:1.2;color:#1c2033;\">{title}</h1>" +
            bodyHtml +
            actionHtml +
            secondaryHtml +
            footnote +
            "</td></tr>" +
            "<tr><td align=\"center\" style=\"padding:16px 8px 0;font-size:12px;line-height:1.5;color:#5c6475;\">Sent by OpenWish, your self-hosted wishlist and gift exchange.</td></tr>" +
            "</table></td></tr></table></body></html>";
    }

    private static string Encode(string value) => HtmlEncoder.Default.Encode(value);

    private static string SanitizeSubject(string value) =>
        value.Replace("\r", string.Empty, StringComparison.Ordinal)
            .Replace("\n", string.Empty, StringComparison.Ordinal);
}