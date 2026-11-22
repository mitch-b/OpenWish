using Xunit;

namespace OpenWish.Shared.Tests.Ux;

public class WishlistDetailsMarkupTests
{
    [Fact]
    public void ListRows_RenderAsCompactIdeaCardsOnPhones()
    {
        var list = ReadWishlistComponent("WishlistItemList.razor");
        var styles = Normalize(ReadWishlistComponent("WishlistItemList.razor.css"));

        Assert.Contains("class=\"item-row", list, StringComparison.Ordinal);
        Assert.Contains("class=\"item-tags\"", list, StringComparison.Ordinal);
        Assert.Contains("<span class=\"item-description\">", list, StringComparison.Ordinal);

        var phone = styles[styles.IndexOf("/* Phones: each table row becomes a compact idea card. */", StringComparison.Ordinal)..];
        Assert.Contains("grid-template-areas:", phone, StringComparison.Ordinal);
        Assert.Contains("-webkit-line-clamp: 2;", phone, StringComparison.Ordinal);
        Assert.DoesNotContain("content: attr(data-label)", phone, StringComparison.Ordinal);
    }

    [Fact]
    public void ItemActions_AreQuietNamedIconButtonsGuardedUntilInteractive()
    {
        var list = ReadWishlistComponent("WishlistItemList.razor");
        var styles = Normalize(ReadWishlistComponent("WishlistItemList.razor.css"));

        Assert.Contains("class=\"btn btn-sm item-icon-action item-delete-action\"", list, StringComparison.Ordinal);
        Assert.Contains("aria-label=\"Delete @item.Name\"", list, StringComparison.Ordinal);
        Assert.Contains("<div class=\"rank-actions\" role=\"group\" aria-label=\"Reorder @item.Name\">", list, StringComparison.Ordinal);
        Assert.Contains("disabled=\"@(!CanMoveItem(item, -1) || !RendererInfo.IsInteractive)\"", list, StringComparison.Ordinal);
        Assert.DoesNotContain("btn-danger", list, StringComparison.Ordinal);
        Assert.Contains(".item-delete-action:hover:not(:disabled) {", styles, StringComparison.Ordinal);
        Assert.Contains("width: 2.75rem;", styles, StringComparison.Ordinal);
    }

    [Fact]
    public void Header_KeepsOneDominantAddActionAndNamedCompactShareTools()
    {
        var page = ReadPage("WishlistDetails.razor");
        var styles = Normalize(ReadPage("WishlistDetails.razor.css"));

        Assert.Contains("<div class=\"hero-tools\" role=\"group\" aria-label=\"Share and export\">", page, StringComparison.Ordinal);
        Assert.Contains("<span class=\"btn-short\" aria-hidden=\"true\">CSV</span>", page, StringComparison.Ordinal);
        Assert.Contains("<span class=\"btn-text\">Download CSV</span>", page, StringComparison.Ordinal);

        // Short phone labels must not remove the full accessible name.
        Assert.Contains(".hero-actions .hero-tool.btn .btn-text,\n    .btn-filter .btn-text {\n        position: absolute;", styles, StringComparison.Ordinal);
    }

    [Fact]
    public void StatsAndOrganizer_StayCompactSoIdeasStartNearTheTop()
    {
        var page = Normalize(ReadPage("WishlistDetails.razor"));
        var styles = Normalize(ReadPage("WishlistDetails.razor.css"));
        var organizer = ReadWishlistComponent("WishlistOrganizePanel.razor");
        var organizerStyles = Normalize(ReadWishlistComponent("WishlistOrganizePanel.razor.css"));

        Assert.Contains(".stats-dashboard .stat-card {\n    display: inline-flex;", styles, StringComparison.Ordinal);
        Assert.Contains("class=\"ranking-help @(CanReorder ? \"visually-hidden\" : string.Empty)\"", page, StringComparison.Ordinal);
        Assert.Contains("&& _items.Count > 0)\n    {\n        <WishlistOrganizePanel", page, StringComparison.Ordinal);
        Assert.Contains("disabled=\"@(Items.Count == 0 || !RendererInfo.IsInteractive)\"", organizer, StringComparison.Ordinal);
        Assert.Contains(".organize:not(.is-open) .organize-intro {\n        display: none;", organizerStyles, StringComparison.Ordinal);
    }

    [Fact]
    public void DesktopTable_UsesSentenceCaseHeadersAndSingleRowActions()
    {
        var list = ReadWishlistComponent("WishlistItemList.razor");
        var styles = Normalize(ReadWishlistComponent("WishlistItemList.razor.css"));

        Assert.Contains("<th scope=\"col\" class=\"col-actions\">Actions</th>", list, StringComparison.Ordinal);
        Assert.DoesNotContain("text-transform: uppercase", styles, StringComparison.Ordinal);
        Assert.Contains("text-transform: none;", styles, StringComparison.Ordinal);
        Assert.Contains(".table td.item-cell-actions {\n        width: 1%;\n        white-space: nowrap;", styles, StringComparison.Ordinal);
    }

    private static string Normalize(string text) => text.Replace("\r\n", "\n", StringComparison.Ordinal);

    private static string ReadPage(string file) =>
        ReadComponent("OpenWish.Web.Client", "Components", "Pages", "Wishlists", file);

    private static string ReadWishlistComponent(string file) =>
        ReadComponent("OpenWish.Web.Client", "Components", "Wishlist", file);

    private static string ReadComponent(params string[] pathParts)
    {
        var solutionDirectory = FindSolutionDirectory();
        return File.ReadAllText(Path.Combine([solutionDirectory, .. pathParts]));
    }

    private static string FindSolutionDirectory()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "OpenWish.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName
            ?? throw new DirectoryNotFoundException("Could not locate OpenWish.slnx.");
    }
}