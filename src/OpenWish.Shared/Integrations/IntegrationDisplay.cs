namespace OpenWish.Shared.Integrations;

public static class IntegrationDisplay
{
    /// <summary>One or two initials that stand in for a connected app's logo.</summary>
    public static string Monogram(string? name)
    {
        var letters = (name ?? string.Empty)
            .Split([' ', '-', '_', '.'], StringSplitOptions.RemoveEmptyEntries)
            .Where(part => char.IsLetterOrDigit(part[0]))
            .Select(part => char.ToUpperInvariant(part[0]))
            .Take(2)
            .ToArray();
        return letters.Length == 0 ? "?" : new string(letters);
    }
}