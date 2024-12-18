# 📃 OpenWish

Shareable wishlists. A web application intended for selfhosting.

* [.NET 9](https://dot.net/)
* Blazor [Auto Rendering Modes](https://learn.microsoft.com/en-us/aspnet/core/blazor/components/render-modes?view=aspnetcore-8.0) (Server + WebAssembly)
* Entity Framework Core

## Screenshots

TODO

## Features

* Users can have their own wishlist and a hidden wishlist
* Users can add items to wishlist (for themselves or others)
  * Simple (Name, description, price)
  * Via online store URL (todo: how?)
* Remove items from wishlist (list owner)
* Mark items as bought (authenticated user)
* Add comments to items (authenticated user)
* Add users and share wishlists (admin user)
* Allow anonymous access (optional feature)

## Installation

### Docker-Compose

TODO

(incl. GitHub actions to publish ghcr image)

### Local Docker

```pwsh
docker build -t openwish:dev .
docker run -it --rm -p 5000:8080 openwish:dev
```

## Local Development

### Secrets Management

```bash
cd src/OpenWish
dotnet tool install --global dotnet-ef
dotnet ef database update

dotnet user-secrets set EmailConfig:SmtpUser myuser
dotnet user-secrets set EmailConfig:SmtpPass mypass
dotnet user-secrets set EmailConfig:SmtpHost my-smtp.host.com
dotnet user-secrets set EmailConfig:SmtpPort 587
```

## License

This project is licensed under the Apache License 2.0 - see the [LICENSE](LICENSE) file for details.

## Possible Model

```csharp
using System;
using System.Collections.Generic;

public class User
{
    public int UserId { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
    public ICollection<Event> Events { get; set; } // Events the user is part of
    public ICollection<Wishlist> Wishlists { get; set; } // Personal wishlists
    public ICollection<Notification> Notifications { get; set; } // Notifications for the user
    public ICollection<PublicWishlist> PublicWishlists { get; set; } // Public wishlists shared outside events
}

public class Event
{
    public int EventId { get; set; }
    public string Name { get; set; }
    public DateTime Date { get; set; }
    public string Description { get; set; }
    public int CreatedByUserId { get; set; }
    public User CreatedBy { get; set; } // Creator of the event
    public ICollection<EventUser> EventUsers { get; set; } // Users invited to the event
    public ICollection<Wishlist> EventWishlists { get; set; } // Wishlists tied to the event
    public Event CopiedFromEvent { get; set; } // Reference to a past event if copied
    public bool IsRecurring { get; set; } // Indicates if the event is recurring
    public decimal? Budget { get; set; } // Budget for the event
    public bool IsGiftExchange { get; set; } // Indicates if this event has Gift Exchange
    public ICollection<GiftExchange> GiftExchanges { get; set; } // Gift Exchange pairings for the event
    public string Tags { get; set; } // Event-specific tags
    public ICollection<CustomPairingRule> PairingRules { get; set; } // Custom rules for pairing participants
}

public class EventUser
{
    public int EventId { get; set; }
    public Event Event { get; set; }

    public int UserId { get; set; }
    public User User { get; set; }

    public DateTime InvitationDate { get; set; }
    public bool IsAccepted { get; set; }
    public string Role { get; set; } // Role of the user in the event (e.g., Organizer, Viewer)
}

public class Wishlist
{
    public int WishlistId { get; set; }
    public string Name { get; set; }
    public int? OwnerId { get; set; } // Nullable if shared directly to event
    public User Owner { get; set; }
    public int? EventId { get; set; } // Nullable if a personal wishlist
    public Event Event { get; set; }
    public ICollection<WishlistItem> Items { get; set; } // Items in the wishlist
    public ICollection<WillPurchase> WillPurchases { get; set; } // Who has committed to buying items
    public bool IsCollaborative { get; set; } // Indicates if multiple users can add items
    public ICollection<WishlistComment> Comments { get; set; } // Comments on the wishlist
    public ICollection<WishlistReaction> Reactions { get; set; } // Reactions to the wishlist
}

public class WishlistItem
{
    public int WishlistItemId { get; set; }
    public string Name { get; set; }
    public string Url { get; set; }
    public string Image { get; set; }
    public string Description { get; set; }
    public decimal? Price { get; set; }
    public string WhereToBuy { get; set; }
    public int WishlistId { get; set; }
    public Wishlist Wishlist { get; set; }
    public bool IsPrivate { get; set; } // Indicates if the item is private
    public int Priority { get; set; } // Priority level (e.g., 1 = High, 2 = Medium, 3 = Low)
    public ICollection<Comment> Comments { get; set; } // Comments on the item
    public ICollection<ItemReaction> Reactions { get; set; } // Reactions to the item
    public int OrderIndex { get; set; } // Determines the order of items in the wishlist
    public bool IsHiddenFromOwner { get; set; } // Indicates if collaborative item is hidden from the wishlist owner
}

public class WillPurchase
{
    public int WillPurchaseId { get; set; }
    public int WishlistItemId { get; set; }
    public WishlistItem WishlistItem { get; set; }

    public int PurchaserId { get; set; }
    public User Purchaser { get; set; }
}

public class Notification
{
    public int NotificationId { get; set; }
    public string Message { get; set; }
    public DateTime Date { get; set; }
    public bool IsRead { get; set; }
    public int UserId { get; set; }
    public User User { get; set; }
}

public class GiftExchange
{
    public int GiftExchangeId { get; set; }
    public int EventId { get; set; }
    public Event Event { get; set; }

    public int GiverId { get; set; }
    public User Giver { get; set; }

    public int ReceiverId { get; set; }
    public User Receiver { get; set; }

    public bool IsAnonymous { get; set; } // Indicates if the pairing is anonymous
    public string ReceiverPreferences { get; set; } // Receiver's gift preferences or notes
    public decimal? Budget { get; set; } // Budget for this specific exchange
}

public class CustomPairingRule
{
    public int CustomPairingRuleId { get; set; }
    public int EventId { get; set; }
    public Event Event { get; set; }

    public int? SourceUserId { get; set; } // The user the rule applies to (e.g., who is excluded or must give to someone)
    public User SourceUser { get; set; }

    public int? TargetUserId { get; set; } // The target of the rule (e.g., who is excluded from or must receive a gift from SourceUser)
    public User TargetUser { get; set; }

    public string RuleType { get; set; } // e.g., "Exclusion", "MandatoryPairing", "CustomBudget"

    public string RuleDescription { get; set; } // Additional description for custom logic
}

public class PublicWishlist
{
    public int PublicWishlistId { get; set; }
    public string Name { get; set; }
    public int OwnerId { get; set; }
    public User Owner { get; set; }
    public ICollection<WishlistItem> Items { get; set; }
    public string SharedLink { get; set; } // Unique link for sharing publicly
    public string Tags { get; set; } // Comma-separated tags (e.g., "Tech,Books")
}

public class WishlistComment
{
    public int WishlistCommentId { get; set; }
    public string Text { get; set; }
    public DateTime CreatedDate { get; set; }
    public int WishlistId { get; set; }
    public Wishlist Wishlist { get; set; }
    public int UserId { get; set; }
    public User User { get; set; }
}

public class WishlistReaction
{
    public int WishlistReactionId { get; set; }
    public int WishlistId { get; set; }
    public Wishlist Wishlist { get; set; }
    public int UserId { get; set; }
    public User User { get; set; }
    public string ReactionType { get; set; } // e.g., "Like", "Love", "Wow"
}

public class Comment
{
    public int CommentId { get; set; }
    public string Text { get; set; }
    public DateTime CreatedDate { get; set; }
    public int WishlistItemId { get; set; }
    public WishlistItem WishlistItem { get; set; }
    public int UserId { get; set; }
    public User User { get; set; }
}

public class ItemReaction
{
    public int ItemReactionId { get; set; }
    public int WishlistItemId { get; set; }
    public WishlistItem WishlistItem { get; set; }

    public int UserId { get; set; }
    public User User { get; set; }

    public string ReactionType { get; set; } // e.g., "Like", "Love", "Wow"
}

public class ApplicationDbContext : DbContext
{
    public DbSet<User> Users { get; set; }
    public DbSet<Event> Events { get; set; }
    public DbSet<Wishlist> Wishlists { get; set; }
    public DbSet<WishlistItem> WishlistItems { get; set; }
    public DbSet<WillPurchase> WillPurchases { get; set; }
    public DbSet<Notification> Notifications { get; set; }
    public DbSet<GiftExchange> GiftExchanges { get; set; }
    public DbSet<CustomPairingRule> CustomPairingRules { get; set; }
    public DbSet<PublicWishlist> PublicWishlists { get; set; }
    public DbSet<WishlistComment> WishlistComments { get; set; }
    public DbSet<WishlistReaction> WishlistReactions { get; set; }
    public DbSet<Comment> Comments { get; set; }
    public DbSet<ItemReaction> ItemReactions { get; set; }
}
```