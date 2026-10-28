using Microsoft.EntityFrameworkCore;
using OpenWish.Data;
using OpenWish.Shared.Models;
using OpenWish.Shared.Services;

namespace OpenWish.Application.Services;

public class GiftPlanService(IDbContextFactory<ApplicationDbContext> contextFactory) : IGiftPlanService
{
    private const string SharedListName = "Shared event list";

    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory = contextFactory;

    public async Task<GiftPlanModel> GetGiftPlanAsync(string userId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        await using var context = await _contextFactory.CreateDbContextAsync();
        var reservations = await context.ItemReservations
            .AsNoTracking()
            .Where(r => !r.Deleted &&
                        r.UserId == userId &&
                        r.WishlistItem != null &&
                        !r.WishlistItem.Deleted &&
                        !r.WishlistItem.IsPrivate &&
                        r.WishlistItem.Wishlist != null &&
                        !r.WishlistItem.Wishlist.Deleted &&
                        r.WishlistItem.Wishlist.OwnerId != userId)
            .Select(r => new
            {
                r.ReservationDate,
                r.PurchasedOn,
                Item = r.WishlistItem!,
                Wishlist = r.WishlistItem!.Wishlist!,
                OwnerName = r.WishlistItem.Wishlist.Owner != null ? r.WishlistItem.Wishlist.Owner.UserName : null,
                EventName = r.WishlistItem.Wishlist.Event != null && !r.WishlistItem.Wishlist.Event.Deleted
                    ? r.WishlistItem.Wishlist.Event.Name : null,
                EventPublicId = r.WishlistItem.Wishlist.Event != null && !r.WishlistItem.Wishlist.Event.Deleted
                    ? r.WishlistItem.Wishlist.Event.PublicId : null
            })
            .ToListAsync();

        var accessible = new Dictionary<int, bool>();
        var visible = new List<(string OwnerKey, string RecipientName, GiftPlanItemModel Item)>();
        foreach (var row in reservations.OrderBy(r => r.ReservationDate))
        {
            if (!accessible.TryGetValue(row.Wishlist.Id, out var canOpen))
            {
                canOpen = await WishlistService.CanUserAccessWishlistInternalAsync(context, row.Wishlist.Id, userId);
                accessible[row.Wishlist.Id] = canOpen;
            }

            if (!canOpen)
            {
                continue;
            }

            visible.Add((
                row.Wishlist.OwnerId,
                PublicDisplayName.From(row.OwnerName, SharedListName),
                new GiftPlanItemModel
                {
                    ItemPublicId = row.Item.PublicId,
                    ItemName = row.Item.Name,
                    Price = row.Item.Price,
                    Url = row.Item.Url,
                    Image = row.Item.Image,
                    WishlistName = row.Wishlist.Name,
                    WishlistPublicId = row.Wishlist.PublicId,
                    EventName = row.EventName,
                    EventPublicId = row.EventPublicId,
                    ReservedOn = row.ReservationDate,
                    PurchasedOn = row.PurchasedOn
                }));
        }

        var recipients = visible
            .GroupBy(entry => entry.OwnerKey)
            .Select(group => new GiftPlanRecipientModel
            {
                RecipientName = group.First().RecipientName,
                Items = group.Select(entry => entry.Item).ToList()
            })
            .OrderBy(recipient => recipient.RecipientName, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        return new GiftPlanModel { Recipients = recipients };
    }

    public async Task SetGiftPurchasedAsync(string userId, string wishlistPublicId, string itemPublicId, bool isPurchased)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        await using var context = await _contextFactory.CreateDbContextAsync();
        var reservation = await context.ItemReservations
            .Include(r => r.WishlistItem)
            .FirstOrDefaultAsync(r => !r.Deleted &&
                                      r.UserId == userId &&
                                      r.WishlistItem != null &&
                                      !r.WishlistItem.Deleted &&
                                      r.WishlistItem.PublicId == itemPublicId &&
                                      r.WishlistItem.Wishlist != null &&
                                      !r.WishlistItem.Wishlist.Deleted &&
                                      r.WishlistItem.Wishlist.PublicId == wishlistPublicId)
            ?? throw new KeyNotFoundException("You have not reserved that gift.");

        if (!await WishlistService.CanUserAccessWishlistInternalAsync(context, reservation.WishlistItem!.WishlistId, userId))
        {
            throw new KeyNotFoundException("You have not reserved that gift.");
        }

        reservation.PurchasedOn = isPurchased ? reservation.PurchasedOn ?? DateTimeOffset.UtcNow : null;
        reservation.UpdatedOn = DateTimeOffset.UtcNow;
        await context.SaveChangesAsync();
    }
}