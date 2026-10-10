using System.Globalization;
using System.Text.Json;
using OpenWish.Shared.Dashboard;
using OpenWish.Shared.Models;
using Xunit;

namespace OpenWish.Shared.Tests.Dashboard;

public class DashboardWelcomeTests
{
    private const string UserId = "user-1";
    private static readonly DateTimeOffset _now = new(2026, 10, 10, 15, 30, 0, TimeSpan.Zero);

    [Fact]
    public void Create_ReturnsUnavailable_WhenDashboardDataFailedToLoad()
    {
        var welcome = DashboardWelcome.Create(UserId, null, null, null, null, _now);

        Assert.Equal(DashboardWelcomeKind.Unavailable, welcome.Kind);
        Assert.False(welcome.IsFirstRun);
        Assert.Equal("Welcome back.", welcome.Headline);
        Assert.Null(welcome.PrimaryAction);
    }

    [Fact]
    public void Create_ShowsFullWelcome_ForAccountWithoutWishlistsOrEvents()
    {
        var welcome = DashboardWelcome.Create(UserId, [], [], [], [], _now);

        Assert.True(welcome.IsFirstRun);
        Assert.Equal("Your gift season, together.", welcome.Headline);
        Assert.Equal(DashboardWelcome.Tagline, welcome.Message);
        Assert.Equal(new DashboardAction("Create your first wishlist", "/wishlists/new", "plus-lg"), welcome.PrimaryAction);
        Assert.Equal(new DashboardAction("Start a gift exchange", "/events/new", "gift"), welcome.SecondaryAction);
    }

    [Fact]
    public void Create_FirstRunLeadsWithTheInvitation_WhenNewAccountWasInvited()
    {
        var invitation = Invitation(Event("Holiday Gift Exchange", _now.AddDays(21), publicId: "evt-1"));

        var welcome = DashboardWelcome.Create(UserId, [], [], [invitation], [], _now);

        Assert.True(welcome.IsFirstRun);
        Assert.Contains("You're invited to Holiday Gift Exchange.", welcome.Message);
        Assert.Equal(new DashboardAction("Review invitation", "/events/evt-1", "envelope"), welcome.PrimaryAction);
        Assert.Equal("/wishlists/new", welcome.SecondaryAction?.Href);
    }

    [Fact]
    public void Create_FirstRunSummarizesSeveralInvitations()
    {
        var invitations = new[]
        {
            Invitation(Event("One", _now.AddDays(3))),
            Invitation(Event("Two", _now.AddDays(5)))
        };

        var welcome = DashboardWelcome.Create(UserId, [], [], invitations, [], _now);

        Assert.True(welcome.IsFirstRun);
        Assert.StartsWith("You have 2 event invitations.", welcome.Message);
        Assert.Equal(new DashboardAction("Review invitations", "/events", "envelope"), welcome.PrimaryAction);
    }

    [Fact]
    public void Create_ReturningUserSeesCompactWelcome_AfterCreatingAWishlist()
    {
        var welcome = DashboardWelcome.Create(UserId, [Wishlist(3)], [], [], [], _now);

        Assert.False(welcome.IsFirstRun);
        Assert.Equal(DashboardWelcomeKind.CaughtUp, welcome.Kind);
    }

    [Fact]
    public void Create_ReturningUserSeesCompactWelcome_WhenOnlyJoinedAnEvent()
    {
        var welcome = DashboardWelcome.Create(UserId, [], [Event("Book Club", _now.AddDays(4))], [], [], _now);

        Assert.False(welcome.IsFirstRun);
        Assert.Equal(DashboardWelcomeKind.UpcomingEvent, welcome.Kind);
    }

    [Fact]
    public void Create_PrioritizesInvitation_OverUpcomingEventAndFriendRequests()
    {
        var invitation = Invitation(Event("Cousins Swap", new DateTimeOffset(2026, 12, 5, 0, 0, 0, TimeSpan.Zero), publicId: "swap"));

        var welcome = DashboardWelcome.Create(
            UserId,
            [Wishlist(1)],
            [Event("Book Club", _now.AddDays(2))],
            [invitation],
            [FriendRequest("Taylor")],
            _now);

        Assert.Equal(DashboardWelcomeKind.Invitation, welcome.Kind);
        Assert.Equal("You're invited to Cousins Swap.", welcome.Headline);
        Assert.Equal(
            $"It's on {invitation.Event!.Date.ToString("MMMM d", CultureInfo.CurrentCulture)}. Review it to join the plan and share your wishlist.",
            welcome.Message);
        Assert.Equal("/events/swap", welcome.PrimaryAction?.Href);
    }

    [Fact]
    public void Create_InvitationOmitsDate_WhenEventDateHasPassed()
    {
        var invitation = Invitation(Event("Past Party", _now.AddDays(-3)));

        var welcome = DashboardWelcome.Create(UserId, [Wishlist(1)], [], [invitation], [], _now);

        Assert.Equal("Review it to join the plan and share your wishlist.", welcome.Message);
    }

    [Fact]
    public void Create_SummarizesSeveralInvitations_ForReturningUser()
    {
        var welcome = DashboardWelcome.Create(
            UserId,
            [Wishlist(1)],
            [],
            [Invitation(Event("One", _now.AddDays(3))), Invitation(Event("Two", _now.AddDays(4))), Invitation(Event("Three", _now.AddDays(5)))],
            [],
            _now);

        Assert.Equal("You have 3 event invitations.", welcome.Headline);
        Assert.Equal("Review invitations", welcome.PrimaryAction?.Text);
        Assert.Equal("/events", welcome.PrimaryAction?.Href);
    }

    [Theory]
    [InlineData(0, "Book Club is today.")]
    [InlineData(1, "Book Club is tomorrow.")]
    [InlineData(21, "Book Club is in 21 days.")]
    [InlineData(30, "Book Club is in 30 days.")]
    public void Create_DescribesUpcomingEventByCalendarDay(int days, string expectedHeadline)
    {
        var date = new DateTimeOffset(_now.Date.AddDays(days), TimeSpan.Zero);

        var welcome = DashboardWelcome.Create(UserId, [Wishlist(1)], [Event("Book Club", date, publicId: "book")], [], [], _now);

        Assert.Equal(DashboardWelcomeKind.UpcomingEvent, welcome.Kind);
        Assert.Equal(expectedHeadline, welcome.Headline);
        Assert.Equal(new DashboardAction("Open event", "/events/book", "calendar-event"), welcome.PrimaryAction);
        Assert.Equal("See who's coming and the wishlists shared for it.", welcome.Message);
    }

    [Fact]
    public void Create_CountsDaysOnTheEventsOwnCalendar()
    {
        var lateEvening = new DateTimeOffset(2026, 10, 10, 23, 0, 0, TimeSpan.FromHours(-5));
        var evt = Event("Dinner", new DateTimeOffset(2026, 10, 11, 0, 0, 0, TimeSpan.FromHours(-5)));

        Assert.Equal(1, DashboardWelcome.GetDaysUntil(evt.Date, lateEvening));
        Assert.Equal("tomorrow", DashboardWelcome.DescribeWhen(evt.Date, lateEvening));
    }

    [Fact]
    public void Create_ChoosesTheSoonestUpcomingEvent()
    {
        var events = new[]
        {
            Event("Later", _now.AddDays(12)),
            Event("Past", _now.AddDays(-2)),
            Event("Sooner", _now.AddDays(3))
        };

        var welcome = DashboardWelcome.Create(UserId, [Wishlist(1)], events, [], [], _now);

        Assert.Equal("Sooner is in 3 days.", welcome.Headline);
    }

    [Fact]
    public void Create_MentionsDrawnNamesAndBudget_WithoutRevealingTheMatch()
    {
        var evt = Event("Holiday Gift Exchange", _now.AddDays(21), isGiftExchange: true, namesDrawn: true, budget: 75m);
        evt.GiftExchanges = [new GiftExchangeModel { Giver = User(UserId, "Alex"), Receiver = User("user-2", "Jordan") }];

        var welcome = DashboardWelcome.Create(UserId, [Wishlist(1)], [evt], [], [], _now);

        var budget = 75m.ToString("C", CultureInfo.CurrentCulture);
        Assert.Equal($"Names have been drawn and the budget is {budget}. Open the event to see who you're shopping for.", welcome.Message);
        Assert.DoesNotContain("Jordan", welcome.Headline + welcome.Message);
    }

    [Fact]
    public void Create_OmitsBudget_WhenNoneIsSet()
    {
        var evt = Event("Swap", _now.AddDays(5), isGiftExchange: true, namesDrawn: true);

        var welcome = DashboardWelcome.Create(UserId, [Wishlist(1)], [evt], [], [], _now);

        Assert.Equal("Names have been drawn. Open the event to see who you're shopping for.", welcome.Message);
    }

    [Fact]
    public void Create_AsksOrganizerToDrawNames_BeforeTheDraw()
    {
        var evt = Event("Swap", _now.AddDays(5), isGiftExchange: true, createdBy: UserId);

        var welcome = DashboardWelcome.Create(UserId, [Wishlist(1)], [evt], [], [], _now);

        Assert.Equal("Names haven't been drawn yet. Check who has joined, then draw names.", welcome.Message);
    }

    [Fact]
    public void Create_AsksParticipantToKeepWishlistCurrent_BeforeTheDraw()
    {
        var evt = Event("Swap", _now.AddDays(5), isGiftExchange: true, createdBy: "organizer");

        var welcome = DashboardWelcome.Create(UserId, [Wishlist(1)], [evt], [], [], _now);

        Assert.Equal("Names haven't been drawn yet. Keep your wishlist current so your match has ideas.", welcome.Message);
    }

    [Fact]
    public void Create_PrioritizesUpcomingEvent_OverFriendRequests()
    {
        var welcome = DashboardWelcome.Create(
            UserId,
            [Wishlist(1)],
            [Event("Book Club", _now.AddDays(2))],
            [],
            [FriendRequest("Taylor")],
            _now);

        Assert.Equal(DashboardWelcomeKind.UpcomingEvent, welcome.Kind);
    }

    [Fact]
    public void Create_NamesSingleFriendRequester()
    {
        var welcome = DashboardWelcome.Create(UserId, [Wishlist(1)], [], [], [FriendRequest("TaylorDemo")], _now);

        Assert.Equal(DashboardWelcomeKind.FriendRequest, welcome.Kind);
        Assert.Equal("TaylorDemo sent you a friend request.", welcome.Headline);
        Assert.Equal(new DashboardAction("Review request", "/friends", "person-plus"), welcome.PrimaryAction);
    }

    [Fact]
    public void Create_FallsBackWhenRequesterNameIsMissing()
    {
        var welcome = DashboardWelcome.Create(UserId, [Wishlist(1)], [], [], [FriendRequest(null)], _now);

        Assert.Equal("You have a new friend request.", welcome.Headline);
    }

    [Fact]
    public void Create_SummarizesSeveralFriendRequests()
    {
        var welcome = DashboardWelcome.Create(UserId, [Wishlist(1)], [], [], [FriendRequest("A"), FriendRequest("B")], _now);

        Assert.Equal("You have 2 friend requests.", welcome.Headline);
        Assert.Equal("Review requests", welcome.PrimaryAction?.Text);
    }

    [Fact]
    public void Create_CaughtUpMentionsTheNextEventBeyondTheUpcomingWindow()
    {
        var later = Event("Spring Picnic", new DateTimeOffset(2027, 3, 14, 0, 0, 0, TimeSpan.Zero));

        var welcome = DashboardWelcome.Create(UserId, [Wishlist(2)], [later], [], [], _now);

        Assert.Equal(DashboardWelcomeKind.CaughtUp, welcome.Kind);
        Assert.Equal("You're all caught up.", welcome.Headline);
        Assert.Equal(
            $"Spring Picnic is next, on {later.Date.ToString("MMMM d, yyyy", CultureInfo.CurrentCulture)}.",
            welcome.Message);
        Assert.Null(welcome.PrimaryAction);
    }

    [Theory]
    [InlineData(new[] { 0 }, "Your wishlist is ready for its first idea.")]
    [InlineData(new[] { 1 }, "Your wishlist holds 1 idea. Save the next one when it comes to mind.")]
    [InlineData(new[] { 2, 3 }, "Your 2 wishlists hold 5 ideas. Save the next one when it comes to mind.")]
    [InlineData(new[] { 0, 0 }, "Your 2 wishlists are ready for their first ideas.")]
    public void Create_CaughtUpSummarizesWishlists(int[] itemCounts, string expectedMessage)
    {
        var wishlists = itemCounts.Select(Wishlist).ToArray();

        var welcome = DashboardWelcome.Create(UserId, wishlists, [Event("Old", _now.AddDays(-40))], [], [], _now);

        Assert.Equal(DashboardWelcomeKind.CaughtUp, welcome.Kind);
        Assert.Equal(expectedMessage, welcome.Message);
    }

    [Fact]
    public void Create_CaughtUpUsesMappedItemCount_WhenItemsAreNotLoaded()
    {
        var wishlist = new WishlistModel { Name = "Ideas", ItemCount = 4 };

        var welcome = DashboardWelcome.Create(UserId, [wishlist], [], [], [], _now);

        Assert.Equal("Your wishlist holds 4 ideas. Save the next one when it comes to mind.", welcome.Message);
    }

    [Fact]
    public void Create_CaughtUpNudgesTowardAWishlist_WhenOnlyPastEventsExist()
    {
        var welcome = DashboardWelcome.Create(UserId, [], [Event("Old", _now.AddDays(-40))], [], [], _now);

        Assert.Equal("Create a wishlist so friends know what you'd love.", welcome.Message);
        Assert.Equal("/wishlists/new", welcome.PrimaryAction?.Href);
    }

    [Fact]
    public void GetUpcomingEvents_IncludesTodayAndExcludesPastOrDistantEvents()
    {
        var todayAtMidnight = new DateTimeOffset(_now.Date, TimeSpan.Zero);
        var events = new[]
        {
            Event("Today", todayAtMidnight),
            Event("Yesterday", todayAtMidnight.AddDays(-1)),
            Event("Distant", todayAtMidnight.AddDays(31)),
            Event("Soon", todayAtMidnight.AddDays(10))
        };

        var upcoming = DashboardWelcome.GetUpcomingEvents(events, _now).Select(evt => evt.Name);

        Assert.Equal(["Today", "Soon"], upcoming);
    }

    [Fact]
    public void Create_EscapesEventPublicIdInLinks()
    {
        var welcome = DashboardWelcome.Create(UserId, [Wishlist(1)], [Event("Odd", _now.AddDays(1), publicId: "a b")], [], [], _now);

        Assert.Equal("/events/a%20b", welcome.PrimaryAction?.Href);
    }

    [Fact]
    public void Welcome_SurvivesPrerenderStatePersistence()
    {
        var welcome = DashboardWelcome.Create(
            UserId,
            [],
            [],
            [Invitation(Event("Cookie Swap", _now.AddDays(3), publicId: "cookies"))],
            [],
            _now);

        var json = JsonSerializer.Serialize(welcome, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        var restored = JsonSerializer.Deserialize<DashboardWelcome>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Assert.Equal(welcome, restored);
        Assert.True(restored!.IsFirstRun);
    }

    private static WishlistModel Wishlist(int itemCount) => new()
    {
        Name = "Ideas",
        Items = Enumerable.Range(0, itemCount).Select(index => new WishlistItemModel { Name = $"Idea {index}" }).ToList()
    };

    private static EventModel Event(
        string name,
        DateTimeOffset date,
        string publicId = "evt",
        bool isGiftExchange = false,
        bool namesDrawn = false,
        decimal? budget = null,
        string createdBy = "organizer") => new()
        {
            Name = name,
            Date = date,
            PublicId = publicId,
            IsGiftExchange = isGiftExchange,
            NamesDrawnOn = namesDrawn ? _now.AddDays(-1) : null,
            Budget = budget,
            CreatedBy = User(createdBy, createdBy),
            EventUsers = []
        };

    private static EventUserModel Invitation(EventModel evt) => new() { Event = evt, Status = "Pending" };

    private static FriendRequestModel FriendRequest(string? requester) => new()
    {
        RequesterId = "requester",
        ReceiverId = UserId,
        Status = "Pending",
        Requester = requester is null ? null : User("requester", requester)
    };

    private static ApplicationUserModel User(string id, string userName) => new() { Id = id, UserName = userName, Email = $"{id}@example.test" };
}