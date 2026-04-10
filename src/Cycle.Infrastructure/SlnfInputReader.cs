using System.Text.Json;
using Cycle.Core;

namespace Cycle.Infrastructure;

public static class SlnfInputReader
{
    public static async Task<(SolutionPath ParentSolution, IReadOnlySet<FilePath> ProjectScope)>
        ReadAsync(FilePath slnfPath, CancellationToken ct)
    {
        await using var stream = File.OpenRead(slnfPath.FullPath);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

        var solution = doc.RootElement.GetProperty("solution");
        var relativeSolutionPath = solution.GetProperty("path").GetString()
            ?? throw new InvalidOperationException("The 'solution.path' property is missing or null.");

        var slnfDir = slnfPath.DirectoryName;
        var parentSolutionFullPath = Path.GetFullPath(Path.Combine(slnfDir, relativeSolutionPath));
        var parentSolution = SolutionPath.FromString(parentSolutionFullPath);

        var solutionDir = parentSolution.FilePath.DirectoryName;

        var scope = new HashSet<FilePath>();

        foreach (var element in solution.GetProperty("projects").EnumerateArray())
        {
            var projectRelativePath = element.GetString();
            if (string.IsNullOrWhiteSpace(projectRelativePath))
            {
                continue;
            }

            var fullPath = Path.GetFullPath(Path.Combine(solutionDir, projectRelativePath));
            scope.Add(FilePath.FromString(fullPath));
        }

        return (parentSolution, scope);
    }
}
