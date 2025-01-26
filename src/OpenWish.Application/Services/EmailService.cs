using FluentEmail.Core;
using Microsoft.Extensions.Logging;
using OpenWish.Shared.Services;

namespace OpenWish.Application.Services;

public class EmailService(IFluentEmail fluentEmail, ILogger<EmailService> logger) : IEmailService
{
    private readonly IFluentEmail _fluentEmail = fluentEmail;
    private readonly ILogger _logger = logger;

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
