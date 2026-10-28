using Microsoft.EntityFrameworkCore;
using OpenWish.Data;
using OpenWish.Shared.Models;
using OpenWish.Shared.Services;

namespace OpenWish.Application.Services;

public class BirthdayService(IDbContextFactory<ApplicationDbContext> contextFactory, TimeProvider? timeProvider = null) : IBirthdayService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory = contextFactory;
    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;

    public async Task<BirthdayModel> GetBirthdayAsync(string userId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Users
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new BirthdayModel { Month = u.BirthdayMonth, Day = u.BirthdayDay })
            .FirstOrDefaultAsync() ?? new BirthdayModel();
    }

    public async Task SetBirthdayAsync(string userId, int? month, int? day)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        if (month is null != day is null)
        {
            throw new ArgumentException("Choose both a month and a day, or neither.");
        }

        if (month is { } m && day is { } d && !BirthdayCalendar.IsValid(m, d))
        {
            throw new ArgumentException("That date doesn't exist. Check the month and day.");
        }

        await using var context = await _contextFactory.CreateDbContextAsync();
        var user = await context.Users.FirstOrDefaultAsync(u => u.Id == userId)
            ?? throw new KeyNotFoundException("User not found.");
        user.BirthdayMonth = month;
        user.BirthdayDay = day;
        await context.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<UpcomingBirthdayModel>> GetUpcomingBirthdaysAsync(string userId, int withinDays = 30)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        await using var context = await _contextFactory.CreateDbContextAsync();
        var friendIds = await context.Friends
            .AsNoTracking()
            .Where(f => !f.Deleted && (f.UserId == userId || f.FriendUserId == userId))
            .Select(f => f.UserId == userId ? f.FriendUserId : f.UserId)
            .Distinct()
            .ToListAsync();

        var friends = await context.Users
            .AsNoTracking()
            .Where(u => friendIds.Contains(u.Id) && u.BirthdayMonth != null && u.BirthdayDay != null)
            .Select(u => new { u.Id, u.UserName, Month = u.BirthdayMonth!.Value, Day = u.BirthdayDay!.Value })
            .ToListAsync();

        var today = DateOnly.FromDateTime(_timeProvider.GetUtcNow().UtcDateTime);
        var upcoming = friends
            .Where(f => BirthdayCalendar.IsValid(f.Month, f.Day))
            .Select(f => new { Friend = f, DaysUntil = BirthdayCalendar.DaysUntil(f.Month, f.Day, today) })
            .Where(f => f.DaysUntil <= withinDays)
            .OrderBy(f => f.DaysUntil)
            .ThenBy(f => f.Friend.UserName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var results = new List<UpcomingBirthdayModel>();
        foreach (var entry in upcoming)
        {
            results.Add(new UpcomingBirthdayModel
            {
                UserName = PublicDisplayName.From(entry.Friend.UserName, "A friend"),
                Month = entry.Friend.Month,
                Day = entry.Friend.Day,
                DaysUntil = entry.DaysUntil,
                WishlistPublicId = await FindOpenableWishlistAsync(context, entry.Friend.Id, userId)
            });
        }

        return results;
    }

    private static async Task<string?> FindOpenableWishlistAsync(ApplicationDbContext context, string ownerId, string viewerId)
    {
        var candidates = await context.Wishlists
            .AsNoTracking()
            .Where(w => !w.Deleted && w.OwnerId == ownerId)
            .OrderBy(w => w.CreatedOn)
            .Select(w => new { w.Id, w.PublicId })
            .ToListAsync();

        foreach (var candidate in candidates)
        {
            if (await WishlistService.CanUserAccessWishlistInternalAsync(context, candidate.Id, viewerId))
            {
                return candidate.PublicId;
            }
        }

        return null;
    }
}