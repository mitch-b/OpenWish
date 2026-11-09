namespace OpenWish.Application.Services;

/// <summary>
/// Application-level interface for sending emails, decoupled from ASP.NET Identity.
/// </summary>
public interface IAppEmailSender
{
    Task SendConfirmationLinkAsync(string toEmail, string confirmationLink);
    Task SendPasswordResetCodeAsync(string toEmail, string resetCode);
    Task SendPasswordResetLinkAsync(string toEmail, string resetLink);
    Task SendFriendInviteEmailAsync(string toEmail, string inviterName, string inviteLink);
    Task SendEventInviteEmailAsync(string toEmail, string inviterName, string eventName, string inviteLink);
    Task SendGuestEventAccessEmailAsync(string toEmail, string eventName, string accessLink, bool matchReady = false,
        string? accountLink = null, string? inviterName = null);
    Task SendGiftExchangeDrawnEmailAsync(string toEmail, string eventName, string? recipientName, string eventLink);
    Task SendGiftExchangeResetEmailAsync(string toEmail, string eventName, string eventLink);
    Task SendEmailAsync(string toEmail, string subject, string message);
}