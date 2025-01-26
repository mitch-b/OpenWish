using Microsoft.AspNetCore.Identity;
using OpenWish.Data.Entities;
using OpenWish.Shared.Services;

namespace OpenWish.Web.Services;

public class OpenWishEmailSender(ILogger<OpenWishEmailSender> logger, IEmailService emailService) : IEmailSender<ApplicationUser>
{
    private readonly ILogger _logger = logger;
    private readonly IEmailService _emailService = emailService;

    public Task SendConfirmationLinkAsync(ApplicationUser user, string email, string confirmationLink) =>
        _emailService.SendEmailAsync(email, "Confirm your email",
            WrapInHtmlFormattedEmail($"Please confirm your account by <a href='{confirmationLink}'>clicking here</a>."));

    public Task SendPasswordResetCodeAsync(ApplicationUser user, string email, string resetCode) =>
        _emailService.SendEmailAsync(email, "Reset your password",
            WrapInHtmlFormattedEmail($"Please reset your password using the following code: {resetCode}"));

    public Task SendPasswordResetLinkAsync(ApplicationUser user, string email, string resetLink) =>
        _emailService.SendEmailAsync(email, "Reset your password",
            WrapInHtmlFormattedEmail($"Please reset your password by <a href='{resetLink}'>clicking here</a>."));

    private string WrapInHtmlFormattedEmail(string message) => $"<html><body>{message}</body></html>";
} 