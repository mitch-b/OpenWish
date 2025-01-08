using AutoMapper;
using Microsoft.EntityFrameworkCore;
using OpenWish.Data;
using OpenWish.Data.Entities;
using OpenWish.Shared.Models;
using OpenWish.Shared.Services;

namespace OpenWish.Application.Services;

public class WishlistService(ApplicationDbContext context, IMapper mapper) : IWishlistService
{
    private readonly ApplicationDbContext _context = context;
    private readonly IMapper _mapper = mapper;

    public async Task<WishlistModel> CreateWishlistAsync(WishlistModel wishlistModel, string ownerId)
    {
        var wishlistEntity = _mapper.Map<Wishlist>(wishlistModel);
        wishlistEntity.OwnerId = ownerId;

        var entry = _context.Wishlists.Add(wishlistEntity);
        await _context.SaveChangesAsync();

        var resultModel = _mapper.Map<WishlistModel>(entry.Entity);
        return resultModel;
    }

    public async Task<WishlistModel> GetWishlistAsync(int id)
    {
        var wishlistEntity = await _context.Wishlists
            .Where(w => !w.Deleted)
            .Include(w => w.Items)
            .Include(w => w.Owner)
            .FirstOrDefaultAsync(w => w.Id == id);

        if (wishlistEntity == null)
            throw new KeyNotFoundException($"Wishlist {id} not found");

        var wishlistModel = _mapper.Map<WishlistModel>(wishlistEntity);
        return wishlistModel;
    }

    public async Task<IEnumerable<WishlistModel>> GetUserWishlistsAsync(string userId)
    {
        var wishlistEntities = await _context.Wishlists
            .Where(w => !w.Deleted && w.OwnerId == userId)
            .Include(w => w.Items)
            .Include(w => w.Owner)
            .ToListAsync();

        var wishlistModels = _mapper.Map<IEnumerable<WishlistModel>>(wishlistEntities);
        return wishlistModels;
    }

    public async Task<WishlistModel> UpdateWishlistAsync(int id, WishlistModel wishlistModel)
    {
        var existingWishlist = await _context.Wishlists.FindAsync(id)
            ?? throw new KeyNotFoundException($"Wishlist {id} not found");

        // Map updated values to existing entity
        _mapper.Map(wishlistModel, existingWishlist);
        existingWishlist.UpdatedOn = DateTimeOffset.UtcNow;

        await _context.SaveChangesAsync();

        var updatedModel = _mapper.Map<WishlistModel>(existingWishlist);
        return updatedModel;
    }

    public async Task DeleteWishlistAsync(int id)
    {
        var wishlist = await _context.Wishlists.FindAsync(id)
            ?? throw new KeyNotFoundException($"Wishlist {id} not found");

        wishlist.Deleted = true;
        wishlist.UpdatedOn = DateTimeOffset.UtcNow;
        await _context.SaveChangesAsync();
    }

    public async Task<WishlistItemModel> AddItemToWishlistAsync(int wishlistId, WishlistItemModel itemModel)
    {
        var wishlist = await _context.Wishlists.FindAsync(wishlistId)
            ?? throw new KeyNotFoundException($"Wishlist {wishlistId} not found");

        var itemEntity = _mapper.Map<WishlistItem>(itemModel);
        itemEntity.WishlistId = wishlistId;
        itemEntity.CreatedOn = DateTimeOffset.UtcNow;
        itemEntity.UpdatedOn = DateTimeOffset.UtcNow;
        itemEntity.OrderIndex = await _context.WishlistItems
            .Where(i => i.WishlistId == wishlistId)
            .CountAsync();

        var entry = _context.WishlistItems.Add(itemEntity);
        await _context.SaveChangesAsync();

        var resultModel = _mapper.Map<WishlistItemModel>(entry.Entity);
        return resultModel;
    }

    public async Task<bool> RemoveItemFromWishlistAsync(int wishlistId, int itemId)
    {
        var item = await _context.WishlistItems
            .FirstOrDefaultAsync(i => i.WishlistId == wishlistId && i.Id == itemId);

        if (item == null)
            return false;

        _context.WishlistItems.Remove(item);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<WishlistItemModel> GetWishlistItemAsync(int wishlistId, int itemId)
    {
        var itemEntity = await _context.WishlistItems
            .FirstOrDefaultAsync(i => i.WishlistId == wishlistId && i.Id == itemId);

        if (itemEntity == null)
            throw new KeyNotFoundException($"Item {itemId} not found in wishlist {wishlistId}");

        var itemModel = _mapper.Map<WishlistItemModel>(itemEntity);
        return itemModel;
    }

    public async Task<IEnumerable<WishlistItemModel>> GetWishlistItemsAsync(int wishlistId)
    {
        var itemEntities = await _context.WishlistItems
            .Where(i => i.WishlistId == wishlistId && !i.Deleted)
            .OrderBy(i => i.OrderIndex)
            .ToListAsync();

        var itemModels = _mapper.Map<IEnumerable<WishlistItemModel>>(itemEntities);
        return itemModels;
    }

    public async Task<WishlistItemModel> UpdateWishlistItemAsync(int wishlistId, int itemId, WishlistItemModel itemModel)
    {
        var existingItem = await _context.WishlistItems
            .FirstOrDefaultAsync(i => i.WishlistId == wishlistId && i.Id == itemId)
            ?? throw new KeyNotFoundException($"Item {itemId} not found in wishlist {wishlistId}");

        // Map updated values to existing entity
        _mapper.Map(itemModel, existingItem);
        existingItem.UpdatedOn = DateTimeOffset.UtcNow;

        await _context.SaveChangesAsync();

        var updatedModel = _mapper.Map<WishlistItemModel>(existingItem);
        return updatedModel;
    }
}
