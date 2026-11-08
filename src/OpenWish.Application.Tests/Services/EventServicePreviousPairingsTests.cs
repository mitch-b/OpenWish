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

public class EventServicePreviousPairingsTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Draw_OnlyNamesRecipientsInMessagesWhenGiftBoxRevealIsOff(bool useGiftBoxReveal)
    {
        var (service, database, recorder) = await CreateServiceAsync(people: 2, copied: false);
        await using (var context = Open(database))
        {
            var evt = await context.Events.SingleAsync(e => e.PublicId == "next");
            evt.UseGiftBoxReveal = useGiftBoxReveal;
            await context.SaveChangesAsync();
        }

        await service.DrawNamesByPublicIdAsync("next", "u0");

        Assert.Equal(2, recorder.DrawnEmails.Count);
        Assert.Equal(2, recorder.DrawnNotifications.Count);
        Assert.All(recorder.DrawnEmails, email =>
        {
            Assert.Equal("https://openwish.local/events/next", email.Link);
            if (useGiftBoxReveal)
            {
                Assert.Null(email.RecipientName);
            }
            else
            {
                Assert.Contains(email.RecipientName, new[] { "u0", "u1" });
            }
        });
        Assert.All(recorder.DrawnNotifications, notification =>
        {
            if (useGiftBoxReveal)
            {
                Assert.Equal("Your gift exchange match is ready", notification.Title);
                Assert.Contains("Open your gift", notification.Message);
                Assert.DoesNotContain("u0", notification.Message);
                Assert.DoesNotContain("u1", notification.Message);
            }
            else
            {
                Assert.Equal("Gift Exchange Names Drawn!", notification.Title);
                Assert.Contains(" is u", notification.Message);
            }
        });
    }

    [Fact]
    public async Task Draw_ForACopiedEventAvoidsThePreviousPairingsWhenPossible()
    {
        for (var attempt = 0; attempt < 12; attempt++)
        {
            var (service, database, _) = await CreateServiceAsync(people: 3, copied: true);

            await service.DrawNamesByPublicIdAsync("next", "u0");

            await using var verify = Open(database);
            var drawn = await verify.GiftExchanges.Where(g => g.Event!.PublicId == "next").ToListAsync();
            Assert.Equal(3, drawn.Count);
            Assert.DoesNotContain(drawn, g => PreviousReceiver(g.GiverId!) == g.ReceiverId);
        }
    }

    [Fact]
    public async Task Draw_StillSucceedsWhenTheGroupCannotAvoidThePreviousPairings()
    {
        var (service, database, _) = await CreateServiceAsync(people: 2, copied: true);

        await service.DrawNamesByPublicIdAsync("next", "u0");

        await using var verify = Open(database);
        Assert.Equal(2, await verify.GiftExchanges.CountAsync(g => g.Event!.PublicId == "next"));
    }

    [Fact]
    public async Task Draw_ForAnEventThatWasNotCopiedIgnoresHistory()
    {
        var (service, database, _) = await CreateServiceAsync(people: 2, copied: false);

        await service.DrawNamesByPublicIdAsync("next", "u0");

        await using var verify = Open(database);
        Assert.Equal(2, await verify.GiftExchanges.CountAsync(g => g.Event!.PublicId == "next"));
    }

    [Fact]
    public async Task OrganizerOfACopiedExchangeSeesThatPairingsAreAvoided_ButMembersDoNot()
    {
        var (service, _, _) = await CreateServiceAsync(people: 3, copied: true);

        Assert.True((await service.GetEventByPublicIdAsync("next", "u0")).AvoidsPreviousPairings);
        Assert.False((await service.GetEventByPublicIdAsync("next", "u1")).AvoidsPreviousPairings);
    }

    private static string PreviousReceiver(string giverId) => giverId switch
    {
        "u0" => "u1",
        "u1" => "u2",
        _ => "u0"
    };

    private static ApplicationDbContext Open(string database) =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(database).Options);

    private static async Task<(EventService Service, string Database, InviteRecorder Invites)> CreateServiceAsync(int people, bool copied)
    {
        var databaseName = Guid.NewGuid().ToString();
        var services = new ServiceCollection();
        services.AddDbContext<ApplicationDbContext>(options => options.UseInMemoryDatabase(databaseName));
        var provider = services.BuildServiceProvider();

        await using (var seed = provider.GetRequiredService<ApplicationDbContext>())
        {
            var users = Enumerable.Range(0, people)
                .Select(i => new ApplicationUser { Id = $"u{i}", UserName = $"u{i}", Email = $"u{i}@example.com" })
                .ToList();
            seed.Users.AddRange(users);

            var previous = new Event { PublicId = "previous", Name = "Last year", CreatedBy = users[0], Date = DateTimeOffset.UtcNow.AddDays(-300), IsGiftExchange = true };
            for (var i = 0; i < people; i++)
            {
                var receiver = users[(i + 1) % people];
                previous.GiftExchanges.Add(new GiftExchange { Giver = users[i], GiverId = users[i].Id, Receiver = receiver, ReceiverId = receiver.Id, ReceiverPreferences = string.Empty });
            }

            var next = new Event { PublicId = "next", Name = "This year", CreatedBy = users[0], Date = DateTimeOffset.UtcNow.AddDays(30), IsGiftExchange = true, CopiedFromEvent = copied ? previous : null };
            foreach (var user in users.Skip(1))
            {
                next.EventUsers.Add(new EventUser { User = user, Status = "Accepted", IsAccepted = true });
            }

            seed.Events.AddRange(previous, next);
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
        public List<(string? RecipientName, string Link)> DrawnEmails { get; } = [];
        public List<(string Title, string Message)> DrawnNotifications { get; } = [];
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
                _recorder.DrawnNotifications.Add(((string)args![2]!, (string)args[3]!));
            }
            else if (targetMethod.Name == "SendGiftExchangeDrawnEmailAsync")
            {
                _recorder.DrawnEmails.Add(((string?)args![2], (string)args[3]!));
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