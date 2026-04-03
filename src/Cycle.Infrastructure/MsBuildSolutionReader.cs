using Cycle.Application;
using Cycle.Core;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.SolutionPersistence.Serializer;

namespace Cycle.Infrastructure;

public sealed partial class MsBuildSolutionReader(
    ILogger<MsBuildSolutionReader> logger)
    : ISolutionReader
{
    public async Task<IReadOnlyList<ProjectInfo>> GetProjectsAsync(SolutionPath solutionPath, CancellationToken ct)
    {
        var solutionFullPath = solutionPath.FilePath.FullPath;

        var serializer = SolutionSerializers.GetSerializerByMoniker(solutionFullPath)
            ?? throw new ArgumentException($"No serializer found for '{solutionFullPath}'", nameof(solutionPath));

        var solution = await serializer.OpenAsync(solutionFullPath, ct);
        var solutionDir = solutionPath.FilePath.DirectoryName;

        var results = new List<ProjectInfo>();

        foreach (var solutionProject in solution.SolutionProjects)
        {
            var fullPath = Path.IsPathRooted(solutionProject.FilePath)
                ? Path.GetFullPath(solutionProject.FilePath)
                : Path.GetFullPath(Path.Combine(solutionDir, solutionProject.FilePath));

            var name = Path.GetFileNameWithoutExtension(fullPath);
            var filePath = FilePath.FromString(fullPath);

            results.Add(new ProjectInfo(name, filePath));
            LogFoundProject(name, fullPath);
        }

        return results;
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Found project {ProjectName} at {ProjectPath}")]
    private partial void LogFoundProject(string projectName, string projectPath);
}
