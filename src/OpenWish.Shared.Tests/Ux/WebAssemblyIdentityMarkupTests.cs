using System.Text.RegularExpressions;
using Xunit;

namespace OpenWish.Shared.Tests.Ux;

public partial class WebAssemblyIdentityMarkupTests
{
    // BaseEntityModel.Id is [JsonIgnore], so it is always 0 once a component runs in WebAssembly.
    // These receivers hold string user ids (ApplicationUserModel.Id) or local section records.
    private static readonly HashSet<string> _stringIdReceivers = new(StringComparer.Ordinal)
    {
        "User",
        "CreatedBy",
        "friend",
        "f",
        "invited",
        "userInfo"
    };

    private static readonly HashSet<(string File, string Receiver)> _fileSpecificStringIdReceivers =
    [
        ("FriendList.razor", "candidate"),
        ("PageSections.razor", "item")
    ];

    [Fact]
    public void ClientComponents_IdentifyEntitiesByPublicIdBecauseInternalIdsAreNotSerialized()
    {
        var clientDirectory = Path.Combine(FindSolutionDirectory(), "OpenWish.Web.Client");
        var violations = new List<string>();

        foreach (var file in Directory.EnumerateFiles(clientDirectory, "*.*", SearchOption.AllDirectories)
                     .Where(path => path.EndsWith(".razor", StringComparison.Ordinal) || path.EndsWith(".cs", StringComparison.Ordinal))
                     .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal) &&
                                    !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)))
        {
            var fileName = Path.GetFileName(file);
            var lines = File.ReadAllLines(file);
            for (var index = 0; index < lines.Length; index++)
            {
                var line = lines[index];
                if (line.Contains("Sections", StringComparison.Ordinal) || IdReset().IsMatch(line))
                {
                    continue;
                }

                foreach (Match match in IdAccess().Matches(line))
                {
                    var receiver = match.Groups["receiver"].Value;
                    if (!_stringIdReceivers.Contains(receiver) &&
                        !_fileSpecificStringIdReceivers.Contains((fileName, receiver)))
                    {
                        violations.Add($"{Path.GetRelativePath(clientDirectory, file)}:{index + 1}: {line.Trim()}");
                    }
                }
            }
        }

        Assert.True(
            violations.Count == 0,
            "Client code must key, compare, and send entities by PublicId because Id is not serialized:\n" +
            string.Join('\n', violations));
    }

    [GeneratedRegex(@"(?<receiver>[A-Za-z_][A-Za-z0-9_]*|\]|\))\??\.Id\b")]
    private static partial Regex IdAccess();

    [GeneratedRegex(@"\.Id = 0;")]
    private static partial Regex IdReset();

    private static string FindSolutionDirectory()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "OpenWish.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName
            ?? throw new DirectoryNotFoundException("Could not locate OpenWish.slnx.");
    }
}