using AutoMapper;
using Microsoft.EntityFrameworkCore;
using OpenWish.Data;
using OpenWish.Data.Entities;
using OpenWish.Shared.Models;
using OpenWish.Shared.Services;

namespace OpenWish.Application.Services;

public class ActivityService(ApplicationDbContext context, IMapper mapper) : IActivityService
{
    private readonly ApplicationDbContext _context = context;
    private readonly IMapper _mapper = mapper;
    
    public async Task<ActivityLogModel> LogActivityAsync(
        string userId, 
        string activityType,
        string description,
        int? wishlistId = null,
        int? wishlistItemId = null)
    {
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
        
        _context.ActivityLogs.Add(activityLog);
        await _context.SaveChangesAsync();
        
        return _mapper.Map<ActivityLogModel>(activityLog);
    }
    
    public async Task<IEnumerable<ActivityLogModel>> GetUserActivityFeedAsync(string userId, int count = 20, int skip = 0)
    {
        var activities = await _context.ActivityLogs
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
        // Get list of friends
        var friendIds = await _context.Friends
            .Where(f => f.UserId == userId && !f.Deleted)
            .Select(f => f.FriendUserId)
            .ToListAsync();
        
        // Get activities from friends
        var activities = await _context.ActivityLogs
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
        var activities = await _context.ActivityLogs
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