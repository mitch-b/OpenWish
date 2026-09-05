using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OpenWish.Data;
using OpenWish.Data.Entities;

namespace OpenWish.Web.Services;

public sealed class DevelopmentDataSeeder(
    UserManager<ApplicationUser> userManager,
    IDbContextFactory<ApplicationDbContext> contextFactory)
{
    public const string OwnerEmail = "playwright-owner@openwish.local";

    private const string OwnerPersona = "owner";
    private const string GuestPersona = "guest";
    private const string FriendPersona = "friend";
    private const string WishlistPublicId = "demo-family-gift-ideas";
    private const string PrivateWishlistPublicId = "demo-private-ideas";
    private const string FriendWishlistPublicId = "demo-jordan-favorites";
    private const string EventPublicId = "demo-holiday-gift-exchange";

    private static readonly IReadOnlyDictionary<string, DevelopmentUser> _developmentUsers =
        new Dictionary<string, DevelopmentUser>(StringComparer.OrdinalIgnoreCase)
        {
            [OwnerPersona] = new(OwnerEmail, "AlexDemo"),
            [GuestPersona] = new("playwright-guest@openwish.local", "TaylorDemo"),
            [FriendPersona] = new("playwright-friend@openwish.local", "JordanDemo")
        };

    public async Task<ApplicationUser?> EnsureUserAsync(string? persona)
    {
        var selectedPersona = string.IsNullOrWhiteSpace(persona) ? OwnerPersona : persona;
        if (!_developmentUsers.TryGetValue(selectedPersona, out var developmentUser))
        {
            return null;
        }

        var user = await userManager.FindByEmailAsync(developmentUser.Email);
        if (user is not null)
        {
            return user;
        }

        user = new ApplicationUser
        {
            UserName = developmentUser.UserName,
            Email = developmentUser.Email,
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(user);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                $"Unable to create development persona '{selectedPersona}': " +
                string.Join(", ", result.Errors.Select(error => error.Description)));
        }

        return user;
    }

    public async Task<DevelopmentSeedResult> SeedAsync(CancellationToken cancellationToken)
    {
        var ownerIdentity = await EnsureUserAsync(OwnerPersona)
            ?? throw new InvalidOperationException("The owner development persona is unavailable.");
        var guestIdentity = await EnsureUserAsync(GuestPersona)
            ?? throw new InvalidOperationException("The guest development persona is unavailable.");
        var friendIdentity = await EnsureUserAsync(FriendPersona)
            ?? throw new InvalidOperationException("The friend development persona is unavailable.");

        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        await context.Database.ExecuteSqlRawAsync(
            "SELECT pg_advisory_xact_lock(684729104611723)",
            cancellationToken);

        var seededWishlistIds = new[]
        {
            WishlistPublicId,
            PrivateWishlistPublicId,
            FriendWishlistPublicId
        };
        var seededUserIds = new[]
        {
            ownerIdentity.Id,
            guestIdentity.Id,
            friendIdentity.Id
        };

        var seededWishlistDatabaseIds = await context.Wishlists
            .Where(entity => seededWishlistIds.Contains(entity.PublicId))
            .Select(entity => entity.Id)
            .ToListAsync(cancellationToken);
        var seededItemDatabaseIds = await context.WishlistItems
            .Where(entity => seededWishlistDatabaseIds.Contains(entity.WishlistId))
            .Select(entity => entity.Id)
            .ToListAsync(cancellationToken);
        var seededEventDatabaseIds = await context.Events
            .Where(entity => entity.PublicId == EventPublicId)
            .Select(entity => entity.Id)
            .ToListAsync(cancellationToken);

        await context.Notifications
            .Where(entity => seededUserIds.Contains(entity.UserId))
            .ExecuteDeleteAsync(cancellationToken);
        await context.ActivityLogs
            .Where(entity =>
                seededUserIds.Contains(entity.UserId) ||
                entity.WishlistId.HasValue && seededWishlistDatabaseIds.Contains(entity.WishlistId.Value) ||
                entity.WishlistItemId.HasValue && seededItemDatabaseIds.Contains(entity.WishlistItemId.Value))
            .ExecuteDeleteAsync(cancellationToken);
        await context.ItemComments
            .Where(entity => seededItemDatabaseIds.Contains(entity.WishlistItemId))
            .ExecuteDeleteAsync(cancellationToken);
        await context.ItemReservations
            .Where(entity => seededItemDatabaseIds.Contains(entity.WishlistItemId))
            .ExecuteDeleteAsync(cancellationToken);
        await context.Comments
            .Where(entity => seededItemDatabaseIds.Contains(entity.WishlistItemId))
            .ExecuteDeleteAsync(cancellationToken);
        await context.ItemReactions
            .Where(entity => seededItemDatabaseIds.Contains(entity.WishlistItemId))
            .ExecuteDeleteAsync(cancellationToken);
        await context.WillPurchases
            .Where(entity => seededItemDatabaseIds.Contains(entity.WishlistItemId))
            .ExecuteDeleteAsync(cancellationToken);
        await context.GiftExchanges
            .Where(entity => seededEventDatabaseIds.Contains(entity.EventId))
            .ExecuteDeleteAsync(cancellationToken);
        await context.EventUsers
            .Where(entity => seededEventDatabaseIds.Contains(entity.EventId))
            .ExecuteDeleteAsync(cancellationToken);
        await context.CustomPairingRules
            .Where(entity => seededEventDatabaseIds.Contains(entity.EventId))
            .ExecuteDeleteAsync(cancellationToken);
        await context.Events
            .Where(entity => seededEventDatabaseIds.Contains(EF.Property<int?>(entity, "CopiedFromEventId").GetValueOrDefault()))
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(
                    entity => EF.Property<int?>(entity, "CopiedFromEventId"),
                    (int?)null),
                cancellationToken);
        await context.Wishlists
            .Where(entity => entity.EventId.HasValue && seededEventDatabaseIds.Contains(entity.EventId.Value))
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(entity => entity.EventId, (int?)null),
                cancellationToken);
        await context.WishlistPermissions
            .Where(entity => seededWishlistDatabaseIds.Contains(entity.WishlistId))
            .ExecuteDeleteAsync(cancellationToken);
        await context.WishlistComments
            .Where(entity => seededWishlistDatabaseIds.Contains(entity.WishlistId))
            .ExecuteDeleteAsync(cancellationToken);
        await context.WishlistReactions
            .Where(entity => seededWishlistDatabaseIds.Contains(entity.WishlistId))
            .ExecuteDeleteAsync(cancellationToken);
        await context.WishlistItems
            .Where(entity => seededWishlistDatabaseIds.Contains(entity.WishlistId))
            .ExecuteDeleteAsync(cancellationToken);
        await context.Wishlists
            .Where(entity => seededWishlistIds.Contains(entity.PublicId))
            .ExecuteDeleteAsync(cancellationToken);
        await context.Events
            .Where(entity => entity.PublicId == EventPublicId)
            .ExecuteDeleteAsync(cancellationToken);
        await context.FriendRequests
            .Where(entity =>
                seededUserIds.Contains(entity.RequesterId) &&
                seededUserIds.Contains(entity.ReceiverId))
            .ExecuteDeleteAsync(cancellationToken);
        await context.Friends
            .Where(entity =>
                seededUserIds.Contains(entity.UserId) &&
                seededUserIds.Contains(entity.FriendUserId))
            .ExecuteDeleteAsync(cancellationToken);

        var owner = await context.Users.SingleAsync(user => user.Id == ownerIdentity.Id, cancellationToken);
        var guest = await context.Users.SingleAsync(user => user.Id == guestIdentity.Id, cancellationToken);
        var friend = await context.Users.SingleAsync(user => user.Id == friendIdentity.Id, cancellationToken);
        var now = DateTimeOffset.UtcNow;

        var eventEntity = new Event
        {
            PublicId = EventPublicId,
            Name = "Holiday Gift Exchange",
            Description = "A relaxed family exchange with a shared budget and surprise pairings.",
            Date = now.AddDays(21),
            Budget = 75m,
            IsGiftExchange = true,
            NamesDrawnOn = now,
            Tags = "Family,Holiday",
            CreatedBy = owner
        };

        var wishlist = new Wishlist
        {
            PublicId = WishlistPublicId,
            Name = "Family Gift Ideas",
            Icon = "🎁",
            Owner = owner,
            OwnerId = owner.Id,
            Event = eventEntity,
            IsCollaborative = true,
            IsPrivate = false,
            IsFriendsOnly = false,
            Items = [],
            WillPurchases = [],
            Comments = [],
            Reactions = [],
            Permissions = []
        };

        var headphones = new WishlistItem
        {
            PublicId = "demo-noise-cancelling-headphones",
            Name = "Noise-Cancelling Headphones",
            Url = "https://example.com/headphones",
            Image = "/images/openwish-color.svg",
            Description = "Comfortable wireless headphones for focused work and travel.",
            Price = 249.99m,
            WhereToBuy = "Local electronics store",
            Priority = 1,
            OrderIndex = 0,
            Wishlist = wishlist,
            Comments = [],
            Reactions = []
        };
        var dutchOven = new WishlistItem
        {
            PublicId = "demo-cast-iron-dutch-oven",
            Name = "Cast-Iron Dutch Oven",
            Url = "https://example.com/dutch-oven",
            Description = "A versatile five-quart pot for bread, soups, and family dinners.",
            Price = 89m,
            WhereToBuy = "Kitchen supply shop",
            Priority = 2,
            OrderIndex = 1,
            Wishlist = wishlist,
            Comments = [],
            Reactions = []
        };
        var parkPass = new WishlistItem
        {
            PublicId = "demo-national-park-pass",
            Name = "National Park Pass",
            Url = "https://example.com/park-pass",
            Description = "A year of weekend adventures and shared experiences.",
            Price = 80m,
            WhereToBuy = "National Park Service",
            Priority = 3,
            OrderIndex = 2,
            Wishlist = wishlist,
            Comments = [],
            Reactions = []
        };
        wishlist.Items.Add(headphones);
        wishlist.Items.Add(dutchOven);
        wishlist.Items.Add(parkPass);

        var privateWishlist = new Wishlist
        {
            PublicId = PrivateWishlistPublicId,
            Name = "Private Ideas",
            Icon = "🔒",
            Owner = owner,
            OwnerId = owner.Id,
            IsPrivate = true,
            Items = [],
            WillPurchases = [],
            Comments = [],
            Reactions = [],
            Permissions = []
        };
        privateWishlist.Items.Add(new WishlistItem
        {
            PublicId = "demo-private-item",
            Name = "A quiet weekend away",
            Description = "A private reminder visible only to the list owner.",
            Priority = 1,
            OrderIndex = 0,
            Wishlist = privateWishlist,
            Comments = [],
            Reactions = []
        });

        var friendWishlist = new Wishlist
        {
            PublicId = FriendWishlistPublicId,
            Name = "Jordan's Favorites",
            Icon = "⭐",
            Owner = friend,
            OwnerId = friend.Id,
            IsPrivate = false,
            Items = [],
            WillPurchases = [],
            Comments = [],
            Reactions = [],
            Permissions = []
        };
        friendWishlist.Items.Add(new WishlistItem
        {
            PublicId = "demo-board-game",
            Name = "Cooperative Board Game",
            Description = "A story-driven game for game night.",
            Price = 44.99m,
            WhereToBuy = "Neighborhood game store",
            Priority = 1,
            OrderIndex = 0,
            Wishlist = friendWishlist,
            Comments = [],
            Reactions = []
        });

        eventEntity.EventUsers.Add(new EventUser
        {
            PublicId = "demo-friend-event-user",
            Event = eventEntity,
            User = friend,
            UserId = friend.Id,
            InvitationDate = now.AddDays(-4),
            Status = "Accepted",
            IsAccepted = true,
            Role = "Participant"
        });
        eventEntity.EventUsers.Add(new EventUser
        {
            PublicId = "demo-guest-event-user",
            Event = eventEntity,
            User = guest,
            UserId = guest.Id,
            InvitationDate = now.AddDays(-1),
            Status = "Pending",
            Role = "Participant"
        });

        eventEntity.GiftExchanges.Add(new GiftExchange
        {
            PublicId = "demo-owner-gift-exchange",
            Event = eventEntity,
            Giver = owner,
            GiverId = owner.Id,
            Receiver = friend,
            ReceiverId = friend.Id,
            ReceiverPreferences = "Books, cooking, and shared experiences.",
            Budget = eventEntity.Budget
        });
        eventEntity.GiftExchanges.Add(new GiftExchange
        {
            PublicId = "demo-friend-gift-exchange",
            Event = eventEntity,
            Giver = friend,
            GiverId = friend.Id,
            Receiver = owner,
            ReceiverId = owner.Id,
            ReceiverPreferences = "Travel, music, and practical gadgets.",
            Budget = eventEntity.Budget
        });

        context.Friends.AddRange(
            new Friend
            {
                PublicId = "demo-owner-friend",
                User = owner,
                UserId = owner.Id,
                FriendUser = friend,
                FriendUserId = friend.Id,
                FriendshipDate = now.AddMonths(-3)
            },
            new Friend
            {
                PublicId = "demo-friend-owner",
                User = friend,
                UserId = friend.Id,
                FriendUser = owner,
                FriendUserId = owner.Id,
                FriendshipDate = now.AddMonths(-3)
            });

        context.FriendRequests.Add(new FriendRequest
        {
            PublicId = "demo-pending-friend-request",
            Requester = guest,
            RequesterId = guest.Id,
            Receiver = owner,
            ReceiverId = owner.Id,
            RequestDate = now.AddHours(-5),
            Status = "Pending"
        });

        wishlist.Permissions.Add(new WishlistPermission
        {
            PublicId = "demo-guest-wishlist-permission",
            Wishlist = wishlist,
            User = guest,
            UserId = guest.Id,
            PermissionType = "View"
        });

        context.ItemComments.Add(new ItemComment
        {
            PublicId = "demo-item-comment",
            WishlistItem = headphones,
            User = friend,
            UserId = friend.Id,
            Text = "These are excellent for long flights."
        });
        context.ItemReservations.Add(new ItemReservation
        {
            PublicId = "demo-item-reservation",
            WishlistItem = headphones,
            User = friend,
            UserId = friend.Id,
            ReservationDate = now.AddHours(-2),
            IsAnonymous = true
        });

        context.Notifications.AddRange(
            new Notification
            {
                PublicId = "demo-event-notification",
                Title = "Event invitation",
                Message = "Taylor is waiting to join Holiday Gift Exchange.",
                Type = "EventInvitation",
                SenderUser = guest,
                SenderUserId = guest.Id,
                User = owner,
                UserId = owner.Id,
                Date = now.AddHours(-1)
            },
            new Notification
            {
                PublicId = "demo-wishlist-notification",
                Title = "Wishlist activity",
                Message = "Jordan commented on Family Gift Ideas.",
                Type = "WishlistComment",
                SenderUser = friend,
                SenderUserId = friend.Id,
                User = owner,
                UserId = owner.Id,
                Date = now.AddMinutes(-20)
            });

        context.ActivityLogs.AddRange(
            new ActivityLog
            {
                PublicId = "demo-wishlist-activity",
                User = owner,
                UserId = owner.Id,
                ActivityType = "WishlistCreated",
                Description = "Created Family Gift Ideas",
                Wishlist = wishlist
            },
            new ActivityLog
            {
                PublicId = "demo-item-activity",
                User = friend,
                UserId = friend.Id,
                ActivityType = "ItemReserved",
                Description = "Reserved Noise-Cancelling Headphones",
                Wishlist = wishlist,
                WishlistItem = headphones
            });

        context.AddRange(eventEntity, wishlist, privateWishlist, friendWishlist);
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new DevelopmentSeedResult(
            owner.Email!,
            guest.Email!,
            friend.Email!,
            WishlistPublicId,
            PrivateWishlistPublicId,
            FriendWishlistPublicId,
            EventPublicId,
            headphones.Id);
    }

    private sealed record DevelopmentUser(string Email, string UserName);
}

public sealed record DevelopmentSeedResult(
    string OwnerEmail,
    string GuestEmail,
    string FriendEmail,
    string WishlistPublicId,
    string PrivateWishlistPublicId,
    string FriendWishlistPublicId,
    string EventPublicId,
    int ReservedItemId);