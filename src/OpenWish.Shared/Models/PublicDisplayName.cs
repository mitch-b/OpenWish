namespace OpenWish.Shared.Models;

/// <summary>Chooses a name that is safe to show to other people; sign-up stores emails as usernames.</summary>
public static class PublicDisplayName
{
    public static string From(string? userName, string fallback) =>
        string.IsNullOrWhiteSpace(userName) || userName.Contains('@')
            ? fallback
            : userName.Trim();

    public static string ForGiftRecipient(string? userName, string? groupName)
    {
        var name = From(groupName, string.Empty);
        if (name.Length > 0)
        {
            return name;
        }
        name = From(userName, string.Empty);
        if (name.Length > 0)
        {
            return name;
        }
        // Email-as-username accounts have no separate name; show their handle, not their email.
        var handle = userName?.Split('@')[0].Replace('.', ' ').Replace('_', ' ').Replace('-', ' ');
        return From(handle, "Invited guest");
    }
}