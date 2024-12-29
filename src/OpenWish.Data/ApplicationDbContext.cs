using OpenWish.Data.Entities;
using OpenWish.Data.Extensions;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace OpenWish.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<OpenWishUser> OpenWishUsers { get; set; }
    public DbSet<Event> Events { get; set; }
    public DbSet<Wishlist> Wishlists { get; set; }
    public DbSet<WishlistItem> WishlistItems { get; set; }
    public DbSet<WillPurchase> WillPurchases { get; set; }
    public DbSet<Notification> Notifications { get; set; }
    public DbSet<GiftExchange> GiftExchanges { get; set; }
    public DbSet<CustomPairingRule> CustomPairingRules { get; set; }
    public DbSet<PublicWishlist> PublicWishlists { get; set; }
    public DbSet<WishlistComment> WishlistComments { get; set; }
    public DbSet<WishlistReaction> WishlistReactions { get; set; }
    public DbSet<Comment> Comments { get; set; }
    public DbSet<ItemReaction> ItemReactions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Event>()
            .HasOne(e => e.CopiedFromEvent)
            .WithMany()
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<Event>()
            .Property(e => e.Budget)
            .HasColumnType("decimal(11,2)");

        modelBuilder.Entity<CustomPairingRule>()
            .HasOne(cpr => cpr.SourceOpenWishUser)
            .WithMany()
            .OnDelete(DeleteBehavior.NoAction);
        
        modelBuilder.Entity<CustomPairingRule>()
            .HasOne(cpr => cpr.TargetOpenWishUser)
            .WithMany()
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<EventUser>()
            .HasOne(eu => eu.OpenWishUser)
            .WithMany()
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<GiftExchange>()
            .HasOne(ge => ge.Receiver)
            .WithMany()
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<GiftExchange>()
            .Property(e => e.Budget)
            .HasColumnType("decimal(11,2)");
        
        modelBuilder.Entity<GiftExchange>()
            .HasOne(ge => ge.Giver)
            .WithMany()
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<WishlistItem>()
            .Property(wli => wli.Price)
            .HasColumnType("decimal(11,2)");

        SqlDefaultValueAttributeConvention.Apply(modelBuilder);
    }

    public async Task<int> SaveChangesAsync()
    {
        return await base.SaveChangesAsync();
    }
}