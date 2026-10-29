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

public class EventServiceRunAgainTests
{
    private static RunEventAgainRequest Request => new() { Name = "Exchange 2027", Date = DateTimeOffset.UtcNow.AddDays(365) };

    [Fact]
    public async Task RunAgain_CopiesDetailsAndLinksTheSource()
    {
        var (service, database, _) = await CreateServiceAsync();

        var result = await service.RunEventAgainByPublicIdAsync("source", Request, "host");

        Assert.NotEqual("source", result.PublicId);
        Assert.Equal("Exchange 2027", result.Name);
        Assert.Equal("Bring snacks", result.Description);
        Assert.Equal(50m, result.Budget);
        Assert.True(result.IsGiftExchange);
        Assert.Equal("SecretSanta", result.GiftExchangeStyle);
        Assert.True(result.IsRecurring);
        Assert.Equal("source", result.CopiedFromEventPublicId);
        Assert.Equal("Exchange", result.CopiedFromEventName);
        Assert.Equal("host", result.CreatedBy?.Id);
        Assert.Null(result.NamesDrawnOn);
        Assert.Null(result.AssignmentsRevealedOn);
        Assert.Empty(result.GiftExchanges);

        await using var verify = Open(database);
        Assert.Equal(0, await verify.GiftExchanges.CountAsync(g => g.Event!.PublicId == result.PublicId));
    }

    [Fact]
    public async Task RunAgain_CopiesPeopleAsPendingInvitationsWithoutTheOrganizerOrRejectedOrDeleted()
    {
        var (service, database, invites) = await CreateServiceAsync();

        var result = await service.RunEventAgainByPublicIdAsync("source", Request, "host");

        await using var verify = Open(database);
        var copied = await verify.EventUsers.Where(eu => eu.Event!.PublicId == result.PublicId).ToListAsync();
        Assert.Equal(2, copied.Count);
        Assert.All(copied, eu =>
        {
            Assert.Equal("Pending", eu.Status);
            Assert.False(eu.IsAccepted);
            Assert.Equal("Participant", eu.Role);
        });
        Assert.Contains(copied, eu => eu.UserId == "member");
        Assert.Contains(copied, eu => eu.InviteeEmail == "guest@example.com");
        Assert.DoesNotContain(copied, eu => eu.UserId is "host" or "removed" or "declined");
        Assert.Equal(2, invites.Emails.Count);
        Assert.Equal(1, invites.Notifications);
    }

    [Fact]
    public async Task RunAgain_CopiesOnlyRulesBetweenKeptParticipants()
    {
        var (service, database, _) = await CreateServiceAsync();

        var result = await service.RunEventAgainByPublicIdAsync("source", Request, "host");

        await using var verify = Open(database);
        var rules = await verify.CustomPairingRules.Where(r => r.Event!.PublicId == result.PublicId).ToListAsync();
        var rule = Assert.Single(rules);
        Assert.Equal("member", rule.SourceUserId);
        Assert.Equal("host", rule.TargetUserId);
        Assert.Equal("Exclusion", rule.RuleType);
    }

    [Fact]
    public async Task RunAgain_LeavesTheSourceUntouched()
    {
        var (service, database, _) = await CreateServiceAsync();

        await service.RunEventAgainByPublicIdAsync("source", Request, "host");

        await using var verify = Open(database);
        var source = await verify.Events
            .Include(e => e.EventUsers)
            .Include(e => e.GiftExchanges)
            .Include(e => e.PairingRules)
            .SingleAsync(e => e.PublicId == "source");
        Assert.Equal("Exchange", source.Name);
        Assert.NotNull(source.NamesDrawnOn);
        Assert.False(source.IsRecurring);
        Assert.Equal(4, source.EventUsers.Count);
        Assert.Single(source.GiftExchanges);
        Assert.Equal(2, source.PairingRules.Count);
        Assert.All(source.EventUsers.Where(eu => eu.UserId == "member"), eu => Assert.Equal("Accepted", eu.Status));
    }

    [Fact]
    public async Task RunAgain_RequiresTheOrganizer()
    {
        var (service, database, _) = await CreateServiceAsync();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.RunEventAgainByPublicIdAsync("source", Request, "member"));

        await using var verify = Open(database);
        Assert.Equal(1, await verify.Events.CountAsync());
    }

    [Fact]
    public async Task RunAgain_HidesTheEventFromNonMembers()
    {
        var (service, _, _) = await CreateServiceAsync();

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            service.RunEventAgainByPublicIdAsync("source", Request, "outsider"));
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            service.RunEventAgainByPublicIdAsync("missing", Request, "host"));
    }

    [Fact]
    public async Task RunAgain_RequiresAName()
    {
        var (service, _, _) = await CreateServiceAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.RunEventAgainByPublicIdAsync("source", new RunEventAgainRequest { Name = "  ", Date = DateTimeOffset.UtcNow }, "host"));
    }

    [Fact]
    public async Task CopiedFromLink_IsOnlyVisibleToTheOrganizer()
    {
        var (service, _, _) = await CreateServiceAsync();
        var copy = await service.RunEventAgainByPublicIdAsync("source", Request, "host");
        await service.AcceptEventInvitationAsync(
            (await service.GetEventInvitationsAsync(copy.Id)).First(i => i.UserId == "member").Id, "member");

        var asMember = await service.GetEventByPublicIdAsync(copy.PublicId, "member");

        Assert.Null(asMember.CopiedFromEventPublicId);
        Assert.Null(asMember.CopiedFromEventName);
    }

    private static ApplicationDbContext Open(string database) =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(database).Options);

    private static async Task<(EventService Service, string Database, InviteRecorder Invites)> CreateServiceAsync()
    {
        var databaseName = Guid.NewGuid().ToString();
        var services = new ServiceCollection();
        services.AddDbContext<ApplicationDbContext>(options => options.UseInMemoryDatabase(databaseName));
        var provider = services.BuildServiceProvider();

        await using (var seed = provider.GetRequiredService<ApplicationDbContext>())
        {
            var host = new ApplicationUser { Id = "host", UserName = "host", Email = "host@example.com" };
            var member = new ApplicationUser { Id = "member", UserName = "member", Email = "member@example.com" };
            var removed = new ApplicationUser { Id = "removed", UserName = "removed", Email = "removed@example.com" };
            var declined = new ApplicationUser { Id = "declined", UserName = "declined", Email = "declined@example.com" };
            seed.Users.AddRange(host, member, removed, declined, new ApplicationUser { Id = "outsider", UserName = "outsider" });
            var evt = new Event
            {
                PublicId = "source",
                Name = "Exchange",
                Description = "Bring snacks",
                CreatedBy = host,
                Date = DateTimeOffset.UtcNow.AddDays(-5),
                Budget = 50m,
                IsGiftExchange = true,
                GiftExchangeStyle = "SecretSanta",
                NamesDrawnOn = DateTimeOffset.UtcNow.AddDays(-20)
            };
            evt.EventUsers.Add(new EventUser { User = member, Status = "Accepted", IsAccepted = true });
            evt.EventUsers.Add(new EventUser { InviteeEmail = "guest@example.com", Status = "Pending" });
            evt.EventUsers.Add(new EventUser { User = removed, Status = "Accepted", IsAccepted = true, Deleted = true });
            evt.EventUsers.Add(new EventUser { User = declined, Status = "Rejected" });
            evt.GiftExchanges.Add(new GiftExchange { Giver = host, Receiver = member, GiverId = "host", ReceiverId = "member", ReceiverPreferences = string.Empty });
            evt.PairingRules.Add(new CustomPairingRule { SourceUserId = "member", TargetUserId = "host", RuleType = "Exclusion", RuleDescription = string.Empty });
            evt.PairingRules.Add(new CustomPairingRule { SourceUserId = "member", TargetUserId = "removed", RuleType = "Exclusion", RuleDescription = string.Empty });
            seed.Events.Add(evt);
            await seed.SaveChangesAsync();
        }

        var invites = new InviteRecorder();
        var mapper = new MapperConfiguration(
            configuration => configuration.AddProfile<OpenWishProfile>(),
            NullLoggerFactory.Instance).CreateMapper();
        var service = new EventService(
            provider.GetRequiredService<IServiceScopeFactory>(),
            mapper,
            Recorder<INotificationService>.Create(invites),
            Recorder<IAppEmailSender>.Create(invites),
            Options.Create(new OpenWishSettings { BaseUri = "https://openwish.local" }),
            NullLogger<EventService>.Instance);
        return (service, databaseName, invites);
    }

    private sealed class InviteRecorder
    {
        public List<string> Emails { get; } = [];
        public int Notifications { get; set; }
    }

    private class Recorder<T> : DispatchProxy where T : class
    {
        private InviteRecorder _recorder = new();

        public static T Create(InviteRecorder recorder)
        {
            var proxy = Create<T, Recorder<T>>();
            ((Recorder<T>)(object)proxy)._recorder = recorder;
            return proxy;
        }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod!.Name == "SendEventInviteEmailAsync")
            {
                _recorder.Emails.Add((string)args![0]!);
            }
            else if (targetMethod.Name == "CreateNotificationAsync")
            {
                _recorder.Notifications++;
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