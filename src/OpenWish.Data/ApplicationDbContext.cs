using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using OpenWish.Data.Entities;
using OpenWish.Data.Extensions;

namespace OpenWish.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<Event> Events { get; set; }
    public DbSet<EventUser> EventUsers { get; set; }
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

    // Social features
    public DbSet<Friend> Friends { get; set; }
    public DbSet<FriendRequest> FriendRequests { get; set; }
    public DbSet<WishlistPermission> WishlistPermissions { get; set; }
    public DbSet<ItemComment> ItemComments { get; set; }
    public DbSet<ItemReservation> ItemReservations { get; set; }
    public DbSet<ActivityLog> ActivityLogs { get; set; }

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
            .HasOne(cpr => cpr.SourceUser)
            .WithMany()
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<CustomPairingRule>()
            .HasOne(cpr => cpr.TargetUser)
            .WithMany()
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<EventUser>()
            .HasOne(eu => eu.User)
            .WithMany()
            .HasForeignKey(eu => eu.UserId)
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

        modelBuilder.Entity<WishlistComment>()
            .HasOne(wc => wc.User)
            .WithMany()
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<WishlistReaction>()
            .HasOne(wr => wr.User)
            .WithMany()
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<Comment>()
            .HasOne(c => c.User)
            .WithMany()
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<ItemReaction>()
            .HasOne(ir => ir.User)
            .WithMany()
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<Notification>()
            .HasOne(n => n.User)
            .WithMany()
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<PublicWishlist>()
            .HasOne(pw => pw.Owner)
            .WithMany()
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<Wishlist>()
            .HasOne(w => w.Owner)
            .WithMany()
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<WillPurchase>()
            .HasOne(wp => wp.Purchaser)
            .WithMany()
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<EventUser>()
            .HasKey(eu => eu.Id);

        modelBuilder.Entity<EventUser>()
            .HasOne(eu => eu.Event)
            .WithMany(e => e.EventUsers)
            .HasForeignKey(eu => eu.EventId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<EventUser>()
            .HasOne(eu => eu.User)
            .WithMany(u => u.EventUsers)
            .HasForeignKey(eu => eu.UserId)
            .OnDelete(DeleteBehavior.NoAction);

        // Social features - Friend relationships
        modelBuilder.Entity<Friend>()
            .HasKey(f => new { f.UserId, f.FriendUserId });

        modelBuilder.Entity<Friend>()
            .HasOne(f => f.User)
            .WithMany(u => u.Friends)
            .HasForeignKey(f => f.UserId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<Friend>()
            .HasOne(f => f.FriendUser)
            .WithMany()
            .HasForeignKey(f => f.FriendUserId)
            .OnDelete(DeleteBehavior.NoAction);

        // Friend requests
        modelBuilder.Entity<FriendRequest>()
            .HasOne(fr => fr.Requester)
            .WithMany(u => u.SentFriendRequests)
            .HasForeignKey(fr => fr.RequesterId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<FriendRequest>()
            .HasOne(fr => fr.Receiver)
            .WithMany(u => u.ReceivedFriendRequests)
            .HasForeignKey(fr => fr.ReceiverId)
            .OnDelete(DeleteBehavior.NoAction);

        // Wishlist permissions
        modelBuilder.Entity<WishlistPermission>()
            .HasOne(wp => wp.User)
            .WithMany(u => u.WishlistPermissions)
            .HasForeignKey(wp => wp.UserId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<WishlistPermission>()
            .HasOne(wp => wp.Wishlist)
            .WithMany()
            .HasForeignKey(wp => wp.WishlistId)
            .OnDelete(DeleteBehavior.NoAction);

        // Item comments
        modelBuilder.Entity<ItemComment>()
            .HasOne(ic => ic.User)
            .WithMany()
            .HasForeignKey(ic => ic.UserId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<ItemComment>()
            .HasOne(ic => ic.WishlistItem)
            .WithMany()
            .HasForeignKey(ic => ic.WishlistItemId)
            .OnDelete(DeleteBehavior.NoAction);

        // Item reservations
        modelBuilder.Entity<ItemReservation>()
            .HasOne(ir => ir.User)
            .WithMany()
            .HasForeignKey(ir => ir.UserId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<ItemReservation>()
            .HasOne(ir => ir.WishlistItem)
            .WithMany()
            .HasForeignKey(ir => ir.WishlistItemId)
            .OnDelete(DeleteBehavior.NoAction);

        // Activity logs
        modelBuilder.Entity<ActivityLog>()
            .HasOne(al => al.User)
            .WithMany(u => u.Activities)
            .HasForeignKey(al => al.UserId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<ActivityLog>()
            .HasOne(al => al.Wishlist)
            .WithMany()
            .HasForeignKey(al => al.WishlistId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<ActivityLog>()
            .HasOne(al => al.WishlistItem)
            .WithMany()
            .HasForeignKey(al => al.WishlistItemId)
            .OnDelete(DeleteBehavior.NoAction);

        SqlDefaultValueAttributeConvention.Apply(modelBuilder);
    }

    public async Task<int> SaveChangesAsync()
    {
        return await base.SaveChangesAsync();
    }
}