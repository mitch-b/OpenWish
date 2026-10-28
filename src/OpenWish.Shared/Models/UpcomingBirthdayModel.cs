namespace OpenWish.Shared.Models;

/// <summary>A friend's birthday coming up soon, with a way back to their wishlist.</summary>
public record UpcomingBirthdayModel
{
    public string UserName { get; init; } = string.Empty;
    public int Month { get; init; }
    public int Day { get; init; }
    public int DaysUntil { get; init; }

    /// <summary>A wishlist of theirs the caller can open, when there is one.</summary>
    public string? WishlistPublicId { get; init; }
}

/// <summary>The caller's own optional birthday.</summary>
public record BirthdayModel
{
    public int? Month { get; init; }
    public int? Day { get; init; }

    public bool IsSet => Month is not null && Day is not null;
}