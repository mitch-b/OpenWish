using Microsoft.AspNetCore.Identity;
using OpenWish.Data.Entities;
using OpenWish.Application.Services;

namespace OpenWish.Web.Services;

/// <summary>
/// Adapter to bridge ASP.NET Identity's IEmailSender with the application-level IAppEmailSender.
/// </summary>
public class IdentityEmailSenderAdapter(IAppEmailSender appEmailSender) : IEmailSender<ApplicationUser>
{
    private readonly IAppEmailSender _appEmailSender = appEmailSender;

    public Task SendConfirmationLinkAsync(ApplicationUser user, string email, string confirmationLink)
        => _appEmailSender.SendConfirmationLinkAsync(email, confirmationLink);

    public Task SendPasswordResetCodeAsync(ApplicationUser user, string email, string resetCode)
        => _appEmailSender.SendPasswordResetCodeAsync(email, resetCode);

    public Task SendPasswordResetLinkAsync(ApplicationUser user, string email, string resetLink)
        => _appEmailSender.SendPasswordResetLinkAsync(email, resetLink);

    public Task SendEmailAsync(string toEmail, string subject, string message)
        => _appEmailSender.SendEmailAsync(toEmail, subject, message);
}
