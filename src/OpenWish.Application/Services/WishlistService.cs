using System;
using System.Collections.Generic;
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
            .Include(w => w.Items.Where(i => !i.Deleted))
            .Include(w => w.Owner)
            .FirstOrDefaultAsync(w => w.Id == id && !w.Deleted);

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
        FilterWishlistItemsForViewer(wishlistModel, userId);
        return wishlistModel;
    }

    public async Task<WishlistModel> GetWishlistByPublicIdAsync(string publicId, string? userId = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var wishlistEntity = await context.Wishlists
            .Include(w => w.Items.Where(i => !i.Deleted))
            .Include(w => w.Owner)
            .FirstOrDefaultAsync(w => w.PublicId == publicId && !w.Deleted);

        if (wishlistEntity == null)
        {
            throw new KeyNotFoundException($"Wishlist {publicId} not found");
        }

        // Check authorization if userId is provided
        if (!string.IsNullOrEmpty(userId))
        {
            var canAccess = await CanUserAccessWishlistInternalAsync(context, wishlistEntity.Id, userId);
            if (!canAccess)
            {
                throw new UnauthorizedAccessException($"Access denied to wishlist {publicId}");
            }
        }

        var wishlistModel = _mapper.Map<WishlistModel>(wishlistEntity);
        FilterWishlistItemsForViewer(wishlistModel, userId);
        return wishlistModel;
    }

    public async Task<IEnumerable<WishlistModel>> GetUserWishlistsAsync(string userId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var wishlistEntities = await context.Wishlists
            .Where(w => !w.Deleted && w.OwnerId == userId)
            .Include(w => w.Items.Where(i => !i.Deleted))
            .Include(w => w.Owner)
            .ToListAsync();

        var wishlistModels = _mapper.Map<List<WishlistModel>>(wishlistEntities);

        foreach (var wishlist in wishlistModels)
        {
            FilterWishlistItemsForViewer(wishlist, userId);
        }

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

    public async Task<WishlistModel> UpdateWishlistByPublicIdAsync(string publicId, WishlistModel wishlistModel)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var existingWishlist = await context.Wishlists
            .FirstOrDefaultAsync(w => w.PublicId == publicId && !w.Deleted)
            ?? throw new KeyNotFoundException($"Wishlist {publicId} not found");

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

    public async Task DeleteWishlistByPublicIdAsync(string publicId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var wishlist = await context.Wishlists
            .FirstOrDefaultAsync(w => w.PublicId == publicId && !w.Deleted)
            ?? throw new KeyNotFoundException($"Wishlist {publicId} not found");

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
        var maxOrderIndex = await context.WishlistItems
            .Where(i => i.WishlistId == wishlistId && !i.Deleted)
            .MaxAsync(i => i.OrderIndex);

        itemEntity.OrderIndex = (maxOrderIndex ?? -1) + 1;

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
            .Include(i => i.Wishlist)
            .FirstOrDefaultAsync(i => i.WishlistId == wishlistId && i.Id == itemId && !i.Deleted);

        if (item == null)
        {
            return false;
        }

        var wishlist = item.Wishlist;
        var utcNow = DateTimeOffset.UtcNow;

        item.Deleted = true;
        item.UpdatedOn = utcNow;

        // Cascade soft delete to dependents to keep relationships consistent
        await context.ItemReservations
            .Where(r => r.WishlistItemId == itemId && !r.Deleted)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(r => r.Deleted, _ => true)
                .SetProperty(r => r.UpdatedOn, _ => utcNow));

        await context.ItemComments
            .Where(c => c.WishlistItemId == itemId && !c.Deleted)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(c => c.Deleted, _ => true)
                .SetProperty(c => c.UpdatedOn, _ => utcNow));

        await context.ItemReactions
            .Where(r => r.WishlistItemId == itemId && !r.Deleted)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(r => r.Deleted, _ => true)
                .SetProperty(r => r.UpdatedOn, _ => utcNow));

        await context.WillPurchases
            .Where(wp => wp.WishlistItemId == itemId && !wp.Deleted)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(wp => wp.Deleted, _ => true)
                .SetProperty(wp => wp.UpdatedOn, _ => utcNow));

        await context.SaveChangesAsync();

        wishlist ??= await context.Wishlists.FindAsync(wishlistId);

        if (wishlist != null)
        {
            await _activityService.LogActivityAsync(
                wishlist.OwnerId,
                "ItemRemoved",
                $"Removed item: {item.Name} from wishlist {wishlist.Name}",
                wishlistId,
                item.Id);
        }

        return true;
    }

    public async Task<WishlistItemModel> GetWishlistItemAsync(int wishlistId, int itemId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var itemEntity = await context.WishlistItems
            .FirstOrDefaultAsync(i => i.WishlistId == wishlistId && i.Id == itemId && !i.Deleted);

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
        var query = context.WishlistItems
            .Where(i => i.WishlistId == wishlistId && !i.Deleted)
            .OrderBy(i => i.OrderIndex);

        return await MapWishlistItemsAsync(context, query);
    }

    public async Task<WishlistItemModel> UpdateWishlistItemAsync(int wishlistId, int itemId, WishlistItemModel itemModel)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var existingItem = await context.WishlistItems
            .FirstOrDefaultAsync(i => i.WishlistId == wishlistId && i.Id == itemId && !i.Deleted)
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
                .ThenInclude(w => w.Items.Where(i => !i.Deleted))
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

        // Get wishlists that the user has explicit permission to access (for friends-only wishlists)
        var permittedWishlistIds = await context.WishlistPermissions
            .Where(wp => wp.UserId == userId && !wp.Deleted)
            .Select(wp => wp.WishlistId)
            .ToListAsync();

        // Get public wishlists (non-private, not friends-only) from friends OR wishlists with explicit permissions
        var wishlists = await context.Wishlists
            .Where(w => !w.Deleted && !w.IsPrivate &&
                ((friendIds.Contains(w.OwnerId) && !w.IsFriendsOnly) ||
                 permittedWishlistIds.Contains(w.Id)))
            .Include(w => w.Owner)
            .Include(w => w.Items.Where(i => !i.Deleted))
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

        // If wishlist is not private and not friends-only, check if user is a friend
        if (!wishlist.IsPrivate && !wishlist.IsFriendsOnly)
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
                .FirstOrDefaultAsync(i => i.Id == comment.WishlistItemId && !i.Deleted);

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

    // PublicId-based methods for public routes
    public async Task<IEnumerable<WishlistItemModel>> GetWishlistItemsByPublicIdAsync(string wishlistPublicId, string? requestingUserId = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var wishlist = await context.Wishlists
            .FirstOrDefaultAsync(w => w.PublicId == wishlistPublicId && !w.Deleted)
            ?? throw new KeyNotFoundException($"Wishlist {wishlistPublicId} not found");

        var query = context.WishlistItems
            .Where(i => i.WishlistId == wishlist.Id && !i.Deleted)
            .OrderBy(i => i.OrderIndex);

        var items = await MapWishlistItemsAsync(context, query);
        return FilterWishlistItemsForViewer(items, wishlist.OwnerId, requestingUserId);
    }

    public async Task<WishlistItemModel> GetWishlistItemByPublicIdAsync(string wishlistPublicId, int itemId, string? requestingUserId = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var wishlist = await context.Wishlists
            .FirstOrDefaultAsync(w => w.PublicId == wishlistPublicId && !w.Deleted)
            ?? throw new KeyNotFoundException($"Wishlist {wishlistPublicId} not found");

        var itemEntity = await context.WishlistItems
            .FirstOrDefaultAsync(i => i.WishlistId == wishlist.Id && i.Id == itemId && !i.Deleted)
            ?? throw new KeyNotFoundException($"Item {itemId} not found in wishlist {wishlistPublicId}");

        if (!CanViewerSeeItem(itemEntity, wishlist.OwnerId, requestingUserId))
        {
            throw new KeyNotFoundException($"Item {itemId} not found in wishlist {wishlistPublicId}");
        }

        return _mapper.Map<WishlistItemModel>(itemEntity);
    }

    public async Task<WishlistItemModel> AddItemToWishlistByPublicIdAsync(string wishlistPublicId, WishlistItemModel itemModel)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var wishlist = await context.Wishlists
            .FirstOrDefaultAsync(w => w.PublicId == wishlistPublicId && !w.Deleted)
            ?? throw new KeyNotFoundException($"Wishlist {wishlistPublicId} not found");

        return await AddItemToWishlistAsync(wishlist.Id, itemModel);
    }

    public async Task<bool> RemoveItemFromWishlistByPublicIdAsync(string wishlistPublicId, int itemId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var wishlist = await context.Wishlists
            .FirstOrDefaultAsync(w => w.PublicId == wishlistPublicId && !w.Deleted)
            ?? throw new KeyNotFoundException($"Wishlist {wishlistPublicId} not found");

        return await RemoveItemFromWishlistAsync(wishlist.Id, itemId);
    }

    public async Task<WishlistItemModel> UpdateWishlistItemByPublicIdAsync(string wishlistPublicId, int itemId, WishlistItemModel item)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var wishlist = await context.Wishlists
            .FirstOrDefaultAsync(w => w.PublicId == wishlistPublicId && !w.Deleted)
            ?? throw new KeyNotFoundException($"Wishlist {wishlistPublicId} not found");

        return await UpdateWishlistItemAsync(wishlist.Id, itemId, item);
    }

    public async Task<bool> CanUserAccessWishlistByPublicIdAsync(string wishlistPublicId, string userId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var wishlist = await context.Wishlists
            .FirstOrDefaultAsync(w => w.PublicId == wishlistPublicId && !w.Deleted)
            ?? throw new KeyNotFoundException($"Wishlist {wishlistPublicId} not found");

        return await CanUserAccessWishlistInternalAsync(context, wishlist.Id, userId);
    }

    public async Task<bool> CanUserEditWishlistByPublicIdAsync(string wishlistPublicId, string userId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var wishlist = await context.Wishlists
            .FirstOrDefaultAsync(w => w.PublicId == wishlistPublicId && !w.Deleted)
            ?? throw new KeyNotFoundException($"Wishlist {wishlistPublicId} not found");

        return await CanUserEditWishlistAsync(wishlist.Id, userId);
    }

    public async Task<WishlistPermissionModel> ShareWishlistByPublicIdAsync(string wishlistPublicId, string userId, string permissionType)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var wishlist = await context.Wishlists
            .FirstOrDefaultAsync(w => w.PublicId == wishlistPublicId && !w.Deleted)
            ?? throw new KeyNotFoundException($"Wishlist {wishlistPublicId} not found");

        return await ShareWishlistAsync(wishlist.Id, userId, permissionType);
    }

    public async Task<string> CreateSharingLinkByPublicIdAsync(string wishlistPublicId, string permissionType, TimeSpan? expiration = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var wishlist = await context.Wishlists
            .FirstOrDefaultAsync(w => w.PublicId == wishlistPublicId && !w.Deleted)
            ?? throw new KeyNotFoundException($"Wishlist {wishlistPublicId} not found");

        return await CreateSharingLinkAsync(wishlist.Id, permissionType, expiration);
    }

    public async Task<IEnumerable<WishlistPermissionModel>> GetWishlistPermissionsByPublicIdAsync(string wishlistPublicId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var wishlist = await context.Wishlists
            .FirstOrDefaultAsync(w => w.PublicId == wishlistPublicId && !w.Deleted)
            ?? throw new KeyNotFoundException($"Wishlist {wishlistPublicId} not found");

        return await GetWishlistPermissionsAsync(wishlist.Id);
    }

    public async Task<bool> RemoveWishlistPermissionByPublicIdAsync(string wishlistPublicId, string userId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var wishlist = await context.Wishlists
            .FirstOrDefaultAsync(w => w.PublicId == wishlistPublicId && !w.Deleted)
            ?? throw new KeyNotFoundException($"Wishlist {wishlistPublicId} not found");

        return await RemoveWishlistPermissionAsync(wishlist.Id, userId);
    }

    public async Task<ItemCommentModel> AddCommentToItemByPublicIdAsync(string wishlistPublicId, int itemId, string userId, string text)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var wishlist = await context.Wishlists
            .FirstOrDefaultAsync(w => w.PublicId == wishlistPublicId && !w.Deleted)
            ?? throw new KeyNotFoundException($"Wishlist {wishlistPublicId} not found");

        return await AddCommentToItemAsync(wishlist.Id, itemId, userId, text);
    }

    public async Task<IEnumerable<ItemCommentModel>> GetItemCommentsByPublicIdAsync(string wishlistPublicId, int itemId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var wishlist = await context.Wishlists
            .FirstOrDefaultAsync(w => w.PublicId == wishlistPublicId && !w.Deleted)
            ?? throw new KeyNotFoundException($"Wishlist {wishlistPublicId} not found");

        return await GetItemCommentsAsync(wishlist.Id, itemId);
    }

    public async Task<bool> ReserveItemByPublicIdAsync(string wishlistPublicId, int itemId, string userId, bool isAnonymous = false)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var wishlist = await context.Wishlists
            .FirstOrDefaultAsync(w => w.PublicId == wishlistPublicId && !w.Deleted)
            ?? throw new KeyNotFoundException($"Wishlist {wishlistPublicId} not found");

        return await ReserveItemAsync(wishlist.Id, itemId, userId, isAnonymous);
    }

    public async Task<bool> CancelReservationByPublicIdAsync(string wishlistPublicId, int itemId, string userId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var wishlist = await context.Wishlists
            .FirstOrDefaultAsync(w => w.PublicId == wishlistPublicId && !w.Deleted)
            ?? throw new KeyNotFoundException($"Wishlist {wishlistPublicId} not found");

        return await CancelReservationAsync(wishlist.Id, itemId, userId);
    }

    public async Task<ItemReservationModel?> GetItemReservationByPublicIdAsync(string wishlistPublicId, int itemId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var wishlist = await context.Wishlists
            .FirstOrDefaultAsync(w => w.PublicId == wishlistPublicId && !w.Deleted)
            ?? throw new KeyNotFoundException($"Wishlist {wishlistPublicId} not found");

        return await GetItemReservationAsync(wishlist.Id, itemId);
    }

    public async Task<IEnumerable<ApplicationUserModel>> GetFriendsWithAccessAsync(int wishlistId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var wishlist = await context.Wishlists
            .FirstOrDefaultAsync(w => w.Id == wishlistId && !w.Deleted)
            ?? throw new KeyNotFoundException($"Wishlist {wishlistId} not found");

        // Get all friends of the wishlist owner
        var allFriends = await context.Friends
            .AsNoTracking()
            .Include(f => f.FriendUser)
            .Where(f => f.UserId == wishlist.OwnerId && !f.Deleted)
            .Select(f => f.FriendUser)
            .ToListAsync();

        // If the wishlist is private, no friends have access (only explicit permissions)
        if (wishlist.IsPrivate)
        {
            // Return only friends who have explicit permissions
            var permittedUserIds = await context.WishlistPermissions
                .Where(wp => wp.WishlistId == wishlistId && wp.UserId != null && !wp.Deleted)
                .Select(wp => wp.UserId)
                .ToListAsync();

            var friendsWithPermission = allFriends.Where(f => permittedUserIds.Contains(f.Id)).ToList();
            return _mapper.Map<IEnumerable<ApplicationUserModel>>(friendsWithPermission);
        }

        // If the wishlist is friends-only, only friends with explicit permissions have access
        if (wishlist.IsFriendsOnly)
        {
            var permittedUserIds = await context.WishlistPermissions
                .Where(wp => wp.WishlistId == wishlistId && wp.UserId != null && !wp.Deleted)
                .Select(wp => wp.UserId)
                .ToListAsync();

            var friendsWithPermission = allFriends.Where(f => permittedUserIds.Contains(f.Id)).ToList();
            return _mapper.Map<IEnumerable<ApplicationUserModel>>(friendsWithPermission);
        }

        // If the wishlist is public to friends, all friends have access
        return _mapper.Map<IEnumerable<ApplicationUserModel>>(allFriends);
    }

    public async Task<IEnumerable<ApplicationUserModel>> GetFriendsWithAccessByPublicIdAsync(string wishlistPublicId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var wishlist = await context.Wishlists
            .FirstOrDefaultAsync(w => w.PublicId == wishlistPublicId && !w.Deleted)
            ?? throw new KeyNotFoundException($"Wishlist {wishlistPublicId} not found");

        return await GetFriendsWithAccessAsync(wishlist.Id);
    }

    private async Task<List<WishlistItemModel>> MapWishlistItemsAsync(ApplicationDbContext context, IQueryable<WishlistItem> query)
    {
        var itemEntities = await query.ToListAsync();
        if (itemEntities.Count == 0)
        {
            return [];
        }

        var itemModels = _mapper.Map<List<WishlistItemModel>>(itemEntities);
        var itemIds = itemEntities.Select(i => i.Id).ToList();

        var reservationEntities = await context.ItemReservations
            .AsNoTracking()
            .Include(r => r.User)
            .Where(r => !r.Deleted && itemIds.Contains(r.WishlistItemId))
            .ToListAsync();

        if (reservationEntities.Count == 0)
        {
            return itemModels;
        }

        var reservationsByItem = reservationEntities
            .GroupBy(r => r.WishlistItemId)
            .ToDictionary(
                g => g.Key,
                g => (ICollection<ItemReservationModel>)_mapper.Map<List<ItemReservationModel>>(g.ToList()),
                EqualityComparer<int>.Default);

        foreach (var item in itemModels)
        {
            if (reservationsByItem.TryGetValue(item.Id, out var reservations))
            {
                item.Reservations = reservations;
            }
            else if (item.Reservations.Count > 0)
            {
                item.Reservations = [];
            }
        }

        return itemModels;
    }

    private static void FilterWishlistItemsForViewer(WishlistModel wishlist, string? requestingUserId)
    {
        if (string.IsNullOrEmpty(requestingUserId) || wishlist.Items is null || wishlist.Items.Count == 0)
        {
            return;
        }

        var filteredItems = FilterWishlistItemsForViewer(wishlist.Items, wishlist.OwnerId, requestingUserId).ToList();

        if (filteredItems.Count != wishlist.Items.Count)
        {
            wishlist.Items = filteredItems;
        }
    }

    private static IEnumerable<WishlistItemModel> FilterWishlistItemsForViewer(IEnumerable<WishlistItemModel> items, string? ownerId, string? requestingUserId)
    {
        if (string.IsNullOrEmpty(requestingUserId))
        {
            return items;
        }

        var itemList = items as ICollection<WishlistItemModel> ?? items.ToList();
        if (itemList.Count == 0)
        {
            return itemList;
        }

        var isOwner = !string.IsNullOrEmpty(ownerId) &&
                      !string.IsNullOrEmpty(requestingUserId) &&
                      string.Equals(ownerId, requestingUserId, StringComparison.Ordinal);

        return itemList.Where(item => ShouldExposeItem(isOwner, item.IsPrivate, item.IsHiddenFromOwner)).ToList();
    }

    private static bool CanViewerSeeItem(WishlistItem item, string? ownerId, string? requestingUserId)
    {
        if (string.IsNullOrEmpty(requestingUserId))
        {
            return true;
        }

        var isOwner = !string.IsNullOrEmpty(ownerId) &&
                      !string.IsNullOrEmpty(requestingUserId) &&
                      string.Equals(ownerId, requestingUserId, StringComparison.Ordinal);

        return ShouldExposeItem(isOwner, item.IsPrivate, item.IsHiddenFromOwner);
    }

    private static bool ShouldExposeItem(bool isOwner, bool isPrivate, bool isHiddenFromOwner)
    {
        if (isPrivate && !isOwner)
        {
            return false;
        }

        if (isHiddenFromOwner && isOwner)
        {
            return false;
        }

        return true;
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