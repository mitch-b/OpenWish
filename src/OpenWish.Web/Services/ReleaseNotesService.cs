using System.Text.Json;
using OpenWish.Shared.Models;
using OpenWish.Shared.Services;

namespace OpenWish.Web.Services;

public sealed class ReleaseNotesService(IWebHostEnvironment environment) : IReleaseNotesService
{
    private static readonly JsonSerializerOptions _serializerOptions = new(JsonSerializerDefaults.Web);
    private readonly IWebHostEnvironment _environment = environment;

    public async Task<IReadOnlyList<ReleaseEntryModel>> GetReleaseNotesAsync(
        CancellationToken cancellationToken = default)
    {
        var releaseNotesFile = _environment.WebRootFileProvider.GetFileInfo("releases.json");
        if (!releaseNotesFile.Exists)
        {
            throw new FileNotFoundException("The release notes file was not found.", releaseNotesFile.PhysicalPath);
        }

        await using var stream = releaseNotesFile.CreateReadStream();
        return await JsonSerializer.DeserializeAsync<List<ReleaseEntryModel>>(
            stream,
            _serializerOptions,
            cancellationToken) ?? [];
    }
}