using System.Reflection;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using OpenWish.Application.Models;
using OpenWish.Application.Models.Configuration;
using OpenWish.Application.Services;
using OpenWish.Data;
using OpenWish.Data.Entities;
using OpenWish.Shared.RequestModels;
using OpenWish.Shared.Services;
using Xunit;

namespace OpenWish.Application.Tests.Services;

public class EventServiceOrganizerAcceptTests
{
    [Fact]
    public async Task OrganizerAccept_AcceptsRegisteredInviteeAndNotifiesThem()
    {
        var (service, database, notifications) = await CreateServiceAsync();

        await service.AcceptInvitationForParticipantAsync("evt", "member-invite", new OrganizerAcceptInvitationRequest(), "host");

        await using var verify = Open(database);
        var invite = await verify.EventUsers.SingleAsync(eu => eu.PublicId == "member-invite");
        Assert.Equal("Accepted", invite.Status);
        Assert.True(invite.IsAccepted);
        Assert.Null(invite.GuestDisplayName);
        Assert.Equal(1, notifications.Count);
    }

    [Fact]
    public async Task OrganizerAccept_AcceptsEmailInviteeWithTrimmedName()
    {
        var (service, database, notifications) = await CreateServiceAsync();

        await service.AcceptInvitationForParticipantAsync("evt", "email-invite",
            new OrganizerAcceptInvitationRequest { DisplayName = "  Grandma  " }, "host");

        await using var verify = Open(database);
        var invite = await verify.EventUsers.SingleAsync(eu => eu.PublicId == "email-invite");
        Assert.Equal("Accepted", invite.Status);
        Assert.True(invite.IsAccepted);
        Assert.Equal("Grandma", invite.GuestDisplayName);
        Assert.Equal(0, notifications.Count);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    [InlineData("grandma@example.com")]
    [InlineData("Line\nbreak")]
    public async Task OrganizerAccept_RequiresAValidNameForEmailInvitees(string? displayName)
    {
        var (service, database, _) = await CreateServiceAsync();

        await Assert.ThrowsAsync<ArgumentException>(() => service.AcceptInvitationForParticipantAsync("evt", "email-invite",
            new OrganizerAcceptInvitationRequest { DisplayName = displayName }, "host"));

        await using var verify = Open(database);
        Assert.Equal("Pending", (await verify.EventUsers.SingleAsync(eu => eu.PublicId == "email-invite")).Status);
    }

    [Fact]
    public async Task OrganizerAccept_RequiresTheOrganizer()
    {
        var (service, database, _) = await CreateServiceAsync();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.AcceptInvitationForParticipantAsync("evt", "email-invite",
            new OrganizerAcceptInvitationRequest { DisplayName = "Grandma" }, "member"));

        await using var verify = Open(database);
        Assert.Equal("Pending", (await verify.EventUsers.SingleAsync(eu => eu.PublicId == "email-invite")).Status);
    }

    [Fact]
    public async Task OrganizerAccept_DoesNotOverrideADecline()
    {
        var (service, _, _) = await CreateServiceAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.AcceptInvitationForParticipantAsync("evt", "declined-invite",
            new OrganizerAcceptInvitationRequest(), "host"));
    }

    [Fact]
    public async Task OrganizerAccept_IsUnavailableAfterTheDraw()
    {
        var (service, database, _) = await CreateServiceAsync();
        await using (var update = Open(database))
        {
            (await update.Events.SingleAsync(e => e.PublicId == "evt")).NamesDrawnOn = DateTimeOffset.UtcNow;
            await update.SaveChangesAsync();
        }

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.AcceptInvitationForParticipantAsync("evt", "email-invite",
            new OrganizerAcceptInvitationRequest { DisplayName = "Grandma" }, "host"));
    }

    [Fact]
    public async Task OrganizerAccept_RejectsInvitationsFromAnotherEvent()
    {
        var (service, _, _) = await CreateServiceAsync();

        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.AcceptInvitationForParticipantAsync("other", "email-invite",
            new OrganizerAcceptInvitationRequest { DisplayName = "Grandma" }, "host"));
    }

    private static ApplicationDbContext Open(string database) =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(database).Options);

    private static async Task<(EventService Service, string Database, NotificationCounter Notifications)> CreateServiceAsync()
    {
        var databaseName = Guid.NewGuid().ToString();
        var services = new ServiceCollection();
        services.AddDbContext<ApplicationDbContext>(options => options.UseInMemoryDatabase(databaseName));
        var provider = services.BuildServiceProvider();

        await using (var seed = provider.GetRequiredService<ApplicationDbContext>())
        {
            var host = new ApplicationUser { Id = "host", UserName = "host", Email = "host@example.com" };
            var member = new ApplicationUser { Id = "member", UserName = "member", Email = "member@example.com" };
            var declined = new ApplicationUser { Id = "declined", UserName = "declined", Email = "declined@example.com" };
            seed.Users.AddRange(host, member, declined);
            var evt = new Event
            {
                PublicId = "evt",
                Name = "Family exchange",
                CreatedBy = host,
                Date = DateTimeOffset.UtcNow.AddDays(30),
                IsGiftExchange = true,
                GiftExchangeStyle = "SecretSanta"
            };
            evt.EventUsers.Add(new EventUser { PublicId = "member-invite", User = member, Status = "Pending" });
            evt.EventUsers.Add(new EventUser { PublicId = "email-invite", InviteeEmail = "grandma@example.com", Status = "Pending" });
            evt.EventUsers.Add(new EventUser { PublicId = "declined-invite", User = declined, Status = "Rejected" });
            seed.Events.Add(evt);
            seed.Events.Add(new Event { PublicId = "other", Name = "Other", CreatedBy = host, Date = DateTimeOffset.UtcNow.AddDays(10) });
            await seed.SaveChangesAsync();
        }

        var notifications = new NotificationCounter();
        var mapper = new MapperConfiguration(
            configuration => configuration.AddProfile<OpenWishProfile>(),
            NullLoggerFactory.Instance).CreateMapper();
        var service = new EventService(
            provider.GetRequiredService<IServiceScopeFactory>(),
            mapper,
            Recorder<INotificationService>.Create(notifications),
            Recorder<IAppEmailSender>.Create(notifications),
            Options.Create(new OpenWishSettings { BaseUri = "https://openwish.local" }),
            NullLogger<EventService>.Instance);
        return (service, databaseName, notifications);
    }

    private sealed class NotificationCounter
    {
        public int Count { get; set; }
    }

    private class Recorder<T> : DispatchProxy where T : class
    {
        private NotificationCounter _counter = new();

        public static T Create(NotificationCounter counter)
        {
            var proxy = Create<T, Recorder<T>>();
            ((Recorder<T>)(object)proxy)._counter = counter;
            return proxy;
        }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod!.Name == "CreateNotificationAsync")
            {
                _counter.Count++;
            }

            var returnType = targetMethod.ReturnType;
            if (returnType == typeof(Task))
            {
                return Task.CompletedTask;
            }

            var resultType = returnType.GetGenericArguments()[0];
            var value = resultType.IsValueType ? Activator.CreateInstance(resultType) : null;
            return typeof(Task).GetMethod(nameof(Task.FromResult))!.MakeGenericMethod(resultType).Invoke(null, [value]);
        }
    }
}
