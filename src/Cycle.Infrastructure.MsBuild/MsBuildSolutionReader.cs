using Cycle.Core;
using Microsoft.VisualStudio.SolutionPersistence.Serializer;

namespace Cycle.Infrastructure.MsBuild;

public class MsBuildSolutionReader : ISolutionReader
{
    public async Task<IReadOnlyList<ProjectInfo>> GetProjectsAsync(string solutionPath, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(solutionPath);

        var serializer = SolutionSerializers.GetSerializerByMoniker(solutionPath)
            ?? throw new ArgumentException($"No serializer found for '{solutionPath}'");

        var solution = await serializer.OpenAsync(solutionPath, ct);
        var solutionDir = Path.GetDirectoryName(Path.GetFullPath(solutionPath))!;

        var results = new List<ProjectInfo>();

        foreach (var solutionProject in solution.SolutionProjects)
        {
            var fullPath = Path.IsPathRooted(solutionProject.FilePath)
                ? Path.GetFullPath(solutionProject.FilePath)
                : Path.GetFullPath(Path.Combine(solutionDir, solutionProject.FilePath));

            var name = Path.GetFileNameWithoutExtension(fullPath);
            var filePath = FilePath.FromString(fullPath);

            results.Add(new ProjectInfo(name, filePath));
        }

        return results;
    }
}
