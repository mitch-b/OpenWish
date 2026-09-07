using System.Text.Json;
using System.Xml.Linq;
using Xunit;

namespace OpenWish.Shared.Tests;

public class ReleaseMetadataTests
{
    [Fact]
    public void CurrentReleaseArtifacts_UseTheSameVersion()
    {
        var repositoryDirectory = FindRepositoryDirectory();
        var releaseVersion = File.ReadAllText(Path.Combine(repositoryDirectory, "version.txt")).Trim();
        var buildVersion = XDocument.Load(Path.Combine(repositoryDirectory, "src", "Directory.Build.props"))
            .Descendants("Version")
            .Single()
            .Value
            .Trim();
        var releases = JsonSerializer.Deserialize<List<ReleaseEntry>>(
            File.ReadAllText(Path.Combine(repositoryDirectory, "src", "OpenWish.Web", "wwwroot", "releases.json")),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(releases);
        Assert.NotEmpty(releases);
        Assert.Equal(releaseVersion, buildVersion);
        Assert.Equal(releaseVersion, releases[0].Version);
    }

    private static string FindRepositoryDirectory()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "OpenWish.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.Parent?.FullName
            ?? throw new DirectoryNotFoundException("Could not locate the repository directory.");
    }

    private sealed class ReleaseEntry
    {
        public required string Version { get; init; }
    }
}