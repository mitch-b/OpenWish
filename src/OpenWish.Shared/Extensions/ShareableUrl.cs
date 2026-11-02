namespace OpenWish.Shared.Extensions;

public static class ShareableUrl
{
    /// <summary>Returns the address without view-only query strings (such as a budget) or fragments.</summary>
    public static string FromCurrentUri(string currentUri)
    {
        ArgumentNullException.ThrowIfNull(currentUri);

        var end = currentUri.IndexOfAny(['?', '#']);
        return end < 0 ? currentUri : currentUri[..end];
    }
}