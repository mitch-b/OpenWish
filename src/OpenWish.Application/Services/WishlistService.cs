using System;
using System.Linq;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using OpenWish.Data;
using OpenWish.Data.Entities;
using OpenWish.Shared.Models;
using OpenWish.Shared.Services;

namespace OpenWish.Application.Services;

public class WishlistService(IDbContextFactory<ApplicationDbContext> contextFactory, IMapper mapper, IActivityService activityService) : IWishlistService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory = contextFactory;
    private readonly IMapper _mapper = mapper;
    private readonly IActivityService _activityService = activityService;

    public async Task<WishlistModel> CreateWishlistAsync(WishlistModel wishlistModel, string ownerId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var wishlistEntity = _mapper.Map<Wishlist>(wishlistModel);
        wishlistEntity.OwnerId = ownerId;

        if (wishlistModel.EventId.HasValue)
        {
            var eventEntity = await context.Events
                .Include(e => e.CreatedBy)
                .Include(e => e.EventUsers)
                .FirstOrDefaultAsync(e => e.Id == wishlistModel.EventId.Value && !e.Deleted)
                ?? throw new KeyNotFoundException($"Event {wishlistModel.EventId.Value} not found");

            if (!IsEventMember(eventEntity, ownerId))
            {
                throw new UnauthorizedAccessException("You must be part of the event to create a wishlist for it.");
            }

            wishlistEntity.EventId = wishlistModel.EventId;
        }

        var entry = context.Wishlists.Add(wishlistEntity);
        await context.SaveChangesAsync();

        // Log activity
        await _activityService.LogActivityAsync(
            ownerId,
            "WishlistCreated",
            $"Created wishlist: {wishlistEntity.Name}",
            wishlistEntity.Id);

        var resultModel = _mapper.Map<WishlistModel>(entry.Entity);
        return resultModel;
    }

    public async Task<WishlistModel> GetWishlistAsync(int id, string? userId = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var wishlistEntity = await context.Wishlists
            .Where(w => !w.Deleted)
            .Include(w => w.Items)
            .Include(w => w.Owner)
            .FirstOrDefaultAsync(w => w.Id == id);

        if (wishlistEntity == null)
        {
            throw new KeyNotFoundException($"Wishlist {id} not found");
        }

        // Check authorization if userId is provided
        if (!string.IsNullOrEmpty(userId))
        {
            var canAccess = await CanUserAccessWishlistInternalAsync(context, id, userId);
            if (!canAccess)
            {
                throw new UnauthorizedAccessException($"Access denied to wishlist {id}");
            }
        }

        var wishlistModel = _mapper.Map<WishlistModel>(wishlistEntity);
        return wishlistModel;
    }

    public async Task<IEnumerable<WishlistModel>> GetUserWishlistsAsync(string userId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var wishlistEntities = await context.Wishlists
            .Where(w => !w.Deleted && w.OwnerId == userId)
            .Include(w => w.Items)
            .Include(w => w.Owner)
            .ToListAsync();

        var wishlistModels = _mapper.Map<IEnumerable<WishlistModel>>(wishlistEntities);
        return wishlistModels;
    }

    public async Task<WishlistModel> UpdateWishlistAsync(int id, WishlistModel wishlistModel)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var existingWishlist = await context.Wishlists.FindAsync(id)
            ?? throw new KeyNotFoundException($"Wishlist {id} not found");

        // Map updated values to existing entity
        _mapper.Map(wishlistModel, existingWishlist);
        existingWishlist.UpdatedOn = DateTimeOffset.UtcNow;

        await context.SaveChangesAsync();

        // Log activity
        await _activityService.LogActivityAsync(
            existingWishlist.OwnerId,
            "WishlistUpdated",
            $"Updated wishlist: {existingWishlist.Name}",
            existingWishlist.Id);

        var updatedModel = _mapper.Map<WishlistModel>(existingWishlist);
        return updatedModel;
    }

    public async Task DeleteWishlistAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var wishlist = await context.Wishlists.FindAsync(id)
            ?? throw new KeyNotFoundException($"Wishlist {id} not found");

        wishlist.Deleted = true;
        wishlist.UpdatedOn = DateTimeOffset.UtcNow;

        // Log activity
        await _activityService.LogActivityAsync(
            wishlist.OwnerId,
            "WishlistDeleted",
            $"Deleted wishlist: {wishlist.Name}",
            wishlist.Id);

        await context.SaveChangesAsync();
    }

    public async Task<WishlistItemModel> AddItemToWishlistAsync(int wishlistId, WishlistItemModel itemModel)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var wishlist = await context.Wishlists.FindAsync(wishlistId)
            ?? throw new KeyNotFoundException($"Wishlist {wishlistId} not found");

        var itemEntity = _mapper.Map<WishlistItem>(itemModel);
        itemEntity.WishlistId = wishlistId;
        itemEntity.CreatedOn = DateTimeOffset.UtcNow;
        itemEntity.UpdatedOn = DateTimeOffset.UtcNow;
        itemEntity.OrderIndex = await context.WishlistItems
            .Where(i => i.WishlistId == wishlistId)
            .CountAsync();

        var entry = context.WishlistItems.Add(itemEntity);
        await context.SaveChangesAsync();

        // Log activity
        await _activityService.LogActivityAsync(
            wishlist.OwnerId,
            "ItemAdded",
            $"Added item: {itemEntity.Name} to wishlist {wishlist.Name}",
            wishlistId,
            itemEntity.Id);

        var resultModel = _mapper.Map<WishlistItemModel>(entry.Entity);
        return resultModel;
    }

    public async Task<bool> RemoveItemFromWishlistAsync(int wishlistId, int itemId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var item = await context.WishlistItems
            .FirstOrDefaultAsync(i => i.WishlistId == wishlistId && i.Id == itemId);

        if (item == null)
        {
            return false;
        }

        var wishlist = await context.Wishlists.FindAsync(wishlistId);

        context.WishlistItems.Remove(item);
        await context.SaveChangesAsync();

        // Log activity
        if (wishlist != null)
        {
            await _activityService.LogActivityAsync(
                wishlist.OwnerId,
                "ItemRemoved",
                $"Removed item: {item.Name} from wishlist {wishlist.Name}",
                wishlistId);
        }

        return true;
    }

    public async Task<WishlistItemModel> GetWishlistItemAsync(int wishlistId, int itemId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var itemEntity = await context.WishlistItems
            .FirstOrDefaultAsync(i => i.WishlistId == wishlistId && i.Id == itemId);

        if (itemEntity == null)
        {
            throw new KeyNotFoundException($"Item {itemId} not found in wishlist {wishlistId}");
        }

        var itemModel = _mapper.Map<WishlistItemModel>(itemEntity);
        return itemModel;
    }

    public async Task<IEnumerable<WishlistItemModel>> GetWishlistItemsAsync(int wishlistId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var itemEntities = await context.WishlistItems
            .Where(i => i.WishlistId == wishlistId && !i.Deleted)
            .OrderBy(i => i.OrderIndex)
            .ToListAsync();

        var itemModels = _mapper.Map<IEnumerable<WishlistItemModel>>(itemEntities);
        return itemModels;
    }

    public async Task<WishlistItemModel> UpdateWishlistItemAsync(int wishlistId, int itemId, WishlistItemModel itemModel)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var existingItem = await context.WishlistItems
            .FirstOrDefaultAsync(i => i.WishlistId == wishlistId && i.Id == itemId)
            ?? throw new KeyNotFoundException($"Item {itemId} not found in wishlist {wishlistId}");

        // Map updated values to existing entity
        _mapper.Map(itemModel, existingItem);
        existingItem.UpdatedOn = DateTimeOffset.UtcNow;

        await context.SaveChangesAsync();

        // Log activity
        var wishlist = await context.Wishlists.FindAsync(wishlistId);
        if (wishlist != null)
        {
            await _activityService.LogActivityAsync(
                wishlist.OwnerId,
                "ItemUpdated",
                $"Updated item: {existingItem.Name} in wishlist {wishlist.Name}",
                wishlistId,
                itemId);
        }

        var updatedModel = _mapper.Map<WishlistItemModel>(existingItem);
        return updatedModel;
    }

    // Wishlist sharing and permissions
    public async Task<WishlistPermissionModel> ShareWishlistAsync(int wishlistId, string userId, string permissionType)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var wishlist = await context.Wishlists.FindAsync(wishlistId)
            ?? throw new KeyNotFoundException($"Wishlist {wishlistId} not found");

        // Check if permission already exists
        var existingPermission = await context.WishlistPermissions
            .FirstOrDefaultAsync(wp => wp.WishlistId == wishlistId && wp.UserId == userId && !wp.Deleted);

        if (existingPermission != null)
        {
            existingPermission.PermissionType = permissionType;
            existingPermission.UpdatedOn = DateTimeOffset.UtcNow;
            await context.SaveChangesAsync();

            var updatedModel = _mapper.Map<WishlistPermissionModel>(existingPermission);
            return updatedModel;
        }

        // Create new permission
        var newPermission = new WishlistPermission
        {
            WishlistId = wishlistId,
            UserId = userId,
            PermissionType = permissionType,
            CreatedOn = DateTimeOffset.UtcNow,
            UpdatedOn = DateTimeOffset.UtcNow
        };

        context.WishlistPermissions.Add(newPermission);
        await context.SaveChangesAsync();

        // Log activity
        await _activityService.LogActivityAsync(
            wishlist.OwnerId,
            "WishlistShared",
            $"Shared wishlist: {wishlist.Name} with user",
            wishlistId);

        return _mapper.Map<WishlistPermissionModel>(newPermission);
    }

    public async Task<string> CreateSharingLinkAsync(int wishlistId, string permissionType, TimeSpan? expiration = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var wishlist = await context.Wishlists.FindAsync(wishlistId)
            ?? throw new KeyNotFoundException($"Wishlist {wishlistId} not found");

        // Create a unique token
        string token = Guid.NewGuid().ToString("N");

        // Create a wishlist permission with the token
        var permission = new WishlistPermission
        {
            WishlistId = wishlistId,
            PermissionType = permissionType,
            InvitationToken = token,
            ExpirationDate = expiration.HasValue ? DateTimeOffset.UtcNow.Add(expiration.Value) : null,
            CreatedOn = DateTimeOffset.UtcNow,
            UpdatedOn = DateTimeOffset.UtcNow
        };

        context.WishlistPermissions.Add(permission);
        await context.SaveChangesAsync();

        // Log activity
        await _activityService.LogActivityAsync(
            wishlist.OwnerId,
            "SharingLinkCreated",
            $"Created sharing link for wishlist: {wishlist.Name}",
            wishlistId);

        return token;
    }

    public async Task<bool> AcceptSharingLinkAsync(string token, string userId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var permission = await context.WishlistPermissions
            .FirstOrDefaultAsync(wp => wp.InvitationToken == token && !wp.Deleted);

        if (permission == null)
        {
            return false;
        }

        // Check if token has expired
        if (permission.ExpirationDate.HasValue && permission.ExpirationDate.Value < DateTimeOffset.UtcNow)
        {
            return false;
        }

        // Update the permission with the user ID and clear the token
        permission.UserId = userId;
        permission.InvitationToken = null;
        permission.UpdatedOn = DateTimeOffset.UtcNow;

        await context.SaveChangesAsync();

        // Log activity
        await _activityService.LogActivityAsync(
            userId,
            "SharingLinkAccepted",
            $"Accepted sharing invitation for wishlist",
            permission.WishlistId);

        return true;
    }

    public async Task<IEnumerable<WishlistPermissionModel>> GetWishlistPermissionsAsync(int wishlistId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var permissions = await context.WishlistPermissions
            .Include(wp => wp.User)
            .Where(wp => wp.WishlistId == wishlistId && wp.UserId != null && !wp.Deleted)
            .ToListAsync();

        return _mapper.Map<IEnumerable<WishlistPermissionModel>>(permissions);
    }

    public async Task<bool> RemoveWishlistPermissionAsync(int wishlistId, string userId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var permission = await context.WishlistPermissions
            .FirstOrDefaultAsync(wp => wp.WishlistId == wishlistId && wp.UserId == userId && !wp.Deleted);

        if (permission == null)
        {
            return false;
        }

        permission.Deleted = true;
        permission.UpdatedOn = DateTimeOffset.UtcNow;

        await context.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<WishlistModel>> GetSharedWithMeWishlistsAsync(string userId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var wishlists = await context.WishlistPermissions
            .Where(wp => wp.UserId == userId && !wp.Deleted)
            .Include(wp => wp.Wishlist)
                .ThenInclude(w => w.Owner)
            .Include(wp => wp.Wishlist)
                .ThenInclude(w => w.Items)
            .Select(wp => wp.Wishlist)
            .Where(w => !w.Deleted)
            .ToListAsync();

        return _mapper.Map<IEnumerable<WishlistModel>>(wishlists);
    }

    public async Task<IEnumerable<WishlistModel>> GetFriendsWishlistsAsync(string userId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        // Get all friends of the current user
        var friendIds = await context.Friends
            .Where(f => f.UserId == userId && !f.Deleted)
            .Select(f => f.FriendUserId)
            .Union(context.Friends
                .Where(f => f.FriendUserId == userId && !f.Deleted)
                .Select(f => f.UserId))
            .ToListAsync();

        // Get public wishlists (non-private) from friends
        var wishlists = await context.Wishlists
            .Where(w => friendIds.Contains(w.OwnerId) && !w.Deleted && !w.IsPrivate)
            .Include(w => w.Owner)
            .Include(w => w.Items)
            .ToListAsync();

        return _mapper.Map<IEnumerable<WishlistModel>>(wishlists);
    }

    public async Task<bool> CanUserAccessWishlistAsync(int wishlistId, string userId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await CanUserAccessWishlistInternalAsync(context, wishlistId, userId);
    }

    private static async Task<bool> CanUserAccessWishlistInternalAsync(ApplicationDbContext context, int wishlistId, string userId)
    {
        var wishlist = await context.Wishlists
            .FirstOrDefaultAsync(w => w.Id == wishlistId && !w.Deleted);

        if (wishlist == null)
        {
            return false;
        }

        if (wishlist.OwnerId == userId)
        {
            return true;
        }

        var hasPermission = await context.WishlistPermissions
            .AnyAsync(wp => wp.WishlistId == wishlistId && wp.UserId == userId && !wp.Deleted);

        if (hasPermission)
        {
            return true;
        }

        if (!wishlist.IsPrivate)
        {
            var isFriend = await context.Friends
                .AnyAsync(f =>
                    ((f.UserId == userId && f.FriendUserId == wishlist.OwnerId) ||
                    (f.UserId == wishlist.OwnerId && f.FriendUserId == userId)) &&
                    !f.Deleted);

            if (isFriend)
            {
                return true;
            }
        }

        if (wishlist.EventId.HasValue && await IsEventMemberAsync(context, wishlist.EventId.Value, userId))
        {
            return true;
        }

        return false;
    }

    public async Task<bool> CanUserEditWishlistAsync(int wishlistId, string userId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var wishlist = await context.Wishlists
            .Where(w => w.Id == wishlistId && !w.Deleted)
            .Select(w => new { w.OwnerId, w.IsCollaborative, w.EventId })
            .FirstOrDefaultAsync();

        if (wishlist == null)
        {
            return false;
        }

        if (wishlist.OwnerId == userId)
        {
            return true;
        }

        if (wishlist.IsCollaborative)
        {
            if (wishlist.EventId.HasValue && await IsEventMemberAsync(context, wishlist.EventId.Value, userId))
            {
                return true;
            }

            var hasCollabPermission = await context.WishlistPermissions
                .AnyAsync(wp => wp.WishlistId == wishlistId && wp.UserId == userId && !wp.Deleted);

            if (hasCollabPermission)
            {
                return true;
            }
        }

        var hasEditPermission = await context.WishlistPermissions
            .AnyAsync(wp => wp.WishlistId == wishlistId && wp.UserId == userId &&
                    (wp.PermissionType == "Edit" || wp.PermissionType == "Admin") &&
                    !wp.Deleted);

        return hasEditPermission;
    }

    // Item comments
    public async Task<ItemCommentModel> AddCommentToItemAsync(int wishlistId, int itemId, string userId, string text)
    {
        // Verify item exists
        await using var context = await _contextFactory.CreateDbContextAsync();
        var item = await context.WishlistItems
            .FirstOrDefaultAsync(i => i.Id == itemId && i.WishlistId == wishlistId && !i.Deleted)
            ?? throw new KeyNotFoundException($"Item {itemId} not found in wishlist {wishlistId}");

        var comment = new ItemComment
        {
            WishlistItemId = itemId,
            UserId = userId,
            Text = text,
            CreatedOn = DateTimeOffset.UtcNow,
            UpdatedOn = DateTimeOffset.UtcNow
        };

        context.ItemComments.Add(comment);
        await context.SaveChangesAsync();

        // Log activity
        await _activityService.LogActivityAsync(
            userId,
            "ItemCommentAdded",
            $"Comment added to item: {item.Name}",
            wishlistId,
            itemId);

        return _mapper.Map<ItemCommentModel>(comment);
    }

    public async Task<IEnumerable<ItemCommentModel>> GetItemCommentsAsync(int wishlistId, int itemId)
    {
        // Verify item exists
        await using var context = await _contextFactory.CreateDbContextAsync();
        var item = await context.WishlistItems
            .FirstOrDefaultAsync(i => i.Id == itemId && i.WishlistId == wishlistId && !i.Deleted)
            ?? throw new KeyNotFoundException($"Item {itemId} not found in wishlist {wishlistId}");

        var comments = await context.ItemComments
            .Include(c => c.User)
            .Where(c => c.WishlistItemId == itemId && !c.Deleted)
            .OrderBy(c => c.CreatedOn)
            .ToListAsync();

        return _mapper.Map<IEnumerable<ItemCommentModel>>(comments);
    }

    public async Task<bool> RemoveItemCommentAsync(int commentId, string userId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var comment = await context.ItemComments.FindAsync(commentId);

        if (comment == null || comment.Deleted)
        {
            return false;
        }

        // Only the comment author or the wishlist owner can delete comments
        var isCommentAuthor = comment.UserId == userId;

        if (!isCommentAuthor)
        {
            // Check if user is wishlist owner
            var item = await context.WishlistItems
                .Include(i => i.Wishlist)
                .FirstOrDefaultAsync(i => i.Id == comment.WishlistItemId);

            var isWishlistOwner = item?.Wishlist?.OwnerId == userId;

            if (!isWishlistOwner)
            {
                return false;
            }
        }

        comment.Deleted = true;
        comment.UpdatedOn = DateTimeOffset.UtcNow;

        await context.SaveChangesAsync();
        return true;
    }

    // Item reservations
    public async Task<bool> ReserveItemAsync(int wishlistId, int itemId, string userId, bool isAnonymous = false)
    {
        // Verify item exists
        await using var context = await _contextFactory.CreateDbContextAsync();
        var item = await context.WishlistItems
            .FirstOrDefaultAsync(i => i.Id == itemId && i.WishlistId == wishlistId && !i.Deleted)
            ?? throw new KeyNotFoundException($"Item {itemId} not found in wishlist {wishlistId}");

        // Check if item is already reserved
        var existingReservation = await context.ItemReservations
            .FirstOrDefaultAsync(r => r.WishlistItemId == itemId && !r.Deleted);

        if (existingReservation != null)
        {
            return false;
        }

        var reservation = new ItemReservation
        {
            WishlistItemId = itemId,
            UserId = userId,
            IsAnonymous = isAnonymous,
            ReservationDate = DateTimeOffset.UtcNow,
            CreatedOn = DateTimeOffset.UtcNow,
            UpdatedOn = DateTimeOffset.UtcNow
        };

        context.ItemReservations.Add(reservation);
        await context.SaveChangesAsync();

        // Log activity
        await _activityService.LogActivityAsync(
            userId,
            "ItemReserved",
            $"Reserved item: {item.Name}",
            wishlistId,
            itemId);

        return true;
    }

    public async Task<bool> CancelReservationAsync(int wishlistId, int itemId, string userId)
    {
        // Verify item exists
        await using var context = await _contextFactory.CreateDbContextAsync();
        var item = await context.WishlistItems
            .FirstOrDefaultAsync(i => i.Id == itemId && i.WishlistId == wishlistId && !i.Deleted)
            ?? throw new KeyNotFoundException($"Item {itemId} not found in wishlist {wishlistId}");

        var reservation = await context.ItemReservations
            .FirstOrDefaultAsync(r => r.WishlistItemId == itemId && r.UserId == userId && !r.Deleted);

        if (reservation == null)
        {
            return false;
        }

        reservation.Deleted = true;
        reservation.UpdatedOn = DateTimeOffset.UtcNow;

        await context.SaveChangesAsync();

        // Log activity
        await _activityService.LogActivityAsync(
            userId,
            "ReservationCanceled",
            $"Canceled reservation for item: {item.Name}",
            wishlistId,
            itemId);

        return true;
    }

    public async Task<ItemReservationModel?> GetItemReservationAsync(int wishlistId, int itemId)
    {
        // Verify item exists
        await using var context = await _contextFactory.CreateDbContextAsync();
        var item = await context.WishlistItems
            .FirstOrDefaultAsync(i => i.Id == itemId && i.WishlistId == wishlistId && !i.Deleted)
            ?? throw new KeyNotFoundException($"Item {itemId} not found in wishlist {wishlistId}");

        var reservation = await context.ItemReservations
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.WishlistItemId == itemId && !r.Deleted);

        return reservation != null ? _mapper.Map<ItemReservationModel>(reservation) : null;
    }

    public async Task<bool> IsItemReservedAsync(int itemId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.ItemReservations
            .AnyAsync(r => r.WishlistItemId == itemId && !r.Deleted);
    }

    private static bool IsEventMember(Event eventEntity, string userId)
    {
        if (string.Equals(eventEntity.CreatedBy?.Id, userId, StringComparison.Ordinal))
        {
            return true;
        }

        return eventEntity.EventUsers.Any(eu =>
            !eu.Deleted &&
            string.Equals(eu.UserId, userId, StringComparison.Ordinal));
    }

    private static async Task<bool> IsEventMemberAsync(ApplicationDbContext context, int eventId, string userId)
    {
        var eventEntity = await context.Events
            .Include(e => e.CreatedBy)
            .Include(e => e.EventUsers)
            .FirstOrDefaultAsync(e => e.Id == eventId && !e.Deleted);

        return eventEntity != null && IsEventMember(eventEntity, userId);
    }
}