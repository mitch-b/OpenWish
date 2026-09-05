using System.Net.Http.Json;
using OpenWish.Shared.Models;
using OpenWish.Shared.Services;

namespace OpenWish.Web.Client.Services;

public sealed class ReleaseNotesHttpClientService(HttpClient httpClient) : IReleaseNotesService
{
    public async Task<IReadOnlyList<ReleaseEntryModel>> GetReleaseNotesAsync(
        CancellationToken cancellationToken = default)
    {
        return await httpClient.GetFromJsonAsync<List<ReleaseEntryModel>>(
            "releases.json",
            cancellationToken) ?? [];
    }
}