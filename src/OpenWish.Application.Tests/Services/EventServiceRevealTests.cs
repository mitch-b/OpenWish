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
using Xunit;

namespace OpenWish.Application.Tests.Services;

public class EventServiceRevealTests
{
    [Fact]
    public async Task Reveal_RequiresTheOrganizer()
    {
        var (service, _) = await CreateServiceAsync(daysFromNow: -2);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.RevealGiftExchangeByPublicIdAsync("event", "member"));
    }

    [Fact]
    public async Task Reveal_WaitsForTheEventDate()
    {
        var (service, _) = await CreateServiceAsync(daysFromNow: 3);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.RevealGiftExchangeByPublicIdAsync("event", "host"));
    }

    [Fact]
    public async Task Reveal_RequiresDrawnNames()
    {
        var (service, _) = await CreateServiceAsync(daysFromNow: -2, drawn: false);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.RevealGiftExchangeByPublicIdAsync("event", "host"));
    }

    [Fact]
    public async Task Reveal_SharesNamesOnlyWithMembersAfterTheOrganizerReveals()
    {
        var (service, _) = await CreateServiceAsync(daysFromNow: -2);

        Assert.Empty(await service.GetRevealedPairingsByPublicIdAsync("event", "member"));

        var revealed = await service.RevealGiftExchangeByPublicIdAsync("event", "host");
        var firstRevealTime = revealed.AssignmentsRevealedOn;
        Assert.NotNull(firstRevealTime);
        Assert.Equal(firstRevealTime, (await service.RevealGiftExchangeByPublicIdAsync("event", "host")).AssignmentsRevealedOn);

        var pairings = (await service.GetRevealedPairingsByPublicIdAsync("event", "member")).ToList();
        Assert.Equal(2, pairings.Count);
        Assert.Contains(pairings, p => p.GiverName == "host" && p.ReceiverName == "member");
        Assert.Contains(pairings, p => p.GiverName == "member" && p.ReceiverName == "host");
        Assert.DoesNotContain(pairings, p => p.GiverName.Contains('@') || p.ReceiverName.Contains('@'));

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.GetRevealedPairingsByPublicIdAsync("event", "outsider"));
    }

    [Fact]
    public async Task Reset_IsBlockedOnceRevealed()
    {
        var (service, _) = await CreateServiceAsync(daysFromNow: -2);
        await service.RevealGiftExchangeByPublicIdAsync("event", "host");
        var eventId = (await service.GetEventByPublicIdAsync("event", "host")).Id;

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ResetGiftExchangeAsync(eventId, "host"));
    }

    private static async Task<(EventService Service, string Database)> CreateServiceAsync(int daysFromNow, bool drawn = true)
    {
        var databaseName = Guid.NewGuid().ToString();
        var services = new ServiceCollection();
        services.AddDbContext<ApplicationDbContext>(options => options.UseInMemoryDatabase(databaseName));
        var provider = services.BuildServiceProvider();

        await using (var seed = provider.GetRequiredService<ApplicationDbContext>())
        {
            var host = new ApplicationUser { Id = "host", UserName = "host", Email = "host@example.com" };
            var member = new ApplicationUser { Id = "member", UserName = "member", Email = "member@example.com" };
            seed.Users.AddRange(host, member, new ApplicationUser { Id = "outsider", UserName = "outsider" });
            var evt = new Event
            {
                PublicId = "event",
                Name = "Exchange",
                CreatedBy = host,
                Date = DateTimeOffset.UtcNow.AddDays(daysFromNow),
                IsGiftExchange = true,
                NamesDrawnOn = drawn ? DateTimeOffset.UtcNow.AddDays(-20) : null
            };
            evt.EventUsers.Add(new EventUser { User = member, Status = "Accepted", IsAccepted = true });
            if (drawn)
            {
                evt.GiftExchanges.Add(new GiftExchange { Giver = host, Receiver = member, GiverId = "host", ReceiverId = "member", ReceiverPreferences = string.Empty });
                evt.GiftExchanges.Add(new GiftExchange { Giver = member, Receiver = host, GiverId = "member", ReceiverId = "host", ReceiverPreferences = string.Empty });
            }

            seed.Events.Add(evt);
            await seed.SaveChangesAsync();
        }

        var mapper = new MapperConfiguration(
            configuration => configuration.AddProfile<OpenWishProfile>(),
            NullLoggerFactory.Instance).CreateMapper();
        var service = new EventService(
            provider.GetRequiredService<IServiceScopeFactory>(),
            mapper,
            null!,
            null!,
            Options.Create(new OpenWishSettings { BaseUri = "https://openwish.local" }),
            NullLogger<EventService>.Instance);
        return (service, databaseName);
    }
}