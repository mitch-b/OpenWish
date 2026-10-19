namespace OpenWish.Shared.Models;

/// <summary>Chooses a name that is safe to show to other people; sign-up stores emails as usernames.</summary>
public static class PublicDisplayName
{
    public static string From(string? userName, string fallback) =>
        string.IsNullOrWhiteSpace(userName) || userName.Contains('@')
            ? fallback
            : userName.Trim();
}