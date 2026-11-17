using System.ComponentModel.DataAnnotations;

namespace OpenWish.Shared.Models;

public record CircleMemberModel
{
    public string UserId { get; init; } = string.Empty;
    public string UserName { get; init; } = string.Empty;
}

public record CircleWishlistModel
{
    public string PublicId { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
}

/// <summary>A named group of the caller's friends. Circles are private to the person who made them.</summary>
public record CircleModel
{
    public string PublicId { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public IReadOnlyList<CircleMemberModel> Members { get; init; } = [];

    /// <summary>Wishlists shared with everyone in the circle, including people who join later.</summary>
    public IReadOnlyList<CircleWishlistModel> Wishlists { get; init; } = [];
}

public sealed class CircleNameRequest
{
    [Required]
    [StringLength(CircleRules.MaxNameLength)]
    public string Name { get; init; } = string.Empty;
}

public sealed class CircleMemberRequest
{
    [Required]
    public string UserId { get; init; } = string.Empty;
}

public sealed class CircleWishlistRequest
{
    [Required]
    public string WishlistPublicId { get; init; } = string.Empty;
}

public static class CircleRules
{
    public const int MaxNameLength = 40;
    public const int MaxCirclesPerPerson = 20;

    /// <summary>Collapses whitespace and rejects empty or over-long names.</summary>
    public static string NormalizeName(string? name)
    {
        var normalized = string.Join(' ', (name ?? string.Empty).Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        if (normalized.Length == 0)
        {
            throw new ArgumentException("Give the circle a name.");
        }

        if (normalized.Length > MaxNameLength)
        {
            throw new ArgumentException($"Circle names can be up to {MaxNameLength} characters.");
        }

        return normalized;
    }

    /// <summary>The user names of everyone in the chosen circle, or null when no circle is chosen or it no longer exists.</summary>
    public static IReadOnlySet<string>? GetMemberNames(IEnumerable<CircleModel> circles, string? circlePublicId)
    {
        if (string.IsNullOrWhiteSpace(circlePublicId))
        {
            return null;
        }

        var circle = circles.FirstOrDefault(candidate => candidate.PublicId == circlePublicId);
        return circle?.Members.Select(member => member.UserName).ToHashSet(StringComparer.OrdinalIgnoreCase);
    }
}