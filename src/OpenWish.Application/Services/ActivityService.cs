using AutoMapper;
using Microsoft.EntityFrameworkCore;
using OpenWish.Data;
using OpenWish.Data.Entities;
using OpenWish.Shared.Models;
using OpenWish.Shared.Services;

namespace OpenWish.Application.Services;

public class ActivityService(IDbContextFactory<ApplicationDbContext> contextFactory, IMapper mapper) : IActivityService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory = contextFactory;
    private readonly IMapper _mapper = mapper;

    public async Task<ActivityLogModel> LogActivityAsync(
        string userId,
        string activityType,
        string description,
        int? wishlistId = null,
        int? wishlistItemId = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var activityLog = new ActivityLog
        {
            UserId = userId,
            ActivityType = activityType,
            Description = description,
            WishlistId = wishlistId,
            WishlistItemId = wishlistItemId,
            CreatedOn = DateTimeOffset.UtcNow,
            UpdatedOn = DateTimeOffset.UtcNow
        };

        context.ActivityLogs.Add(activityLog);
        await context.SaveChangesAsync();

        return _mapper.Map<ActivityLogModel>(activityLog);
    }

    public async Task<IEnumerable<ActivityLogModel>> GetUserActivityFeedAsync(string userId, int count = 20, int skip = 0)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var activities = await context.ActivityLogs
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.CreatedOn)
            .Skip(skip)
            .Take(count)
            .Include(a => a.User)
            .Include(a => a.Wishlist)
            .Include(a => a.WishlistItem)
            .ToListAsync();

        return RemoveUserEmails(_mapper.Map<IEnumerable<ActivityLogModel>>(activities));
    }

    public async Task<IEnumerable<ActivityLogModel>> GetFriendsActivityFeedAsync(string userId, int count = 20, int skip = 0)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        // Get list of friends
        var friendIds = await context.Friends
            .Where(f => f.UserId == userId && !f.Deleted)
            .Select(f => f.FriendUserId)
            .ToListAsync();

        var accessibleWishlistIds = context.Wishlists
            .Where(w => !w.Deleted &&
                (w.OwnerId == userId ||
                 context.WishlistPermissions.Any(permission =>
                     permission.WishlistId == w.Id &&
                     permission.UserId == userId &&
                     !permission.Deleted) ||
                 (!w.IsPrivate &&
                  !w.IsFriendsOnly &&
                  friendIds.Contains(w.OwnerId)) ||
                 (w.EventId.HasValue &&
                  !w.Event.Deleted &&
                  (w.Event.CreatedBy.Id == userId ||
                   w.Event.EventUsers.Any(eventUser =>
                       eventUser.UserId == userId &&
                       eventUser.Status == "Accepted" &&
                       !eventUser.Deleted)))))
            .Select(w => w.Id);

        var activities = await context.ActivityLogs
            .Where(a =>
                !a.Deleted &&
                friendIds.Contains(a.UserId) &&
                (!a.WishlistId.HasValue || accessibleWishlistIds.Contains(a.WishlistId.Value)) &&
                (!a.WishlistItemId.HasValue ||
                 (a.WishlistItem != null &&
                  (!a.WishlistItem.IsPrivate || a.Wishlist!.OwnerId == userId) &&
                  (!a.WishlistItem.IsHiddenFromOwner || a.Wishlist!.OwnerId != userId))) &&
                ((a.ActivityType != "ItemReserved" && a.ActivityType != "ReservationCanceled") ||
                 !context.ItemReservations.Any(reservation =>
                    reservation.WishlistItemId == a.WishlistItemId &&
                    reservation.UserId == a.UserId &&
                    reservation.IsAnonymous)))
            .OrderByDescending(a => a.CreatedOn)
            .Skip(skip)
            .Take(count)
            .Include(a => a.User)
            .Include(a => a.Wishlist)
            .Include(a => a.WishlistItem)
            .ToListAsync();

        return RemoveUserEmails(_mapper.Map<IEnumerable<ActivityLogModel>>(activities));
    }

    public async Task<IEnumerable<ActivityLogModel>> GetWishlistActivityAsync(
        int wishlistId,
        int count = 20,
        int skip = 0,
        string? requestingUserId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(requestingUserId);

        await using var context = await _contextFactory.CreateDbContextAsync();
        var activities = await context.ActivityLogs
            .Where(a =>
                !a.Deleted &&
                a.WishlistId == wishlistId &&
                (!a.WishlistItemId.HasValue ||
                 (a.WishlistItem != null &&
                  (!a.WishlistItem.IsPrivate || a.Wishlist!.OwnerId == requestingUserId) &&
                  (!a.WishlistItem.IsHiddenFromOwner || a.Wishlist!.OwnerId != requestingUserId))) &&
                (a.UserId == requestingUserId ||
                 ((a.ActivityType != "ItemReserved" && a.ActivityType != "ReservationCanceled") ||
                  !context.ItemReservations.Any(reservation =>
                      reservation.WishlistItemId == a.WishlistItemId &&
                      reservation.UserId == a.UserId &&
                      reservation.IsAnonymous))))
            .OrderByDescending(a => a.CreatedOn)
            .Skip(skip)
            .Take(count)
            .Include(a => a.User)
            .Include(a => a.WishlistItem)
            .ToListAsync();

        return RemoveUserEmails(_mapper.Map<IEnumerable<ActivityLogModel>>(activities));
    }

    private static IEnumerable<ActivityLogModel> RemoveUserEmails(IEnumerable<ActivityLogModel> activities)
    {
        var activityModels = activities.ToList();
        foreach (var activity in activityModels)
        {
            if (activity.User is not null)
            {
                activity.User.Email = string.Empty;
            }

            if (activity.Wishlist?.Owner is not null)
            {
                activity.Wishlist.Owner.Email = string.Empty;
            }
        }

        return activityModels;
    }
}