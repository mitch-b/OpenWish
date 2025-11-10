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

        return _mapper.Map<IEnumerable<ActivityLogModel>>(activities);
    }

    public async Task<IEnumerable<ActivityLogModel>> GetFriendsActivityFeedAsync(string userId, int count = 20, int skip = 0)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        // Get list of friends
        var friendIds = await context.Friends
            .Where(f => f.UserId == userId && !f.Deleted)
            .Select(f => f.FriendUserId)
            .ToListAsync();

        // Get activities from friends
        var activities = await context.ActivityLogs
            .Where(a => friendIds.Contains(a.UserId))
            .OrderByDescending(a => a.CreatedOn)
            .Skip(skip)
            .Take(count)
            .Include(a => a.User)
            .Include(a => a.Wishlist)
            .Include(a => a.WishlistItem)
            .ToListAsync();

        return _mapper.Map<IEnumerable<ActivityLogModel>>(activities);
    }

    public async Task<IEnumerable<ActivityLogModel>> GetWishlistActivityAsync(int wishlistId, int count = 20, int skip = 0)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var activities = await context.ActivityLogs
            .Where(a => a.WishlistId == wishlistId)
            .OrderByDescending(a => a.CreatedOn)
            .Skip(skip)
            .Take(count)
            .Include(a => a.User)
            .Include(a => a.WishlistItem)
            .ToListAsync();

        return _mapper.Map<IEnumerable<ActivityLogModel>>(activities);
    }
}