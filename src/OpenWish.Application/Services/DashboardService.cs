using Microsoft.EntityFrameworkCore;
using OpenWish.Data;
using OpenWish.Shared.Dashboard;
using OpenWish.Shared.Services;

namespace OpenWish.Application.Services;

public class DashboardService(IDbContextFactory<ApplicationDbContext> contextFactory) : IDashboardService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory = contextFactory;

    public async Task<FirstRunChecklist> GetFirstRunChecklistAsync(string userId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        await using var context = await _contextFactory.CreateDbContextAsync();
        var dismissedOn = await context.Users
            .Where(u => u.Id == userId)
            .Select(u => u.ChecklistDismissedOn)
            .FirstOrDefaultAsync();
        if (dismissedOn is not null)
        {
            return FirstRunChecklist.Hidden;
        }

        var hasWishlist = await context.Wishlists.AnyAsync(w => !w.Deleted && w.OwnerId == userId);
        var hasIdea = await context.WishlistItems.AnyAsync(i => !i.Deleted && !i.IsHiddenFromOwner && !i.Wishlist!.Deleted && i.Wishlist.OwnerId == userId);
        var hasFriend = await context.Friends.AnyAsync(f => !f.Deleted && (f.UserId == userId || f.FriendUserId == userId));
        var hasEvent = await context.Events.AnyAsync(e => !e.Deleted && e.CreatedBy.Id == userId)
            || await context.EventUsers.AnyAsync(eu => !eu.Deleted && eu.UserId == userId && eu.Status == "Accepted" && !eu.Event!.Deleted);

        return new FirstRunChecklist(hasWishlist, hasIdea, hasFriend, hasEvent, IsDismissed: false);
    }

    public async Task DismissFirstRunChecklistAsync(string userId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        await using var context = await _contextFactory.CreateDbContextAsync();
        var user = await context.Users.FirstOrDefaultAsync(u => u.Id == userId)
            ?? throw new KeyNotFoundException("User not found.");
        user.ChecklistDismissedOn ??= DateTimeOffset.UtcNow;
        await context.SaveChangesAsync();
    }
}