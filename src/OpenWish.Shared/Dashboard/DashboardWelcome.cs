using System.Globalization;
using OpenWish.Shared.Models;

namespace OpenWish.Shared.Dashboard;

public enum DashboardWelcomeKind
{
    FirstRun,
    Invitation,
    UpcomingEvent,
    FriendRequest,
    CaughtUp,
    Unavailable
}

public sealed record DashboardAction(string Text, string Href, string Icon);

/// <summary>
/// The dashboard's opening message. New accounts get the full welcome; returning people get one
/// sentence about the most relevant thing waiting for them, so the dashboard stays out of the way.
/// </summary>
public sealed record DashboardWelcome
{
    public const int UpcomingWindowDays = 30;
    public const string Tagline = "Save ideas, check plans, and keep surprises private.";

    public required DashboardWelcomeKind Kind { get; init; }
    public required string Headline { get; init; }
    public required string Message { get; init; }
    public string Icon { get; init; } = "gift";
    public DashboardAction? PrimaryAction { get; init; }
    public DashboardAction? SecondaryAction { get; init; }

    public bool IsFirstRun => Kind == DashboardWelcomeKind.FirstRun;

    public static DashboardWelcome Unavailable { get; } = new()
    {
        Kind = DashboardWelcomeKind.Unavailable,
        Headline = "Welcome back.",
        Message = Tagline
    };

    public static DashboardWelcome Create(
        string? userId,
        IReadOnlyCollection<WishlistModel>? wishlists,
        IReadOnlyCollection<EventModel>? events,
        IReadOnlyCollection<EventUserModel>? pendingInvitations,
        IReadOnlyCollection<FriendRequestModel>? friendRequests,
        DateTimeOffset now)
    {
        if (wishlists is null || events is null)
        {
            return Unavailable;
        }

        var invitations = pendingInvitations ?? [];
        var requests = friendRequests ?? [];

        if (wishlists.Count == 0 && events.Count == 0)
        {
            return CreateFirstRun(invitations);
        }

        if (invitations.Count > 0)
        {
            return CreateInvitation(invitations, now);
        }

        var nextEvent = GetUpcomingEvents(events, now).FirstOrDefault();
        if (nextEvent is not null)
        {
            return CreateUpcomingEvent(nextEvent, userId, now);
        }

        if (requests.Count > 0)
        {
            return CreateFriendRequest(requests);
        }

        return CreateCaughtUp(wishlists, events, now);
    }

    /// <summary>
    /// Events from today through the next <see cref="UpcomingWindowDays"/> days, soonest first.
    /// Days are counted on the event's own calendar so a date-only event stays upcoming all day.
    /// </summary>
    public static IEnumerable<EventModel> GetUpcomingEvents(IEnumerable<EventModel> events, DateTimeOffset now) =>
        events
            .Where(evt => evt is not null && GetDaysUntil(evt.Date, now) is >= 0 and <= UpcomingWindowDays)
            .OrderBy(evt => evt.Date);

    public static int GetDaysUntil(DateTimeOffset date, DateTimeOffset now) =>
        (date.Date - now.ToOffset(date.Offset).Date).Days;

    public static string DescribeWhen(DateTimeOffset date, DateTimeOffset now) =>
        GetDaysUntil(date, now) switch
        {
            0 => "today",
            1 => "tomorrow",
            var days => $"in {days} days"
        };

    private static DashboardWelcome CreateFirstRun(IReadOnlyCollection<EventUserModel> invitations)
    {
        const string headline = "Your gift season, together.";
        var createWishlist = new DashboardAction("Create a wishlist", "/wishlists/new", "plus-lg");

        if (invitations.Count == 1)
        {
            var invitation = invitations.First();
            return new DashboardWelcome
            {
                Kind = DashboardWelcomeKind.FirstRun,
                Headline = headline,
                Message = $"You're invited to {GetEventName(invitation.Event)}. Review the invitation, " +
                    "then create a wishlist so your group knows what you'd love.",
                PrimaryAction = new DashboardAction("Review invitation", GetInvitationHref(invitation), "envelope"),
                SecondaryAction = createWishlist
            };
        }

        if (invitations.Count > 1)
        {
            return new DashboardWelcome
            {
                Kind = DashboardWelcomeKind.FirstRun,
                Headline = headline,
                Message = $"You have {invitations.Count} event invitations. Review them, " +
                    "then create a wishlist so your groups know what you'd love.",
                PrimaryAction = new DashboardAction("Review invitations", "/events", "envelope"),
                SecondaryAction = createWishlist
            };
        }

        return new DashboardWelcome
        {
            Kind = DashboardWelcomeKind.FirstRun,
            Headline = headline,
            Message = Tagline,
            PrimaryAction = createWishlist with { Text = "Create your first wishlist" },
            SecondaryAction = new DashboardAction("Start a gift exchange", "/events/new", "gift")
        };
    }

    private static DashboardWelcome CreateInvitation(IReadOnlyCollection<EventUserModel> invitations, DateTimeOffset now)
    {
        if (invitations.Count > 1)
        {
            return new DashboardWelcome
            {
                Kind = DashboardWelcomeKind.Invitation,
                Icon = "envelope",
                Headline = $"You have {invitations.Count} event invitations.",
                Message = "Review them to join each plan and share your wishlist.",
                PrimaryAction = new DashboardAction("Review invitations", "/events", "envelope")
            };
        }

        var invitation = invitations.First();
        var datePrefix = invitation.Event is { } evt && GetDaysUntil(evt.Date, now) >= 0
            ? $"It's {DescribeDate(evt.Date, now)}. "
            : string.Empty;

        return new DashboardWelcome
        {
            Kind = DashboardWelcomeKind.Invitation,
            Icon = "envelope",
            Headline = $"You're invited to {GetEventName(invitation.Event)}.",
            Message = $"{datePrefix}Review it to join the plan and share your wishlist.",
            PrimaryAction = new DashboardAction("Review invitation", GetInvitationHref(invitation), "envelope")
        };
    }

    private static DashboardWelcome CreateUpcomingEvent(EventModel evt, string? userId, DateTimeOffset now)
    {
        string message;
        if (evt.IsGiftExchange && evt.NamesDrawnOn.HasValue)
        {
            var budget = evt.Budget is > 0
                ? $" and the budget is {evt.Budget.Value.ToString("C", CultureInfo.CurrentCulture)}"
                : string.Empty;
            message = $"Names have been drawn{budget}. Open the event to see who you're shopping for.";
        }
        else if (evt.IsGiftExchange && IsOrganizer(evt, userId))
        {
            message = "Names haven't been drawn yet. Check who has joined, then draw names.";
        }
        else if (evt.IsGiftExchange)
        {
            message = "Names haven't been drawn yet. Keep your wishlist current so your match has ideas.";
        }
        else
        {
            message = "See who's coming and the wishlists shared for it.";
        }

        return new DashboardWelcome
        {
            Kind = DashboardWelcomeKind.UpcomingEvent,
            Icon = "calendar-event",
            Headline = $"{GetEventName(evt)} is {DescribeWhen(evt.Date, now)}.",
            Message = message,
            PrimaryAction = new DashboardAction("Open event", GetEventHref(evt), "calendar-event")
        };
    }

    private static DashboardWelcome CreateFriendRequest(IReadOnlyCollection<FriendRequestModel> requests)
    {
        if (requests.Count > 1)
        {
            return new DashboardWelcome
            {
                Kind = DashboardWelcomeKind.FriendRequest,
                Icon = "person-plus",
                Headline = $"You have {requests.Count} friend requests.",
                Message = "Accept them to share wishlists with each other.",
                PrimaryAction = new DashboardAction("Review requests", "/friends", "person-plus")
            };
        }

        var requester = requests.First()?.Requester?.UserName?.Trim();
        return new DashboardWelcome
        {
            Kind = DashboardWelcomeKind.FriendRequest,
            Icon = "person-plus",
            Headline = string.IsNullOrEmpty(requester)
                ? "You have a new friend request."
                : $"{requester} sent you a friend request.",
            Message = "Accept it to share wishlists with each other.",
            PrimaryAction = new DashboardAction("Review request", "/friends", "person-plus")
        };
    }

    private static DashboardWelcome CreateCaughtUp(
        IReadOnlyCollection<WishlistModel> wishlists,
        IReadOnlyCollection<EventModel> events,
        DateTimeOffset now)
    {
        const string headline = "You're all caught up.";

        var laterEvent = events
            .Where(evt => evt is not null && GetDaysUntil(evt.Date, now) > UpcomingWindowDays)
            .OrderBy(evt => evt.Date)
            .FirstOrDefault();

        if (laterEvent is not null)
        {
            return new DashboardWelcome
            {
                Kind = DashboardWelcomeKind.CaughtUp,
                Icon = "check-circle",
                Headline = headline,
                Message = $"{GetEventName(laterEvent)} is next, {DescribeDate(laterEvent.Date, now)}."
            };
        }

        if (wishlists.Count == 0)
        {
            return new DashboardWelcome
            {
                Kind = DashboardWelcomeKind.CaughtUp,
                Icon = "check-circle",
                Headline = headline,
                Message = "Create a wishlist so friends know what you'd love.",
                PrimaryAction = new DashboardAction("Create a wishlist", "/wishlists/new", "plus-lg")
            };
        }

        var ideaCount = wishlists.Sum(GetItemCount);
        var ideas = ideaCount == 1 ? "1 idea" : $"{ideaCount} ideas";
        var message = (wishlists.Count, ideaCount) switch
        {
            (1, 0) => "Your wishlist is ready for its first idea.",
            (1, _) => $"Your wishlist holds {ideas}. Save the next one when it comes to mind.",
            (var count, 0) => $"Your {count} wishlists are ready for their first ideas.",
            var (count, _) => $"Your {count} wishlists hold {ideas}. Save the next one when it comes to mind."
        };

        return new DashboardWelcome
        {
            Kind = DashboardWelcomeKind.CaughtUp,
            Icon = "check-circle",
            Headline = headline,
            Message = message
        };
    }

    private static string DescribeDate(DateTimeOffset date, DateTimeOffset now) =>
        GetDaysUntil(date, now) switch
        {
            0 => "today",
            1 => "tomorrow",
            _ when date.Year == now.ToOffset(date.Offset).Year =>
                $"on {date.ToString("MMMM d", CultureInfo.CurrentCulture)}",
            _ => $"on {date.ToString("MMMM d, yyyy", CultureInfo.CurrentCulture)}"
        };

    private static int GetItemCount(WishlistModel? wishlist) =>
        wishlist is null ? 0 : wishlist.ItemCount > 0 ? wishlist.ItemCount : wishlist.Items?.Count ?? 0;

    private static bool IsOrganizer(EventModel evt, string? userId) =>
        !string.IsNullOrWhiteSpace(userId) &&
        string.Equals(evt.CreatedBy?.Id, userId, StringComparison.Ordinal);

    private static string GetEventName(EventModel? evt) =>
        string.IsNullOrWhiteSpace(evt?.Name) ? "an event" : evt.Name.Trim();

    private static string GetEventHref(EventModel evt) =>
        string.IsNullOrWhiteSpace(evt.PublicId) ? "/events" : $"/events/{Uri.EscapeDataString(evt.PublicId)}";

    private static string GetInvitationHref(EventUserModel invitation) =>
        invitation.Event is { } evt ? GetEventHref(evt) : "/events";
}