using OpenWish.Shared.Models;

namespace OpenWish.Shared.Services;

/// <summary>Optional month-and-day birthdays shared with friends only.</summary>
public interface IBirthdayService
{
    Task<BirthdayModel> GetBirthdayAsync(string userId);

    /// <summary>Sets or clears the caller's birthday. Throws <see cref="ArgumentException"/> for an impossible date.</summary>
    Task SetBirthdayAsync(string userId, int? month, int? day);

    /// <summary>Gets friends' birthdays within the next <paramref name="withinDays"/> days, soonest first. Never includes non-friends.</summary>
    Task<IReadOnlyList<UpcomingBirthdayModel>> GetUpcomingBirthdaysAsync(string userId, int withinDays = 30);
}