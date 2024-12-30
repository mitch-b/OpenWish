using Microsoft.EntityFrameworkCore;
using OpenWish.Data;
using OpenWish.Data.Entities;

namespace OpenWish.Application.Services;

public interface IWishlistService
{
    Task<Wishlist> CreateWishlistAsync(string name, string ownerId);
    Task<Wishlist> GetWishlistAsync(int id);
    Task<IEnumerable<Wishlist>> GetUserWishlistsAsync(string userId);
    Task<Wishlist> UpdateWishlistAsync(int id, string name);
    Task DeleteWishlistAsync(int id);
    Task<WishlistItem> AddItemToWishlistAsync(int wishlistId, WishlistItem item);
    Task<bool> RemoveItemFromWishlistAsync(int wishlistId, int itemId);
}

public class WishlistService(ApplicationDbContext context) : IWishlistService
{
    private readonly ApplicationDbContext _context = context;

    public async Task<Wishlist> CreateWishlistAsync(string name, string ownerId)
    {
        var wishlist = new Wishlist
        {
            Name = name,
            OwnerId = ownerId,
            IsCollaborative = false
        };

        _context.Wishlists.Add(wishlist);
        await _context.SaveChangesAsync();
        return wishlist;
    }

    public async Task<Wishlist> GetWishlistAsync(int id)
    {
        var wishlist = await _context.Wishlists
            .Include(w => w.Items)
            .Include(w => w.Owner)
            .FirstOrDefaultAsync(w => w.Id == id);

        return wishlist ?? throw new KeyNotFoundException($"Wishlist {id} not found");
    }

    public async Task<IEnumerable<Wishlist>> GetUserWishlistsAsync(string userId)
    {
        return await _context.Wishlists
            .Include(w => w.Items)
            .Include(w => w.Owner)
            .Where(w => w.Owner.Id == userId)
            .ToListAsync();
    }

    public async Task<Wishlist> UpdateWishlistAsync(int id, string name)
    {
        var wishlist = await _context.Wishlists.FindAsync(id) 
            ?? throw new KeyNotFoundException($"Wishlist {id} not found");
        
        wishlist.Name = name;
        wishlist.UpdatedOn = DateTime.UtcNow;
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
        var wishlist = await _context.Wishlists
            .Include(w => w.Items)
            .FirstOrDefaultAsync(w => w.Id == wishlistId)
            ?? throw new KeyNotFoundException($"Wishlist {wishlistId} not found");

        item.WishlistId = wishlistId;
        item.CreatedOn = DateTime.UtcNow;
        
        wishlist.Items.Add(item);
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
}
