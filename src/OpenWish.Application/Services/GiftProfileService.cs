using Microsoft.EntityFrameworkCore;
using OpenWish.Data;
using OpenWish.Shared.Models;
using OpenWish.Shared.Services;

namespace OpenWish.Application.Services;

public class GiftProfileService(IDbContextFactory<ApplicationDbContext> contextFactory) : IGiftProfileService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory = contextFactory;

    public async Task<GiftProfileModel> GetMyProfileAsync(string userId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Users
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new GiftProfileModel { Sizes = u.GiftSizes, Favorites = u.GiftFavorites, Avoid = u.GiftAvoid })
            .FirstOrDefaultAsync() ?? new GiftProfileModel();
    }

    public async Task SetMyProfileAsync(string userId, GiftProfileModel profile)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ArgumentNullException.ThrowIfNull(profile);

        var sizes = Normalize(profile.Sizes, "Sizes");
        var favorites = Normalize(profile.Favorites, "Favorites");
        var avoid = Normalize(profile.Avoid, "Things to avoid");

        await using var context = await _contextFactory.CreateDbContextAsync();
        var user = await context.Users.FirstOrDefaultAsync(u => u.Id == userId)
            ?? throw new KeyNotFoundException("User not found.");
        user.GiftSizes = sizes;
        user.GiftFavorites = favorites;
        user.GiftAvoid = avoid;
        await context.SaveChangesAsync();
    }

    public async Task<GiftProfileModel?> GetProfileForWishlistAsync(string wishlistPublicId, string viewerId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(wishlistPublicId);
        ArgumentException.ThrowIfNullOrWhiteSpace(viewerId);

        await using var context = await _contextFactory.CreateDbContextAsync();
        var wishlist = await context.Wishlists
            .AsNoTracking()
            .Where(w => !w.Deleted && w.PublicId == wishlistPublicId)
            .Select(w => new { w.Id, w.OwnerId })
            .FirstOrDefaultAsync();
        if (wishlist is null || wishlist.OwnerId == viewerId)
        {
            return null;
        }

        var isFriend = await context.Friends
            .AsNoTracking()
            .AnyAsync(f => !f.Deleted &&
                ((f.UserId == viewerId && f.FriendUserId == wishlist.OwnerId) ||
                 (f.UserId == wishlist.OwnerId && f.FriendUserId == viewerId)));
        if (!isFriend || !await WishlistService.CanUserAccessWishlistInternalAsync(context, wishlist.Id, viewerId))
        {
            return null;
        }

        var profile = await context.Users
            .AsNoTracking()
            .Where(u => u.Id == wishlist.OwnerId)
            .Select(u => new GiftProfileModel { Sizes = u.GiftSizes, Favorites = u.GiftFavorites, Avoid = u.GiftAvoid })
            .FirstOrDefaultAsync();
        return profile is { HasContent: true } ? profile : null;
    }

    private static string? Normalize(string? value, string label)
    {
        var trimmed = value?.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            return null;
        }

        return trimmed.Length > GiftProfileModel.MaxLength
            ? throw new ArgumentException($"{label} can be at most {GiftProfileModel.MaxLength} characters.")
            : trimmed;
    }
}