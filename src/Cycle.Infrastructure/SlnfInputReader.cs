using System.Text.Json;
using Cycle.Application;
using Cycle.Core;
using Microsoft.Extensions.Logging;

namespace Cycle.Infrastructure;

public sealed partial class SlnfInputReader(
    ILogger<SlnfInputReader> logger)
    : ISlnfInputReader
{
    public async Task<SlnfInput> ReadAsync(FilePath slnfPath, CancellationToken ct)
    {
        await using var stream = File.OpenRead(slnfPath.FullPath);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

        if (!doc.RootElement.TryGetProperty("solution", out var solution)
            || solution.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidOperationException(
                $"The .slnf file '{slnfPath.FullPath}' is missing the required 'solution' object.");
        }

        if (!solution.TryGetProperty("path", out var pathElement)
            || pathElement.GetString() is not { Length: > 0 } relativeSolutionPath)
        {
            throw new InvalidOperationException(
                $"The .slnf file '{slnfPath.FullPath}' is missing the required 'solution.path' property.");
        }

        var slnfDir = slnfPath.DirectoryName;
        var parentSolutionFullPath = Path.GetFullPath(Path.Combine(slnfDir, relativeSolutionPath));
        var parentSolution = SolutionPath.FromString(parentSolutionFullPath);

        var solutionDir = parentSolution.FilePath.DirectoryName;

        var scope = new HashSet<FilePath>();

        if (!solution.TryGetProperty("projects", out var projectsElement)
            || projectsElement.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidOperationException(
                $"The .slnf file '{slnfPath.FullPath}' is missing the required 'solution.projects' array.");
        }

        foreach (var element in projectsElement.EnumerateArray())
        {
            var projectRelativePath = element.GetString();
            if (string.IsNullOrWhiteSpace(projectRelativePath))
            {
                continue;
            }

            var fullPath = Path.GetFullPath(Path.Combine(solutionDir, projectRelativePath));
            scope.Add(FilePath.FromString(fullPath));
        }

        LogSlnfRead(parentSolution.FilePath.FullPath, scope.Count);

        return new SlnfInput(parentSolution, scope);
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Read .slnf scoping to parent solution {ParentSolution} with {ProjectCount} project(s)")]
    private partial void LogSlnfRead(string parentSolution, int projectCount);
}
