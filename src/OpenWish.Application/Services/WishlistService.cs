using Microsoft.EntityFrameworkCore;
using OpenWish.Data;
using OpenWish.Data.Entities;

namespace OpenWish.Application.Services;

public interface IWishlistService
{
    Task<Wishlist> CreateWishlistAsync(Wishlist wishlist, string ownerId);
    Task<Wishlist> GetWishlistAsync(int id);
    Task<IEnumerable<Wishlist>> GetUserWishlistsAsync(string userId);
    Task<Wishlist> UpdateWishlistAsync(int id, Wishlist wishlist);
    Task DeleteWishlistAsync(int id);
    
    // WishlistItems
    Task<WishlistItem> GetWishlistItemAsync(int wishlistId, int itemId);
    Task<IEnumerable<WishlistItem>> GetWishlistItemsAsync(int wishlistId);
    Task<WishlistItem> AddItemToWishlistAsync(int wishlistId, WishlistItem item);
    Task<bool> RemoveItemFromWishlistAsync(int wishlistId, int itemId);
    Task<WishlistItem> UpdateWishlistItemAsync(int wishlistId, int itemId, WishlistItem item);

}

public class WishlistService(ApplicationDbContext context) : IWishlistService
{
    private readonly ApplicationDbContext _context = context;

    public async Task<Wishlist> CreateWishlistAsync(Wishlist wishlist, string ownerId)
    {
        var newWishlist = new Wishlist
        {
            Name = wishlist.Name,
            OwnerId = ownerId,
            IsCollaborative = wishlist.IsCollaborative
        };

        _context.Wishlists.Add(newWishlist);
        await _context.SaveChangesAsync();
        return newWishlist;
    }

    public async Task<Wishlist> GetWishlistAsync(int id)
    {
        var wishlist = await _context.Wishlists
            .Where(w => !w.Deleted)
            .Include(w => w.Items)
            .Include(w => w.Owner)
            .FirstOrDefaultAsync(w => w.Id == id);

        return wishlist ?? throw new KeyNotFoundException($"Wishlist {id} not found");
    }

    public async Task<IEnumerable<Wishlist>> GetUserWishlistsAsync(string userId)
    {
        return await _context.Wishlists
            .Where(w => !w.Deleted)
            .Include(w => w.Items)
            .Include(w => w.Owner)
            .Where(w => w.Owner.Id == userId)
            .ToListAsync();
    }

    public async Task<Wishlist> UpdateWishlistAsync(int id, Wishlist wishlist)
    {
        var editedWishlist = await _context.Wishlists.FindAsync(id) 
            ?? throw new KeyNotFoundException($"Wishlist {id} not found");

        editedWishlist.Name = wishlist.Name;
        editedWishlist.IsCollaborative = wishlist.IsCollaborative;
        editedWishlist.UpdatedOn = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        
        return wishlist;
    }

    public async Task DeleteWishlistAsync(int id)
    {
        var wishlist = await _context.Wishlists.FindAsync(id);
        if (wishlist != null)
        {
            _context.Wishlists.Remove(wishlist);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<WishlistItem> AddItemToWishlistAsync(int wishlistId, WishlistItem item)
    {
        var wishlist = await GetWishlistAsync(wishlistId);
        
        item.WishlistId = wishlistId;
        item.CreatedOn = DateTime.UtcNow;
        item.UpdatedOn = DateTime.UtcNow;
        item.OrderIndex = await _context.WishlistItems
            .Where(i => i.WishlistId == wishlistId)
            .CountAsync();

        _context.WishlistItems.Add(item);
        await _context.SaveChangesAsync();
        return item;
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

    public async Task<WishlistItem> GetWishlistItemAsync(int wishlistId, int itemId)
    {
        return await _context.WishlistItems
            .FirstOrDefaultAsync(i => i.WishlistId == wishlistId && i.Id == itemId)
            ?? throw new KeyNotFoundException($"Item {itemId} not found in wishlist {wishlistId}");
    }

    public async Task<IEnumerable<WishlistItem>> GetWishlistItemsAsync(int wishlistId)
    {
        return await _context.WishlistItems
            .Where(i => i.WishlistId == wishlistId && !i.Deleted)
            .OrderBy(i => i.OrderIndex)
            .ToListAsync();
    }

    public async Task<WishlistItem> UpdateWishlistItemAsync(int wishlistId, int itemId, WishlistItem item)
    {
        var existingItem = await GetWishlistItemAsync(wishlistId, itemId);
        
        existingItem.Name = item.Name;
        existingItem.Description = item.Description;
        existingItem.Price = item.Price;
        existingItem.Url = item.Url;
        existingItem.WhereToBuy = item.WhereToBuy;
        existingItem.Priority = item.Priority;
        existingItem.IsPrivate = item.IsPrivate;
        existingItem.UpdatedOn = DateTime.UtcNow;
        
        await _context.SaveChangesAsync();
        return existingItem;
    }
}
