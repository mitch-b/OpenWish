using Microsoft.EntityFrameworkCore;
using OpenWish.Application.Services;
using OpenWish.Data;
using OpenWish.Data.Entities;
using Xunit;

namespace OpenWish.Application.Tests.Services;

public class GiftPlanServiceTests
{
    [Fact]
    public async Task GetGiftPlanAsync_GroupsOnlyTheCallersReservationsByRecipient()
    {
        var factory = CreateFactory();
        await using (var seed = factory.CreateDbContext())
        {
            var (me, other, jordan, casey) = AddUsers(seed);
            AddFriends(seed, "me", "jordan", "casey");
            var jordanList = new Wishlist { Name = "Jordan birthday", OwnerId = jordan.Id };
            var caseyList = new Wishlist { Name = "Casey list", OwnerId = casey.Id };
            var scarf = new WishlistItem { Name = "Scarf", Price = 30m, Wishlist = jordanList };
            var book = new WishlistItem { Name = "Book", Price = 12.5m, Wishlist = jordanList };
            var lamp = new WishlistItem { Name = "Lamp", Wishlist = caseyList };
            var tea = new WishlistItem { Name = "Tea", Price = 8m, Wishlist = caseyList };
            seed.WishlistItems.AddRange(scarf, book, lamp, tea);
            await seed.SaveChangesAsync();
            seed.ItemReservations.AddRange(
                Reserve(scarf, me.Id), Reserve(book, me.Id), Reserve(lamp, me.Id), Reserve(tea, other.Id));
            await seed.SaveChangesAsync();
        }

        var plan = await new GiftPlanService(factory).GetGiftPlanAsync("me");

        Assert.Equal(["casey", "jordan"], plan.Recipients.Select(r => r.RecipientName));
        Assert.Equal(3, plan.ItemCount);
        Assert.Equal(42.5m, plan.Total);
        Assert.Equal(1, plan.UnpricedCount);
        Assert.DoesNotContain(plan.Recipients.SelectMany(r => r.Items), i => i.ItemName == "Tea");
        Assert.Equal(["Scarf", "Book"], plan.Recipients[1].Items.Select(i => i.ItemName));
    }

    [Fact]
    public async Task GetGiftPlanAsync_OmitsGiftsTheCallerCanNoLongerOpenOrThatAreGone()
    {
        var factory = CreateFactory();
        await using (var seed = factory.CreateDbContext())
        {
            var (me, _, jordan, casey) = AddUsers(seed);
            AddFriends(seed, "me", "jordan");
            var visible = new WishlistItem { Name = "Visible", Wishlist = new Wishlist { Name = "Open", OwnerId = jordan.Id } };
            var removedItem = new WishlistItem { Name = "Removed", Deleted = true, Wishlist = new Wishlist { Name = "Open too", OwnerId = jordan.Id } };
            var privateItem = new WishlistItem { Name = "Private", IsPrivate = true, Wishlist = new Wishlist { Name = "Open three", OwnerId = jordan.Id } };
            var strangerList = new WishlistItem { Name = "Stranger", Wishlist = new Wishlist { Name = "Not a friend", OwnerId = casey.Id } };
            var closedList = new WishlistItem { Name = "Closed", Wishlist = new Wishlist { Name = "Now private", OwnerId = jordan.Id, IsPrivate = true } };
            var deletedList = new WishlistItem { Name = "Deleted list", Wishlist = new Wishlist { Name = "Gone", OwnerId = jordan.Id, Deleted = true } };
            var ownList = new WishlistItem { Name = "Mine", Wishlist = new Wishlist { Name = "My own", OwnerId = me.Id } };
            var items = new[] { visible, removedItem, privateItem, strangerList, closedList, deletedList, ownList };
            seed.WishlistItems.AddRange(items);
            await seed.SaveChangesAsync();
            seed.ItemReservations.AddRange(items.Select(i => Reserve(i, me.Id)));
            await seed.SaveChangesAsync();
        }

        var plan = await new GiftPlanService(factory).GetGiftPlanAsync("me");

        Assert.Equal(["Visible"], plan.Recipients.SelectMany(r => r.Items).Select(i => i.ItemName));
    }

    [Fact]
    public async Task GetGiftPlanAsync_NeverShowsAnEmailAndSkipsCancelledReservations()
    {
        var factory = CreateFactory();
        await using (var seed = factory.CreateDbContext())
        {
            var me = new ApplicationUser { Id = "me", UserName = "me" };
            var emailOwner = new ApplicationUser { Id = "emailOwner", UserName = "someone@example.test", Email = "someone@example.test" };
            seed.Users.AddRange(me, emailOwner);
            AddFriends(seed, "me", "emailOwner");
            var kept = new WishlistItem { Name = "Kept", Wishlist = new Wishlist { Name = "List", OwnerId = "emailOwner" } };
            var cancelled = new WishlistItem { Name = "Cancelled", Wishlist = kept.Wishlist };
            seed.WishlistItems.AddRange(kept, cancelled);
            await seed.SaveChangesAsync();
            var released = Reserve(cancelled, "me");
            released.Deleted = true;
            seed.ItemReservations.AddRange(Reserve(kept, "me"), released);
            await seed.SaveChangesAsync();
        }

        var plan = await new GiftPlanService(factory).GetGiftPlanAsync("me");

        var recipient = Assert.Single(plan.Recipients);
        Assert.DoesNotContain("@", recipient.RecipientName);
        Assert.Equal(["Kept"], recipient.Items.Select(i => i.ItemName));
    }

    [Fact]
    public async Task GetGiftPlanAsync_ReturnsAnEmptyPlanWithoutReservations()
    {
        var plan = await new GiftPlanService(CreateFactory()).GetGiftPlanAsync("me");

        Assert.Empty(plan.Recipients);
        Assert.Equal(0, plan.ItemCount);
        Assert.Equal(0m, plan.Total);
    }

    [Fact]
    public async Task SetGiftPurchasedAsync_MarksAndClearsOnlyTheCallersOwnReservation()
    {
        var factory = CreateFactory();
        string wishlistId;
        await using (var seed = factory.CreateDbContext())
        {
            var (me, other, jordan, _) = AddUsers(seed);
            AddFriends(seed, "me", "jordan");
            AddFriends(seed, "other", "jordan");
            var list = new Wishlist { Name = "List", OwnerId = jordan.Id };
            var mine = new WishlistItem { Name = "Mine", Price = 10m, Wishlist = list };
            var theirs = new WishlistItem { Name = "Theirs", Price = 20m, Wishlist = list };
            seed.WishlistItems.AddRange(mine, theirs);
            await seed.SaveChangesAsync();
            seed.ItemReservations.AddRange(Reserve(mine, me.Id), Reserve(theirs, other.Id));
            await seed.SaveChangesAsync();
            wishlistId = list.PublicId;
        }

        var service = new GiftPlanService(factory);
        var minePublicId = (await service.GetGiftPlanAsync("me")).Recipients[0].Items[0].ItemPublicId;

        await service.SetGiftPurchasedAsync("me", wishlistId, minePublicId, true);
        var bought = await service.GetGiftPlanAsync("me");
        Assert.Equal(1, bought.PurchasedCount);
        Assert.Equal(0, bought.ToBuyCount);
        Assert.Equal(0m, bought.ToBuyTotal);

        await service.SetGiftPurchasedAsync("me", wishlistId, minePublicId, false);
        var cleared = await service.GetGiftPlanAsync("me");
        Assert.Equal(1, cleared.ToBuyCount);
        Assert.Equal(10m, cleared.ToBuyTotal);

        var theirPublicId = (await new GiftPlanService(factory).GetGiftPlanAsync("other")).Recipients[0].Items[0].ItemPublicId;
        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.SetGiftPurchasedAsync("me", wishlistId, theirPublicId, true));
        await using var verify = factory.CreateDbContext();
        Assert.All(verify.ItemReservations.Where(r => r.UserId == "other"), r => Assert.Null(r.PurchasedOn));
    }

    [Fact]
    public async Task SetGiftPurchasedAsync_RejectsGiftsTheCallerCanNoLongerOpen()
    {
        var factory = CreateFactory();
        string wishlistId;
        string itemId;
        await using (var seed = factory.CreateDbContext())
        {
            var (me, _, jordan, _) = AddUsers(seed);
            var list = new Wishlist { Name = "Closed", OwnerId = jordan.Id, IsPrivate = true };
            var item = new WishlistItem { Name = "Gift", Wishlist = list };
            seed.WishlistItems.Add(item);
            await seed.SaveChangesAsync();
            seed.ItemReservations.Add(Reserve(item, me.Id));
            await seed.SaveChangesAsync();
            wishlistId = list.PublicId;
            itemId = item.PublicId;
        }

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => new GiftPlanService(factory).SetGiftPurchasedAsync("me", wishlistId, itemId, true));
    }

    private static (ApplicationUser Me, ApplicationUser Other, ApplicationUser Jordan, ApplicationUser Casey) AddUsers(ApplicationDbContext context)
    {
        var users = (
            Me: new ApplicationUser { Id = "me", UserName = "me" },
            Other: new ApplicationUser { Id = "other", UserName = "other" },
            Jordan: new ApplicationUser { Id = "jordan", UserName = "jordan" },
            Casey: new ApplicationUser { Id = "casey", UserName = "casey" });
        context.Users.AddRange(users.Me, users.Other, users.Jordan, users.Casey);
        return users;
    }

    private static void AddFriends(ApplicationDbContext context, string userId, params string[] friendIds)
    {
        foreach (var friendId in friendIds)
        {
            context.Friends.Add(new Friend { UserId = userId, FriendUserId = friendId });
        }
    }

    private static ItemReservation Reserve(WishlistItem item, string userId) => new()
    {
        WishlistItem = item,
        UserId = userId,
        ReservationDate = DateTimeOffset.UtcNow.AddMinutes(item.Id)
    };

    private static TestFactory CreateFactory() => new();

    private sealed class TestFactory : IDbContextFactory<ApplicationDbContext>
    {
        private readonly string _name = Guid.NewGuid().ToString();

        public ApplicationDbContext CreateDbContext() =>
            new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(_name).Options);
    }
}