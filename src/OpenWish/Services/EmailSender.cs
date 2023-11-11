using FluentEmail.Core;
using Microsoft.AspNetCore.Identity.UI.Services;

namespace OpenWish.Services;

public class EmailSender : IEmailSender
{
    //private readonly ILogger _logger;
    private readonly IFluentEmail _fluentEmail;

    public EmailSender(IFluentEmail fluentEmail)
    {
        //_logger = logger;
        _fluentEmail = fluentEmail;
    }

    public async Task SendEmailAsync(string toEmail, string subject, string message)
    {
        var response = await _fluentEmail
            .To(toEmail)
            .Subject(subject)
            .Body(message)
            .SendAsync();

        // _logger.LogInformation(response.Successful 
        //                        ? $"Email to {toEmail} queued successfully!"
        //                        : $"Failure sending email to {toEmail}");
    }
} 