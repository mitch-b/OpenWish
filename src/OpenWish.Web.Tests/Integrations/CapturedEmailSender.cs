using System.Collections.Concurrent;
using FluentEmail.Core;
using FluentEmail.Core.Interfaces;
using FluentEmail.Core.Models;

namespace OpenWish.Web.Tests.Integrations;

public sealed class CapturedEmailSender : ISender
{
    public ConcurrentQueue<CapturedEmail> Messages { get; } = new();
    public bool FailGuestDelivery { get; set; }
    public string? FailGuestAddress { get; set; }

    public SendResponse Send(IFluentEmail email, CancellationToken? token = null)
    {
        if ((FailGuestDelivery || email.Data.ToAddresses.Any(to => to.EmailAddress == FailGuestAddress)) &&
            email.Data.Body.Contains("#access=", StringComparison.Ordinal))
        {
            var failure = new SendResponse();
            failure.ErrorMessages.Add("Synthetic guest delivery failure.");
            return failure;
        }
        foreach (var recipient in email.Data.ToAddresses)
        {
            Messages.Enqueue(new CapturedEmail(recipient.EmailAddress, email.Data.Subject, email.Data.Body));
        }
        return new SendResponse();
    }

    public Task<SendResponse> SendAsync(IFluentEmail email, CancellationToken? token = null) =>
        Task.FromResult(Send(email, token));
}

public sealed record CapturedEmail(string To, string Subject, string Body);