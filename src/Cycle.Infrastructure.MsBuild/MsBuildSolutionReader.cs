using Cycle.Core;
using Microsoft.VisualStudio.SolutionPersistence.Serializer;

namespace Cycle.Infrastructure.MsBuild;

public class MsBuildSolutionReader : ISolutionReader
{
    public async Task<IReadOnlyList<ProjectInfo>> GetProjectsAsync(SolutionPath solutionPath, CancellationToken ct)
    {
        var solutionFullPath = solutionPath.FilePath.FullPath;

        var serializer = SolutionSerializers.GetSerializerByMoniker(solutionFullPath)
            ?? throw new ArgumentException($"No serializer found for '{solutionFullPath}'");

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
        }

        return results;
    }
}
