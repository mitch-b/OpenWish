using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OpenWish.Data;
using OpenWish.Data.Entities;
using OpenWish.Shared.Models;

namespace OpenWish.Application.Services;

/// <summary>Copies a person's own wishlists out of OpenWish as JSON and back in as new, private lists.</summary>
public interface IWishlistPortabilityService
{
    Task<WishlistExportFile> ExportAsync(string userId);

    /// <summary>Reads an export file; throws <see cref="InvalidDataException"/> with a message safe to show when it is not usable.</summary>
    Task<WishlistExportFile> ReadAsync(Stream stream);

    Task<WishlistImportResult> ImportAsync(string userId, WishlistExportFile file);
}

public class WishlistPortabilityService(IDbContextFactory<ApplicationDbContext> contextFactory) : IWishlistPortabilityService
{
    public const int MaxFileBytes = 2 * 1024 * 1024;
    public const int MaxLists = 100;
    public const int MaxItemsPerList = 1000;
    private const int MaxNameLength = 200;
    private const int MaxTextLength = 2000;
    private const int MaxUrlLength = 2048;
    private const string ImportedSuffix = " (imported)";

    private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true, MaxDepth = 8 };

    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory = contextFactory;

    public async Task<WishlistExportFile> ExportAsync(string userId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        await using var context = await _contextFactory.CreateDbContextAsync();
        var lists = await context.Wishlists
            .AsNoTracking()
            .Where(w => w.OwnerId == userId && !w.Deleted)
            .OrderBy(w => w.Name)
            .Select(w => new
            {
                w.Name,
                w.Icon,
                // Ideas collaborators hid from the owner are surprises and stay out of the owner's copy.
                Items = w.Items!
                    .Where(i => !i.Deleted && !i.IsHiddenFromOwner)
                    .OrderBy(i => i.OrderIndex)
                    .ThenBy(i => i.Id)
                    .Select(i => new
                    {
                        i.Name,
                        i.Url,
                        i.Image,
                        i.Description,
                        i.Price,
                        i.WhereToBuy,
                        i.Priority,
                        i.IsPrivate,
                        i.IsPinned,
                        i.ReceivedOn,
                        i.ArchivedOn
                    })
                    .ToList()
            })
            .ToListAsync();

        return new WishlistExportFile
        {
            Format = WishlistExportFile.FormatName,
            Version = 1,
            ExportedOn = DateTimeOffset.UtcNow,
            Lists = lists.Select(list => new WishlistExportList
            {
                Name = list.Name,
                Icon = list.Icon,
                Items = list.Items.Select(i => new WishlistExportItem
                {
                    Name = i.Name,
                    Url = i.Url,
                    Image = i.Image,
                    Description = i.Description,
                    Price = i.Price,
                    WhereToBuy = i.WhereToBuy,
                    Priority = i.Priority,
                    IsPrivate = i.IsPrivate,
                    IsPinned = i.IsPinned,
                    Status = i.ReceivedOn is not null ? WishlistExportStatus.Received
                        : i.ArchivedOn is not null ? WishlistExportStatus.Archived
                        : WishlistExportStatus.Active
                }).ToList()
            }).ToList()
        };
    }

    public async Task<WishlistExportFile> ReadAsync(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        WishlistExportFile? file;
        try
        {
            file = await JsonSerializer.DeserializeAsync<WishlistExportFile>(stream, _jsonOptions);
        }
        catch (JsonException)
        {
            throw new InvalidDataException("That file is not an OpenWish wishlist export.");
        }

        if (file is null || file.Format != WishlistExportFile.FormatName)
        {
            throw new InvalidDataException("That file is not an OpenWish wishlist export.");
        }

        if (file.Version != 1)
        {
            throw new InvalidDataException("That export was made by a newer version of OpenWish and cannot be imported here.");
        }

        if (file.Lists is null || file.Lists.Count > MaxLists || file.Lists.Any(list => list?.Items is { Count: > MaxItemsPerList }))
        {
            throw new InvalidDataException($"An import can hold up to {MaxLists} lists with {MaxItemsPerList} ideas each.");
        }

        return file;
    }

    public async Task<WishlistImportResult> ImportAsync(string userId, WishlistExportFile file)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ArgumentNullException.ThrowIfNull(file);

        await using var context = await _contextFactory.CreateDbContextAsync();
        var existingNames = (await context.Wishlists
                .Where(w => w.OwnerId == userId && !w.Deleted)
                .Select(w => w.Name)
                .ToListAsync())
            .ToHashSet(StringComparer.CurrentCultureIgnoreCase);

        var listsCreated = 0;
        var itemsCreated = 0;
        var itemsSkipped = 0;
        var now = DateTimeOffset.UtcNow;
        foreach (var source in file.Lists.Where(list => list is not null))
        {
            var name = Clean(source.Name, MaxNameLength);
            var sourceItems = (source.Items ?? []).Where(item => item is not null).ToList();
            if (name is null)
            {
                itemsSkipped += sourceItems.Count;
                continue;
            }

            var listName = existingNames.Contains(name)
                ? Clean(name, MaxNameLength - ImportedSuffix.Length)! + ImportedSuffix
                : name;
            existingNames.Add(listName);

            // Imported lists start private: an export carries no sharing, and sharing is the owner's choice to make again.
            var wishlist = new Wishlist
            {
                Name = listName,
                Icon = Clean(source.Icon, 16),
                OwnerId = userId,
                IsPrivate = true,
                Items = []
            };

            var order = 0;
            foreach (var item in sourceItems)
            {
                var entity = ToEntity(item, order, now);
                if (entity is null)
                {
                    itemsSkipped++;
                    continue;
                }

                wishlist.Items.Add(entity);
                order++;
            }

            context.Wishlists.Add(wishlist);
            listsCreated++;
            itemsCreated += wishlist.Items.Count;
        }

        await context.SaveChangesAsync();
        return new WishlistImportResult(listsCreated, itemsCreated, itemsSkipped);
    }

    private static WishlistItem? ToEntity(WishlistExportItem item, int order, DateTimeOffset now)
    {
        var name = Clean(item.Name, MaxNameLength);
        if (name is null)
        {
            return null;
        }

        var price = item.Price is { } value && value >= 0 && value <= 999_999_999.99m ? decimal.Round(value, 2) : (decimal?)null;
        return new WishlistItem
        {
            Name = name,
            Url = SafeUrl(item.Url),
            Image = SafeUrl(item.Image),
            Description = Clean(item.Description, MaxTextLength),
            Price = price,
            WhereToBuy = Clean(item.WhereToBuy, MaxNameLength),
            Priority = item.Priority is >= 1 and <= 3 ? item.Priority : null,
            IsPrivate = item.IsPrivate,
            IsPinned = item.IsPinned,
            OrderIndex = order,
            ReceivedOn = item.Status == WishlistExportStatus.Received ? now : null,
            ArchivedOn = item.Status == WishlistExportStatus.Archived ? now : null
        };
    }

    private static string? Clean(string? value, int maxLength)
    {
        var trimmed = value?.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            return null;
        }

        return trimmed.Length > maxLength ? trimmed[..maxLength] : trimmed;
    }

    // Links come from an uploaded file, so only plain web addresses are kept.
    private static string? SafeUrl(string? value)
    {
        var trimmed = Clean(value, MaxUrlLength);
        return trimmed is not null &&
               Uri.TryCreate(trimmed, UriKind.Absolute, out var uri) &&
               (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
            ? trimmed
            : null;
    }
}