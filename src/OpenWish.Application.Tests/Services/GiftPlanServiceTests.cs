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

    [Fact]
    public async Task GetGiftPlanAsync_HidesEventDetailsFromNonMembersWhoCanStillOpenTheList()
    {
        var factory = CreateFactory();
        await using (var seed = factory.CreateDbContext())
        {
            var (me, _, jordan, casey) = AddUsers(seed);
            AddFriends(seed, "me", "jordan", "casey");
            var secret = new Event { Name = "Surprise party", CreatedBy = jordan, Date = DateTimeOffset.UtcNow.AddDays(5) };
            var shared = new Event { Name = "Family swap", CreatedBy = casey, Date = DateTimeOffset.UtcNow.AddDays(5) };
            shared.EventUsers.Add(new EventUser { UserId = "me", Status = "Accepted" });
            var hidden = new WishlistItem { Name = "Hidden event", Wishlist = new Wishlist { Name = "A", OwnerId = jordan.Id, Event = secret } };
            var member = new WishlistItem { Name = "Member event", Wishlist = new Wishlist { Name = "B", OwnerId = casey.Id, Event = shared } };
            seed.WishlistItems.AddRange(hidden, member);
            await seed.SaveChangesAsync();
            seed.ItemReservations.AddRange(Reserve(hidden, me.Id), Reserve(member, me.Id));
            await seed.SaveChangesAsync();
        }

        var items = (await new GiftPlanService(factory).GetGiftPlanAsync("me")).Recipients.SelectMany(r => r.Items).ToList();

        var hiddenItem = Assert.Single(items, i => i.ItemName == "Hidden event");
        Assert.Null(hiddenItem.EventName);
        Assert.Null(hiddenItem.EventPublicId);
        Assert.Equal("Family swap", Assert.Single(items, i => i.ItemName == "Member event").EventName);
    }

    [Fact]
    public async Task SetGiftPurchasedAsync_DoesNotTouchSharedReservationTimestamps()
    {
        var factory = CreateFactory();
        var original = DateTimeOffset.UtcNow.AddDays(-3);
        await using (var seed = factory.CreateDbContext())
        {
            var (me, _, jordan, _) = AddUsers(seed);
            AddFriends(seed, "me", "jordan");
            var item = new WishlistItem { Name = "Scarf", Wishlist = new Wishlist { Name = "List", OwnerId = jordan.Id } };
            seed.WishlistItems.Add(item);
            await seed.SaveChangesAsync();
            var reservation = Reserve(item, me.Id);
            reservation.UpdatedOn = original;
            seed.ItemReservations.Add(reservation);
            await seed.SaveChangesAsync();
        }

        await using var read = factory.CreateDbContext();
        var plan = await new GiftPlanService(factory).GetGiftPlanAsync("me");
        var listId = plan.Recipients[0].Items[0].WishlistPublicId;
        var itemId = plan.Recipients[0].Items[0].ItemPublicId;
        await new GiftPlanService(factory).SetGiftPurchasedAsync("me", listId, itemId, true);

        var stored = await read.ItemReservations.AsNoTracking().SingleAsync();
        Assert.NotNull(stored.PurchasedOn);
        Assert.Equal(original, stored.UpdatedOn);
    }

    [Fact]
    public async Task GetGiftPlanAsync_MovesBoughtGiftsOfPastOccasionsIntoPrivateHistory()
    {
        var factory = CreateFactory();
        await using (var seed = factory.CreateDbContext())
        {
            var (me, _, jordan, casey) = AddUsers(seed);
            AddFriends(seed, "me", "jordan", "casey");
            var past = new Event { Name = "Last birthday", CreatedBy = jordan, Date = DateTimeOffset.UtcNow.AddDays(-10) };
            var pastList = new Wishlist { Name = "Old", OwnerId = jordan.Id, Event = past };
            var plain = new Wishlist { Name = "Plain", OwnerId = casey.Id };
            var oldGift = new WishlistItem { Name = "Old gift", Wishlist = pastList };
            var olderGift = new WishlistItem { Name = "Older gift", Wishlist = pastList };
            var oldNoEvent = new WishlistItem { Name = "Old plain", Wishlist = plain };
            var recentNoEvent = new WishlistItem { Name = "Recent plain", Wishlist = plain };
            var unbought = new WishlistItem { Name = "Unbought", Wishlist = pastList };
            seed.WishlistItems.AddRange(oldGift, olderGift, oldNoEvent, recentNoEvent, unbought);
            await seed.SaveChangesAsync();
            var reservations = new[]
            {
                Reserve(oldGift, me.Id), Reserve(olderGift, me.Id), Reserve(oldNoEvent, me.Id),
                Reserve(recentNoEvent, me.Id), Reserve(unbought, me.Id)
            };
            reservations[0].PurchasedOn = DateTimeOffset.UtcNow.AddDays(-12);
            reservations[1].PurchasedOn = DateTimeOffset.UtcNow.AddDays(-20);
            reservations[2].PurchasedOn = DateTimeOffset.UtcNow.AddDays(-45);
            reservations[3].PurchasedOn = DateTimeOffset.UtcNow.AddDays(-2);
            seed.ItemReservations.AddRange(reservations);
            await seed.SaveChangesAsync();
        }

        var plan = await new GiftPlanService(factory).GetGiftPlanAsync("me");

        Assert.Equal(["casey", "jordan"], plan.History.Select(r => r.RecipientName));
        Assert.Equal(["Old plain"], plan.History[0].Items.Select(i => i.ItemName));
        Assert.Equal(["Old gift", "Older gift"], plan.History[1].Items.Select(i => i.ItemName));
        Assert.Equal(["Recent plain", "Unbought"], plan.Recipients.SelectMany(r => r.Items).Select(i => i.ItemName).Order());

        var other = await new GiftPlanService(factory).GetGiftPlanAsync("other");
        Assert.Empty(other.History);
        var owner = await new GiftPlanService(factory).GetGiftPlanAsync("jordan");
        Assert.Empty(owner.History);
    }

    [Fact]
    public async Task GetPreviouslyGivenMatchesAsync_MatchesByUrlOrNameForTheSameRecipientOnly()
    {
        var factory = CreateFactory();
        int urlTarget, nameTarget, otherRecipientTarget, noMatchTarget;
        string jordanListId, caseyListId;
        await using (var seed = factory.CreateDbContext())
        {
            var (me, other, jordan, casey) = AddUsers(seed);
            AddFriends(seed, "me", "jordan", "casey");
            AddFriends(seed, "other", "jordan");
            var old = new Wishlist { Name = "Old", OwnerId = jordan.Id };
            var fresh = new Wishlist { Name = "New", OwnerId = jordan.Id };
            var caseyList = new Wishlist { Name = "Casey", OwnerId = casey.Id };
            var givenUrl = new WishlistItem { Name = "Kettle", Url = "https://www.shop.test/kettle/", Wishlist = old };
            var givenName = new WishlistItem { Name = "Blue Scarf", Wishlist = old };
            var givenToCasey = new WishlistItem { Name = "Mug", Wishlist = caseyList };
            var newUrl = new WishlistItem { Name = "Electric kettle", Url = "http://shop.test/kettle", Wishlist = fresh };
            var newName = new WishlistItem { Name = " blue scarf ", Wishlist = fresh };
            var newMug = new WishlistItem { Name = "Mug", Wishlist = fresh };
            var newOther = new WishlistItem { Name = "Different", Wishlist = fresh };
            seed.WishlistItems.AddRange(givenUrl, givenName, givenToCasey, newUrl, newName, newMug, newOther);
            await seed.SaveChangesAsync();
            var reservations = new[] { Reserve(givenUrl, me.Id), Reserve(givenName, me.Id), Reserve(givenToCasey, me.Id) };
            foreach (var r in reservations)
            {
                r.PurchasedOn = DateTimeOffset.UtcNow.AddDays(-3);
            }

            seed.ItemReservations.AddRange(reservations);
            await seed.SaveChangesAsync();
            urlTarget = newUrl.Id;
            nameTarget = newName.Id;
            otherRecipientTarget = newMug.Id;
            noMatchTarget = newOther.Id;
            jordanListId = fresh.PublicId;
            caseyListId = caseyList.PublicId;
        }

        var service = new GiftPlanService(factory);

        var byUrl = Assert.Single(await service.GetPreviouslyGivenMatchesAsync("me", jordanListId, urlTarget));
        Assert.Equal("jordan", byUrl.RecipientName);
        Assert.Equal("Kettle", byUrl.ItemName);
        Assert.Equal("Blue Scarf", Assert.Single(await service.GetPreviouslyGivenMatchesAsync("me", jordanListId, nameTarget)).ItemName);
        Assert.Empty(await service.GetPreviouslyGivenMatchesAsync("me", jordanListId, otherRecipientTarget));
        Assert.Empty(await service.GetPreviouslyGivenMatchesAsync("me", jordanListId, noMatchTarget));
        Assert.Empty(await service.GetPreviouslyGivenMatchesAsync("other", jordanListId, urlTarget));
        Assert.Empty(await service.GetPreviouslyGivenMatchesAsync("jordan", jordanListId, urlTarget));
        Assert.NotEmpty(caseyListId);
    }

    [Fact]
    public async Task GetPreviouslyGivenMatchesAsync_IgnoresUnboughtAndInaccessibleLists()
    {
        var factory = CreateFactory();
        int unboughtTarget, closedTarget;
        string openId, closedId;
        await using (var seed = factory.CreateDbContext())
        {
            var (me, _, jordan, casey) = AddUsers(seed);
            AddFriends(seed, "me", "jordan");
            var open = new Wishlist { Name = "Open", OwnerId = jordan.Id };
            var closed = new Wishlist { Name = "Closed", OwnerId = jordan.Id, IsPrivate = true };
            var reservedOnly = new WishlistItem { Name = "Hat", Wishlist = open };
            var hatAgain = new WishlistItem { Name = "Hat", Wishlist = open };
            var given = new WishlistItem { Name = "Gloves", Wishlist = open };
            var closedGloves = new WishlistItem { Name = "Gloves", Wishlist = closed };
            seed.WishlistItems.AddRange(reservedOnly, hatAgain, given, closedGloves);
            await seed.SaveChangesAsync();
            var bought = Reserve(given, me.Id);
            bought.PurchasedOn = DateTimeOffset.UtcNow;
            seed.ItemReservations.AddRange(Reserve(reservedOnly, me.Id), bought);
            await seed.SaveChangesAsync();
            unboughtTarget = hatAgain.Id;
            closedTarget = closedGloves.Id;
            openId = open.PublicId;
            closedId = closed.PublicId;
        }

        var service = new GiftPlanService(factory);

        Assert.Empty(await service.GetPreviouslyGivenMatchesAsync("me", openId, unboughtTarget));
        Assert.Empty(await service.GetPreviouslyGivenMatchesAsync("me", closedId, closedTarget));
    }

    [Fact]
    public async Task GetPreviouslyGivenMatchesAsync_DoesNotLeakPrivateItemsOrRevokedLists()
    {
        var factory = CreateFactory();
        int target;
        string openId;
        await using (var seed = factory.CreateDbContext())
        {
            var (me, _, jordan, _) = AddUsers(seed);
            AddFriends(seed, "me", "jordan");
            var open = new Wishlist { Name = "Open", OwnerId = jordan.Id };
            var closed = new Wishlist { Name = "Closed", OwnerId = jordan.Id, IsPrivate = true };
            var nowPrivate = new WishlistItem { Name = "Secret scarf", Url = "https://shop.test/scarf", Wishlist = open, IsPrivate = true };
            var fromClosed = new WishlistItem { Name = "Secret scarf renamed", Url = "https://shop.test/scarf", Wishlist = closed };
            var again = new WishlistItem { Name = "Scarf", Url = "https://shop.test/scarf", Wishlist = open };
            seed.WishlistItems.AddRange(nowPrivate, fromClosed, again);
            await seed.SaveChangesAsync();
            var a = Reserve(nowPrivate, me.Id);
            var b = Reserve(fromClosed, me.Id);
            a.PurchasedOn = b.PurchasedOn = DateTimeOffset.UtcNow;
            seed.ItemReservations.AddRange(a, b);
            await seed.SaveChangesAsync();
            target = again.Id;
            openId = open.PublicId;
        }

        Assert.Empty(await new GiftPlanService(factory).GetPreviouslyGivenMatchesAsync("me", openId, target));
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