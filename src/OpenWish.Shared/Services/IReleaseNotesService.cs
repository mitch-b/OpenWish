using OpenWish.Shared.Models;

namespace OpenWish.Shared.Services;

public interface IReleaseNotesService
{
    Task<IReadOnlyList<ReleaseEntryModel>> GetReleaseNotesAsync(
        CancellationToken cancellationToken = default);
}