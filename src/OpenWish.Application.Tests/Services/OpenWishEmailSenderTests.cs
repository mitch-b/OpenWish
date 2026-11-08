using FluentEmail.Core;
using FluentEmail.Core.Interfaces;
using FluentEmail.Core.Models;
using Microsoft.Extensions.Logging.Abstractions;
using OpenWish.Application.Services;
using Xunit;

namespace OpenWish.Application.Tests.Services;

public class OpenWishEmailSenderTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("Recipient <name>")]
    public async Task DrawnEmail_PreservesOptionalRevealAndEncodesContent(string? recipientName)
    {
        var capture = new CapturingSender();
        var sender = new OpenWishEmailSender(
            NullLogger<OpenWishEmailSender>.Instance,
            new CapturingEmailFactory(capture));

        await sender.SendGiftExchangeDrawnEmailAsync(
            "someone@example.test", "Holiday <party>", recipientName, "https://openwish.local/events/test?a=1&b=2");

        Assert.NotNull(capture.Email);
        var body = capture.Email.Data.Body;
        Assert.Contains("Holiday &lt;party&gt;", body);
        Assert.Contains("https://openwish.local/events/test?a=1&amp;b=2", body);
        Assert.DoesNotContain("<party>", body);
        if (recipientName is null)
        {
            Assert.Contains("match is ready", capture.Email.Data.Subject);
            Assert.Contains("Open your gift", body);
            Assert.DoesNotContain("Your Gift Recipient", body);
        }
        else
        {
            Assert.Contains("Gift Exchange Names Drawn", capture.Email.Data.Subject);
            Assert.Contains("Recipient &lt;name&gt;", body);
            Assert.Contains("Your Gift Recipient", body);
        }
    }

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

    private sealed class CapturingSender : ISender
    {
        public IFluentEmail? Email { get; private set; }

        public SendResponse Send(IFluentEmail email, CancellationToken? token = null)
        {
            Email = email;
            return new SendResponse();
        }

        public Task<SendResponse> SendAsync(IFluentEmail email, CancellationToken? token = null) =>
            Task.FromResult(Send(email, token));
    }

    private sealed class CapturingEmailFactory(CapturingSender sender) : IFluentEmailFactory
    {
        public IFluentEmail Create() => new Email(null!, sender, "from@example.test");
    }
}