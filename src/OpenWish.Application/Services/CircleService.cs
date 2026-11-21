using Microsoft.EntityFrameworkCore;
using OpenWish.Data;
using OpenWish.Data.Entities;
using OpenWish.Shared.Models;
using OpenWish.Shared.Services;

namespace OpenWish.Application.Services;

public class CircleService(IDbContextFactory<ApplicationDbContext> contextFactory) : ICircleService
{
    private const string ViewPermission = "View";

    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory = contextFactory;

    public async Task<IReadOnlyList<CircleModel>> GetCirclesAsync(string userId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        await using var context = await _contextFactory.CreateDbContextAsync();
        var circles = await context.Circles
            .AsNoTracking()
            .Where(c => c.OwnerId == userId && !c.Deleted)
            .OrderBy(c => c.CreatedOn)
            .ToListAsync();
        return await BuildModelsAsync(context, circles);
    }

    public async Task<CircleModel> CreateCircleAsync(string userId, string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        var normalized = CircleRules.NormalizeName(name);

        await using var context = await _contextFactory.CreateDbContextAsync();
        var existingNames = await context.Circles
            .Where(c => c.OwnerId == userId && !c.Deleted)
            .Select(c => c.Name)
            .ToListAsync();
        if (existingNames.Count >= CircleRules.MaxCirclesPerPerson)
        {
            throw new InvalidOperationException($"You can have up to {CircleRules.MaxCirclesPerPerson} circles.");
        }

        if (existingNames.Contains(normalized, StringComparer.OrdinalIgnoreCase))
        {
            throw new ArgumentException("You already have a circle with that name.");
        }

        var circle = new Circle { OwnerId = userId, Name = normalized };
        context.Circles.Add(circle);
        await context.SaveChangesAsync();
        return (await BuildModelsAsync(context, [circle]))[0];
    }

    public async Task<CircleModel> RenameCircleAsync(string userId, string circlePublicId, string name)
    {
        var normalized = CircleRules.NormalizeName(name);

        await using var context = await _contextFactory.CreateDbContextAsync();
        var circle = await FindOwnedCircleAsync(context, userId, circlePublicId);
        var duplicate = await context.Circles.AnyAsync(c =>
            c.OwnerId == userId && !c.Deleted && c.Id != circle.Id && c.Name.ToLower() == normalized.ToLower());
        if (duplicate)
        {
            throw new ArgumentException("You already have a circle with that name.");
        }

        circle.Name = normalized;
        circle.UpdatedOn = DateTimeOffset.UtcNow;
        await context.SaveChangesAsync();
        return (await BuildModelsAsync(context, [circle]))[0];
    }

    public async Task DeleteCircleAsync(string userId, string circlePublicId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var circle = await FindOwnedCircleAsync(context, userId, circlePublicId);
        var now = DateTimeOffset.UtcNow;

        circle.Deleted = true;
        circle.UpdatedOn = now;
        await WithdrawAccessAsync(context, circle.Id, wishlistId: null, memberUserId: null);
        foreach (var member in await context.CircleMembers.Where(m => m.CircleId == circle.Id && !m.Deleted).ToListAsync())
        {
            member.Deleted = true;
            member.UpdatedOn = now;
        }

        foreach (var share in await context.CircleWishlists.Where(w => w.CircleId == circle.Id && !w.Deleted).ToListAsync())
        {
            share.Deleted = true;
            share.UpdatedOn = now;
        }

        await SaveCircleChangesAsync(context);
    }

    public async Task<CircleModel> AddMemberAsync(string userId, string circlePublicId, string memberUserId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(memberUserId);

        await using var context = await _contextFactory.CreateDbContextAsync();
        var circle = await FindOwnedCircleAsync(context, userId, circlePublicId);
        if (!await AreFriendsAsync(context, userId, memberUserId))
        {
            throw new InvalidOperationException("Only your friends can join a circle.");
        }

        var alreadyMember = await context.CircleMembers
            .AnyAsync(m => m.CircleId == circle.Id && m.UserId == memberUserId && !m.Deleted);
        if (!alreadyMember)
        {
            context.CircleMembers.Add(new CircleMember { CircleId = circle.Id, UserId = memberUserId });
            var sharedWishlistIds = await context.CircleWishlists
                .Where(w => w.CircleId == circle.Id && !w.Deleted && !w.Wishlist!.Deleted && !w.Wishlist.IsPrivate)
                .Select(w => w.WishlistId)
                .ToListAsync();
            foreach (var wishlistId in sharedWishlistIds)
            {
                await GrantAccessAsync(context, circle.Id, wishlistId, userId, memberUserId);
            }

            await SaveCircleChangesAsync(context, circle);
        }

        return (await BuildModelsAsync(context, [circle]))[0];
    }

    public async Task<CircleModel> RemoveMemberAsync(string userId, string circlePublicId, string memberUserId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var circle = await FindOwnedCircleAsync(context, userId, circlePublicId);
        await RemoveMemberCoreAsync(context, circle, memberUserId);
        await SaveCircleChangesAsync(context, circle);
        return (await BuildModelsAsync(context, [circle]))[0];
    }

    public async Task<CircleModel> ShareWishlistAsync(string userId, string circlePublicId, string wishlistPublicId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(wishlistPublicId);

        await using var context = await _contextFactory.CreateDbContextAsync();
        var circle = await FindOwnedCircleAsync(context, userId, circlePublicId);
        var wishlist = await FindOwnedWishlistAsync(context, userId, wishlistPublicId);
        if (wishlist.IsPrivate)
        {
            throw new InvalidOperationException("Make this wishlist visible to specific friends before sharing it with a circle.");
        }

        var alreadyShared = await context.CircleWishlists
            .AnyAsync(w => w.CircleId == circle.Id && w.WishlistId == wishlist.Id && !w.Deleted);
        if (!alreadyShared)
        {
            context.CircleWishlists.Add(new CircleWishlist { CircleId = circle.Id, WishlistId = wishlist.Id });
            var memberIds = await context.CircleMembers
                .Where(m => m.CircleId == circle.Id && !m.Deleted)
                .Select(m => m.UserId)
                .ToListAsync();
            foreach (var memberId in memberIds)
            {
                await GrantAccessAsync(context, circle.Id, wishlist.Id, userId, memberId);
            }

            await SaveCircleChangesAsync(context, circle);
        }

        return (await BuildModelsAsync(context, [circle]))[0];
    }

    public async Task<CircleModel> UnshareWishlistAsync(string userId, string circlePublicId, string wishlistPublicId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(wishlistPublicId);

        await using var context = await _contextFactory.CreateDbContextAsync();
        var circle = await FindOwnedCircleAsync(context, userId, circlePublicId);
        var wishlist = await FindOwnedWishlistAsync(context, userId, wishlistPublicId);

        var shares = await context.CircleWishlists
            .Where(w => w.CircleId == circle.Id && w.WishlistId == wishlist.Id && !w.Deleted)
            .ToListAsync();
        foreach (var share in shares)
        {
            share.Deleted = true;
            share.UpdatedOn = DateTimeOffset.UtcNow;
        }

        await WithdrawAccessAsync(context, circle.Id, wishlist.Id, memberUserId: null);
        await SaveCircleChangesAsync(context, circle);
        return (await BuildModelsAsync(context, [circle]))[0];
    }

    /// <summary>
    /// Takes someone out of all of an owner's circles, for example when they stop being friends, and withdraws the
    /// wishlist access those circles gave them. It only stages changes so the caller can save them together with its own.
    /// </summary>
    internal static async Task RemoveFromOwnerCirclesAsync(ApplicationDbContext context, string ownerId, string memberUserId)
    {
        var circles = await context.Circles
            .Where(c => c.OwnerId == ownerId && !c.Deleted &&
                context.CircleMembers.Any(m => m.CircleId == c.Id && m.UserId == memberUserId && !m.Deleted))
            .ToListAsync();
        foreach (var circle in circles)
        {
            await RemoveMemberCoreAsync(context, circle, memberUserId);
        }
    }

    private static async Task RemoveMemberCoreAsync(ApplicationDbContext context, Circle circle, string memberUserId)
    {
        var memberships = await context.CircleMembers
            .Where(m => m.CircleId == circle.Id && m.UserId == memberUserId && !m.Deleted)
            .ToListAsync();
        if (memberships.Count == 0)
        {
            return;
        }

        foreach (var membership in memberships)
        {
            membership.Deleted = true;
            membership.UpdatedOn = DateTimeOffset.UtcNow;
        }

        circle.UpdatedOn = DateTimeOffset.UtcNow;
        await WithdrawAccessAsync(context, circle.Id, wishlistId: null, memberUserId);
    }

    /// <summary>
    /// Stages removal of the access this circle granted. Each circle owns its own permission rows, so access a
    /// person was given by hand or through another circle is unaffected.
    /// </summary>
    private static async Task WithdrawAccessAsync(ApplicationDbContext context, int circleId, int? wishlistId, string? memberUserId)
    {
        var grants = await context.WishlistPermissions
            .Where(p => p.CircleId == circleId && !p.Deleted &&
                (wishlistId == null || p.WishlistId == wishlistId) &&
                (memberUserId == null || p.UserId == memberUserId))
            .ToListAsync();
        foreach (var grant in grants)
        {
            grant.Deleted = true;
            grant.UpdatedOn = DateTimeOffset.UtcNow;
        }
    }

    /// <summary>
    /// Saves everything staged by one operation atomically. Touching the circle row makes two overlapping
    /// operations on the same circle conflict instead of silently missing each other's grants.
    /// </summary>
    private static async Task SaveCircleChangesAsync(ApplicationDbContext context, Circle? circle = null)
    {
        if (circle is not null)
        {
            circle.UpdatedOn = DateTimeOffset.UtcNow;
        }

        try
        {
            await context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new InvalidOperationException("This circle changed at the same time. Try again.");
        }
    }

    private static async Task GrantAccessAsync(ApplicationDbContext context, int circleId, int wishlistId, string ownerId, string memberUserId)
    {
        if (memberUserId == ownerId)
        {
            return;
        }

        var existing = await context.WishlistPermissions
            .AnyAsync(p => p.WishlistId == wishlistId && p.UserId == memberUserId && p.CircleId == circleId && !p.Deleted);
        if (existing)
        {
            return;
        }

        context.WishlistPermissions.Add(new WishlistPermission
        {
            WishlistId = wishlistId,
            UserId = memberUserId,
            PermissionType = ViewPermission,
            CircleId = circleId
        });
    }

    private static async Task<bool> AreFriendsAsync(ApplicationDbContext context, string userId, string otherUserId) =>
        userId != otherUserId && await context.Friends.AnyAsync(f =>
            !f.Deleted &&
            ((f.UserId == userId && f.FriendUserId == otherUserId) ||
             (f.UserId == otherUserId && f.FriendUserId == userId)));

    private static async Task<Circle> FindOwnedCircleAsync(ApplicationDbContext context, string userId, string circlePublicId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ArgumentException.ThrowIfNullOrWhiteSpace(circlePublicId);

        return await context.Circles.FirstOrDefaultAsync(c => c.PublicId == circlePublicId && c.OwnerId == userId && !c.Deleted)
            ?? throw new KeyNotFoundException("Circle not found.");
    }

    private static async Task<Wishlist> FindOwnedWishlistAsync(ApplicationDbContext context, string userId, string wishlistPublicId) =>
        await context.Wishlists.FirstOrDefaultAsync(w => w.PublicId == wishlistPublicId && w.OwnerId == userId && !w.Deleted)
            ?? throw new KeyNotFoundException("Wishlist not found.");

    private static async Task<IReadOnlyList<CircleModel>> BuildModelsAsync(ApplicationDbContext context, IReadOnlyList<Circle> circles)
    {
        var circleIds = circles.Select(c => c.Id).ToList();
        var members = await context.CircleMembers
            .AsNoTracking()
            .Where(m => circleIds.Contains(m.CircleId) && !m.Deleted && m.User != null)
            .Select(m => new { m.CircleId, m.UserId, UserName = m.User!.UserName ?? m.User.Email ?? string.Empty })
            .ToListAsync();
        var wishlists = await context.CircleWishlists
            .AsNoTracking()
            .Where(w => circleIds.Contains(w.CircleId) && !w.Deleted && w.Wishlist != null && !w.Wishlist.Deleted)
            .Select(w => new { w.CircleId, w.Wishlist!.PublicId, w.Wishlist.Name })
            .ToListAsync();

        return circles
            .Select(circle => new CircleModel
            {
                PublicId = circle.PublicId,
                Name = circle.Name,
                Members = members
                    .Where(m => m.CircleId == circle.Id)
                    .OrderBy(m => m.UserName, StringComparer.OrdinalIgnoreCase)
                    .Select(m => new CircleMemberModel { UserId = m.UserId, UserName = m.UserName })
                    .ToList(),
                Wishlists = wishlists
                    .Where(w => w.CircleId == circle.Id)
                    .OrderBy(w => w.Name, StringComparer.OrdinalIgnoreCase)
                    .Select(w => new CircleWishlistModel { PublicId = w.PublicId, Name = w.Name })
                    .ToList()
            })
            .ToList();
    }
}