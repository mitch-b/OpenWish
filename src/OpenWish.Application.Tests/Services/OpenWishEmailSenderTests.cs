using FluentEmail.Core;
using FluentEmail.Core.Interfaces;
using FluentEmail.Core.Models;
using Microsoft.Extensions.Logging.Abstractions;
using OpenWish.Application.Services;
using Xunit;

namespace OpenWish.Application.Tests.Services;

public class OpenWishEmailSenderTests
{
    [Fact]
    public async Task SendEmailAsync_WithoutConfiguredSender_DoesNotThrow()
    {
        var sender = new OpenWishEmailSender(
            NullLogger<OpenWishEmailSender>.Instance,
            new UnconfiguredEmailFactory());

        await sender.SendEmailAsync("someone@example.test", "Subject", "Body");
    }

    private sealed class ThrowingSender : ISender
    {
        public SendResponse Send(IFluentEmail email, CancellationToken? token = null) => throw new InvalidOperationException("No sender.");

        public Task<SendResponse> SendAsync(IFluentEmail email, CancellationToken? token = null) =>
            throw new InvalidOperationException("No sender.");
    }

    private sealed class UnconfiguredEmailFactory : IFluentEmailFactory
    {
        public IFluentEmail Create() => new Email(null!, new ThrowingSender(), "from@example.test");
    }
}