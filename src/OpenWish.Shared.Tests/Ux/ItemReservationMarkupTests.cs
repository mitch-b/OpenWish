using Xunit;

namespace OpenWish.Shared.Tests.Ux;

public class ItemReservationMarkupTests
{
    [Fact]
    public void Reservation_ExplainsPrivacyAndNeverOffersControlsToTheOwnerOrSignedOutViewers()
    {
        var markup = ReadWishlistComponent("ItemReservation.razor");

        Assert.Contains("The list owner never sees reservations.", markup, StringComparison.Ordinal);
        Assert.Contains("aria-describedby=\"anonymousHelp-@ItemId\"", markup, StringComparison.Ordinal);
        Assert.Contains("Hide your name from other shoppers.", markup, StringComparison.Ordinal);

        var ownerBranch = markup.IndexOf("else if (_currentUserId == OwnerId)", StringComparison.Ordinal);
        var reserveButton = markup.IndexOf("Reserve this item", StringComparison.Ordinal);
        Assert.True(markup.IndexOf("else if (_currentUserId is null)", StringComparison.Ordinal) is > 0 and var signedOut && signedOut < ownerBranch);
        Assert.True(ownerBranch > 0 && ownerBranch < reserveButton);
    }

    [Fact]
    public void Reservation_ShowsACompactStatusThatStacksOnPhones()
    {
        var markup = ReadWishlistComponent("ItemReservation.razor");
        var styles = ReadWishlistComponent("ItemReservation.razor.css");

        Assert.Contains("class=\"reservation-status reservation-status-mine\"", markup, StringComparison.Ordinal);
        Assert.Contains("You reserved this gift", markup, StringComparison.Ordinal);
        Assert.Contains("Pick another idea to avoid a duplicate gift.", markup, StringComparison.Ordinal);
        Assert.Contains("class=\"reservation-feedback\" role=\"status\"", markup, StringComparison.Ordinal);

        var phone = styles[styles.IndexOf("@media (max-width: 767.98px)", StringComparison.Ordinal)..];
        Assert.Contains(".reservation-cancel {\n        grid-column: 1 / -1;\n        width: 100%;", phone, StringComparison.Ordinal);
        Assert.Contains(".reservation-reserve {\n        justify-self: stretch;", phone, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("ItemReservation.razor")]
    [InlineData("ItemComments.razor")]
    public void GiftCoordinationControls_StayDisabledUntilInteractive(string component)
    {
        var markup = ReadWishlistComponent(component);

        var clickHandlers = markup.Split("@onclick=").Length - 1;
        Assert.True(clickHandlers > 0);
        foreach (var button in markup.Split("<button").Skip(1))
        {
            var tag = button[..button.IndexOf('>')];
            Assert.Contains("!RendererInfo.IsInteractive", tag, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Comments_UseAVisibleLabelAndWarnThatTheOwnerCanReadThem()
    {
        var markup = ReadWishlistComponent("ItemComments.razor");

        Assert.DoesNotContain("form-floating", markup, StringComparison.Ordinal);
        Assert.Contains("<label class=\"form-label comment-label\" for=\"commentText-@ItemId\">Add a comment</label>", markup, StringComparison.Ordinal);
        Assert.Contains("including the list owner.", markup, StringComparison.Ordinal);
        Assert.Contains("<ul class=\"comment-list\" aria-labelledby=\"comments-title-@ItemId\">", markup, StringComparison.Ordinal);
        Assert.Contains("<time class=\"comment-time\"", markup, StringComparison.Ordinal);
        Assert.Contains("No comments yet. Ask a question", markup, StringComparison.Ordinal);
    }

    private static string ReadWishlistComponent(string fileName)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "OpenWish.slnx")))
        {
            directory = directory.Parent;
        }

        var root = directory?.FullName
            ?? throw new DirectoryNotFoundException("Could not locate OpenWish.slnx.");
        return File.ReadAllText(Path.Combine(root, "OpenWish.Web.Client", "Components", "Wishlist", fileName))
            .Replace("\r\n", "\n", StringComparison.Ordinal);
    }
}