using System.Text.RegularExpressions;
using Xunit;

namespace OpenWish.Shared.Tests.Accessibility;

public partial class LoadingStatusMarkupTests
{
    [Fact]
    public void RazorLoadingIndicators_HaveConsistentAccessibleSemantics()
    {
        var solutionDirectory = FindSolutionDirectory();
        var componentDirectories = new[]
        {
            Path.Combine(solutionDirectory, "OpenWish.Web"),
            Path.Combine(solutionDirectory, "OpenWish.Web.Client")
        };
        var violations = new List<string>();

        foreach (var file in componentDirectories.SelectMany(directory =>
                     Directory.EnumerateFiles(directory, "*.razor", SearchOption.AllDirectories)))
        {
            var markup = File.ReadAllText(file);

            if (markup.Contains("<span class=\"visually-hidden\">Loading...</span>", StringComparison.Ordinal))
            {
                violations.Add($"{Path.GetRelativePath(solutionDirectory, file)} uses a generic loading status.");
            }

            foreach (Match match in SpinnerSpanRegex().Matches(markup))
            {
                if (!match.Value.Contains("aria-hidden=\"true\"", StringComparison.Ordinal) ||
                    match.Value.Contains("role=\"status\"", StringComparison.Ordinal))
                {
                    violations.Add(
                        $"{Path.GetRelativePath(solutionDirectory, file)} has an announced action spinner: {match.Value}");
                }
            }
        }

        Assert.True(violations.Count == 0, string.Join(Environment.NewLine, violations));
    }

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

    [GeneratedRegex("<span\\s+class=\"[^\"]*spinner-border[^\"]*\"[^>]*>")]
    private static partial Regex SpinnerSpanRegex();
}