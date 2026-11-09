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
    public async Task GuestEmail_ExplainsPrivateAccessWithoutNamingTheMatch()
    {
        var capture = new CapturingSender();
        var sender = new OpenWishEmailSender(NullLogger<OpenWishEmailSender>.Instance, new CapturingEmailFactory(capture));
        await sender.SendGuestEventAccessEmailAsync("guest@example.test", "Holiday <party>", "https://wish.example.test/guest#access=synthetic-token");
        Assert.NotNull(capture.Email);
        Assert.Contains("Holiday &lt;party&gt;", capture.Email.Data.Body);
        Assert.Contains("No account is needed", capture.Email.Data.Body);
        Assert.Contains("anyone you forward it to can use it", capture.Email.Data.Body);
        Assert.Contains("expires in 30 days", capture.Email.Data.Body);
        Assert.DoesNotContain("Your gift recipient", capture.Email.Data.Body);
    }

    [Fact]
    public async Task GuestEmail_ReportsDeliveryFailureInsteadOfClaimingSuccess()
    {
        var sender = new OpenWishEmailSender(NullLogger<OpenWishEmailSender>.Instance, new UnconfiguredEmailFactory());
        await Assert.ThrowsAsync<HttpRequestException>(() =>
            sender.SendGuestEventAccessEmailAsync("guest@example.test", "Event", "https://wish.example.test/guest"));
    }

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
            Assert.DoesNotContain("Your gift recipient", body);
        }
        else
        {
            Assert.Contains("Gift exchange names drawn", capture.Email.Data.Subject);
            Assert.Contains("Recipient &lt;name&gt;", body);
            Assert.Contains("Your gift recipient", body);
        }
    }

    [Fact]
    public async Task EventInviteEmail_UsesBrandedResponsiveLayout()
    {
        var capture = new CapturingSender();
        var sender = new OpenWishEmailSender(NullLogger<OpenWishEmailSender>.Instance, new CapturingEmailFactory(capture));

        await sender.SendEventInviteEmailAsync(
            "guest@example.test", "Alex <host>", "Holiday <party>", "https://wish.example.test/events/abc/accept-invite?email=a%40b.test&x=1");

        Assert.NotNull(capture.Email);
        var body = capture.Email.Data.Body;
        Assert.Contains("name=\"viewport\"", body);
        Assert.Contains("max-width:560px", body);
        Assert.Contains("@media only screen and (max-width:600px)", body);
        Assert.Contains("background-color:#5b4dcc", body);
        Assert.Contains(">OpenWish</span>", body);
        Assert.Contains("View and accept the invitation", body);
        Assert.Contains("Alex &lt;host&gt;", body);
        Assert.Contains("Holiday &lt;party&gt;", body);
        Assert.Contains("href=\"https://wish.example.test/events/abc/accept-invite?email=a%40b.test&amp;x=1\"", body);
        Assert.DoesNotContain("<host>", body);
        Assert.DoesNotContain("<party>", body);
        Assert.DoesNotContain("#28a745", body);
        Assert.Equal("Alex <host> invited you to Holiday <party>", capture.Email.Data.Subject);
    }

    [Fact]
    public async Task ResetEmail_UsesBrandedLayoutWithoutLegacyColors()
    {
        var capture = new CapturingSender();
        var sender = new OpenWishEmailSender(NullLogger<OpenWishEmailSender>.Instance, new CapturingEmailFactory(capture));

        await sender.SendGiftExchangeResetEmailAsync("someone@example.test", "Family", "https://wish.example.test/events/abc");

        Assert.NotNull(capture.Email);
        var body = capture.Email.Data.Body;
        Assert.Contains("Gift exchange reset", body);
        Assert.Contains("View event", body);
        Assert.DoesNotContain("#dc3545", body);
        Assert.DoesNotContain("#007bff", body);
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