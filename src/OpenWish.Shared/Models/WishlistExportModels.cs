namespace OpenWish.Shared.Models;

/// <summary>A portable copy of a person's own wishlists. It never holds reservations, comments, friends, or other people's data.</summary>
public class WishlistExportFile
{
    public const string FormatName = "openwish-wishlists";

    public string Format { get; set; } = string.Empty;
    public int Version { get; set; }
    public DateTimeOffset ExportedOn { get; set; }
    public List<WishlistExportList> Lists { get; set; } = [];
}

public class WishlistExportList
{
    public string Name { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public List<WishlistExportItem> Items { get; set; } = [];
}

public class WishlistExportItem
{
    public string Name { get; set; } = string.Empty;
    public string? Url { get; set; }
    public string? Image { get; set; }
    public string? Description { get; set; }
    public decimal? Price { get; set; }
    public string? WhereToBuy { get; set; }
    public int? Priority { get; set; }
    public bool IsPrivate { get; set; }
    public bool IsPinned { get; set; }

    /// <summary>"active", "received", or "archived".</summary>
    public string Status { get; set; } = WishlistExportStatus.Active;
}

public static class WishlistExportStatus
{
    public const string Active = "active";
    public const string Received = "received";
    public const string Archived = "archived";
}

public record WishlistImportResult(int ListsCreated, int ItemsCreated, int ItemsSkipped);