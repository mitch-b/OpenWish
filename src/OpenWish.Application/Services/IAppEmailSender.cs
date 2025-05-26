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
    Task SendEmailAsync(string toEmail, string subject, string message);
}
