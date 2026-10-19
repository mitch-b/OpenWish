using OpenWish.Shared.Dashboard;

namespace OpenWish.Shared.Services;

/// <summary>Per-user dashboard state that is not tied to a single wishlist or event.</summary>
public interface IDashboardService
{
    /// <summary>Gets the caller's first-run checklist progress and whether they dismissed it.</summary>
    Task<FirstRunChecklist> GetFirstRunChecklistAsync(string userId);

    /// <summary>Hides the first-run checklist for the user permanently.</summary>
    Task DismissFirstRunChecklistAsync(string userId);
}