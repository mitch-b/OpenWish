using System.Text;
using Microsoft.EntityFrameworkCore;
using OpenWish.Application.Services;
using OpenWish.Data;
using OpenWish.Data.Entities;
using OpenWish.Shared.Models;
using Xunit;

namespace OpenWish.Application.Tests.Services;

public class WishlistPortabilityServiceTests
{
    [Fact]
    public async Task Export_IncludesOnlyTheOwnersLiveListsAndVisibleIdeas()
    {
        var factory = new TestFactory();
        await using (var seed = factory.CreateDbContext())
        {
            seed.Wishlists.AddRange(
                new Wishlist
                {
                    Name = "Kitchen",
                    OwnerId = "owner",
                    Items =
                    [
                        new WishlistItem { Name = "Skillet", Price = 38m, Url = "https://example.com/s", Priority = 1, IsPinned = true, OrderIndex = 1 },
                        new WishlistItem { Name = "Apron", OrderIndex = 0, ArchivedOn = DateTimeOffset.UtcNow },
                        new WishlistItem { Name = "Surprise", IsHiddenFromOwner = true },
                        new WishlistItem { Name = "Removed", Deleted = true },
                        new WishlistItem
                        {
                            Name = "Reserved", Description = "note", ReceivedOn = DateTimeOffset.UtcNow
                        }
                    ]
                },
                new Wishlist { Name = "Old", OwnerId = "owner", Deleted = true },
                new Wishlist { Name = "Someone else's", OwnerId = "other", Items = [new WishlistItem { Name = "Secret" }] });
            await seed.SaveChangesAsync();
        }

        var export = await new WishlistPortabilityService(factory).ExportAsync("owner");

        var list = Assert.Single(export.Lists);
        Assert.Equal("Kitchen", list.Name);
        Assert.Equal(["Apron", "Reserved", "Skillet"], list.Items.Select(i => i.Name).Order(StringComparer.Ordinal));
        Assert.DoesNotContain(list.Items, i => i.Name is "Surprise" or "Removed");
        Assert.Equal(WishlistExportStatus.Archived, list.Items.Single(i => i.Name == "Apron").Status);
        Assert.Equal(WishlistExportStatus.Received, list.Items.Single(i => i.Name == "Reserved").Status);
        var skillet = list.Items.Single(i => i.Name == "Skillet");
        Assert.Equal((38m, 1, true), (skillet.Price, skillet.Priority, skillet.IsPinned));
    }

    [Fact]
    public async Task Import_CreatesPrivateListsOwnedByTheCaller()
    {
        var factory = new TestFactory();
        var service = new WishlistPortabilityService(factory);
        var file = new WishlistExportFile
        {
            Lists =
            [
                new WishlistExportList
                {
                    Name = "  Birthday ",
                    Items =
                    [
                        new WishlistExportItem { Name = "Book", Price = 12.5m, Priority = 2, Url = "https://example.com/b", IsPinned = true },
                        new WishlistExportItem { Name = "Old shirt", Status = WishlistExportStatus.Archived },
                        new WishlistExportItem { Name = "Got it", Status = WishlistExportStatus.Received },
                        new WishlistExportItem { Name = "   " }
                    ]
                }
            ]
        };

        var result = await service.ImportAsync("me", file);

        Assert.Equal(new WishlistImportResult(1, 3, 1), result);
        await using var context = factory.CreateDbContext();
        var list = await context.Wishlists.Include(w => w.Items).SingleAsync();
        Assert.Equal("Birthday", list.Name);
        Assert.Equal("me", list.OwnerId);
        Assert.True(list.IsPrivate);
        Assert.Equal(["Book", "Old shirt", "Got it"], list.Items!.OrderBy(i => i.OrderIndex).Select(i => i.Name));
        Assert.NotNull(list.Items!.Single(i => i.Name == "Old shirt").ArchivedOn);
        Assert.NotNull(list.Items!.Single(i => i.Name == "Got it").ReceivedOn);
        Assert.Equal((12.5m, 2, true), (list.Items!.First(i => i.Name == "Book").Price, list.Items!.First(i => i.Name == "Book").Priority, list.Items!.First(i => i.Name == "Book").IsPinned));
    }

    [Fact]
    public async Task Import_NeverReplacesAnExistingListWithTheSameName()
    {
        var factory = new TestFactory();
        await using (var seed = factory.CreateDbContext())
        {
            seed.Wishlists.Add(new Wishlist { Name = "Kitchen", OwnerId = "me", Items = [new WishlistItem { Name = "Mine" }] });
            await seed.SaveChangesAsync();
        }

        var file = new WishlistExportFile { Lists = [new WishlistExportList { Name = "kitchen", Items = [new WishlistExportItem { Name = "New" }] }] };
        await new WishlistPortabilityService(factory).ImportAsync("me", file);

        await using var context = factory.CreateDbContext();
        Assert.Equal(["Kitchen", "kitchen (imported)"], context.Wishlists.OrderBy(w => w.Id).Select(w => w.Name));
        Assert.Single(context.WishlistItems.Where(i => i.Name == "Mine"));
    }

    [Fact]
    public async Task Import_DropsUnsafeLinksAndInvalidNumbers()
    {
        var factory = new TestFactory();
        var file = new WishlistExportFile
        {
            Lists =
            [
                new WishlistExportList
                {
                    Name = new string('n', 500),
                    Items =
                    [
                        new WishlistExportItem
                        {
                            Name = "Thing",
                            Url = "javascript:alert(1)",
                            Image = "data:image/png;base64,AAAA",
                            Price = -4m,
                            Priority = 9
                        },
                        new WishlistExportItem { Name = "Huge", Price = 12345678901m }
                    ]
                }
            ]
        };

        await new WishlistPortabilityService(factory).ImportAsync("me", file);

        await using var context = factory.CreateDbContext();
        var items = await context.WishlistItems.OrderBy(i => i.OrderIndex).ToListAsync();
        Assert.All(items, item => Assert.Equal((null, null, null, null), (item.Url, item.Image, item.Price, item.Priority)));
    }

    [Fact]
    public async Task Read_RoundTripsAnExport()
    {
        var factory = new TestFactory();
        var service = new WishlistPortabilityService(factory);
        var json = """{"format":"openwish-wishlists","version":1,"lists":[{"name":"A","items":[{"name":"B","price":3}]}]}""";

        var file = await service.ReadAsync(new MemoryStream(Encoding.UTF8.GetBytes(json)));

        Assert.Equal("B", file.Lists.Single().Items.Single().Name);
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("{\"format\":\"something-else\",\"version\":1}")]
    [InlineData("{\"format\":\"openwish-wishlists\",\"version\":2}")]
    [InlineData("null")]
    public async Task Read_RejectsFilesThatAreNotUsableExports(string json) =>
        await Assert.ThrowsAsync<InvalidDataException>(() =>
            new WishlistPortabilityService(new TestFactory()).ReadAsync(new MemoryStream(Encoding.UTF8.GetBytes(json))));

    [Fact]
    public async Task Import_KeepsLongValuesIntact()
    {
        var factory = new TestFactory();
        var description = new string('d', 3000);
        var url = "https://example.com/p?" + new string('q', 3000);
        var file = new WishlistExportFile
        {
            Lists = [new WishlistExportList { Name = new string('n', 500), Items = [new WishlistExportItem { Name = "Thing", Description = description, Url = url }] }]
        };

        await new WishlistPortabilityService(factory).ImportAsync("me", file);

        await using var context = factory.CreateDbContext();
        var item = await context.WishlistItems.SingleAsync();
        Assert.Equal((description, url), (item.Description, item.Url));
        Assert.Equal(500, (await context.Wishlists.SingleAsync()).Name.Length);
    }

    [Fact]
    public async Task Read_AcceptsAnExportWithManyListsAndIdeas()
    {
        var items = string.Join(',', Enumerable.Repeat("""{"name":"x"}""", 1500));
        var lists = string.Join(',', Enumerable.Repeat($$"""{"name":"A","items":[{{items}}]}""", 150));
        var json = $$"""{"format":"openwish-wishlists","version":1,"lists":[{{lists}}]}""";

        var file = await new WishlistPortabilityService(new TestFactory()).ReadAsync(new MemoryStream(Encoding.UTF8.GetBytes(json)));

        Assert.Equal(150, file.Lists.Count);
    }

    private sealed class TestFactory : IDbContextFactory<ApplicationDbContext>
    {
        private readonly string _name = Guid.NewGuid().ToString();

        public ApplicationDbContext CreateDbContext() =>
            new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(_name).Options);
    }
}