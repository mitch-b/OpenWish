using OpenWish.Shared.Models;

namespace OpenWish.Shared.Dashboard;

public sealed record DashboardWishlistCard(
    int Id,
    string PublicId,
    string Name,
    string? Icon,
    string? OwnerId,
    string? OwnerName,
    int ItemCount,
    bool IsCollaborative,
    bool IsPrivate)
{
    public static DashboardWishlistCard From(WishlistModel wishlist) => new(
        wishlist.Id,
        wishlist.PublicId,
        wishlist.Name,
        wishlist.Icon,
        wishlist.OwnerId,
        wishlist.Owner?.UserName,
        wishlist.Items?.Count ?? wishlist.ItemCount,
        wishlist.IsCollaborative,
        wishlist.IsPrivate);

    public WishlistModel ToWishlistModel() => new()
    {
        Id = Id,
        PublicId = PublicId,
        Name = Name,
        Icon = Icon,
        OwnerId = OwnerId,
        Owner = OwnerName is null ? null : new ApplicationUserModel { Id = OwnerId ?? string.Empty, UserName = OwnerName, Email = string.Empty },
        ItemCount = ItemCount,
        IsCollaborative = IsCollaborative,
        IsPrivate = IsPrivate
    };
}

public sealed record DashboardEventCard(
    int Id,
    string PublicId,
    string Name,
    DateTimeOffset Date,
    string? CreatedById,
    string? CreatedByName,
    bool IsGiftExchange,
    string? GiftExchangeStyle,
    DateTimeOffset? NamesDrawnOn,
    int AcceptedParticipantCount,
    int PendingInvitationCount)
{
    public static DashboardEventCard From(EventModel evt)
    {
        var participants = evt.EventUsers ?? [];
        var acceptedUserIds = participants
            .Where(participant =>
                !participant.Deleted &&
                participant.IsAccepted &&
                string.Equals(participant.Status, "Accepted", StringComparison.OrdinalIgnoreCase))
            .Select(participant => participant.UserId)
            .Where(userId => !string.IsNullOrWhiteSpace(userId))
            .ToHashSet(StringComparer.Ordinal);

        if (!string.IsNullOrWhiteSpace(evt.CreatedBy?.Id))
        {
            acceptedUserIds.Add(evt.CreatedBy.Id);
        }

        return new DashboardEventCard(
            evt.Id,
            evt.PublicId,
            evt.Name,
            evt.Date,
            evt.CreatedBy?.Id,
            evt.CreatedBy?.UserName,
            evt.IsGiftExchange,
            evt.GiftExchangeStyle,
            evt.NamesDrawnOn,
            acceptedUserIds.Count,
            participants.Count(participant =>
                !participant.Deleted &&
                string.Equals(participant.Status, "Pending", StringComparison.OrdinalIgnoreCase)));
    }

    public EventModel ToEventModel() => new()
    {
        Id = Id,
        PublicId = PublicId,
        Name = Name,
        Date = Date,
        CreatedBy = CreatedByName is null ? null : new ApplicationUserModel { Id = CreatedById ?? string.Empty, UserName = CreatedByName, Email = string.Empty },
        IsGiftExchange = IsGiftExchange,
        GiftExchangeStyle = GiftExchangeStyle,
        NamesDrawnOn = NamesDrawnOn,
        EventUsers = []
    };
}