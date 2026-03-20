using System.Collections.Concurrent;
using Cycle.Core;
using Microsoft.Build.Definition;
using Microsoft.Build.Evaluation;
using Microsoft.VisualStudio.SolutionPersistence.Serializer;

namespace Cycle.Infrastructure.MsBuild;

public class MsBuildSolutionReader : ISolutionReader
{
    public async Task<IReadOnlyList<ProjectInfo>> GetProjectsAsync(string solutionPath, CancellationToken ct)
    {
        var serializer = SolutionSerializers.GetSerializerByMoniker(solutionPath)
            ?? throw new ArgumentException($"No serializer found for '{solutionPath}'");

        var solution = await serializer.OpenAsync(solutionPath, ct);
        var solutionDir = Path.GetDirectoryName(Path.GetFullPath(solutionPath))!;

        using var projectCollection = new ProjectCollection();
        var results = new ConcurrentBag<ProjectInfo>();

        Parallel.ForEach(solution.SolutionProjects, solutionProject => {
            var fullPath = Path.IsPathRooted(solutionProject.FilePath)
                ? Path.GetFullPath(solutionProject.FilePath)
                : Path.GetFullPath(Path.Combine(solutionDir, solutionProject.FilePath));

            var project = Project.FromFile(fullPath, new ProjectOptions
            {
                // ReSharper disable once AccessToDisposedClosure
                ProjectCollection = projectCollection
            });

            results.Add(new ProjectInfo(project.GetPropertyValue("MSBuildProjectName"), fullPath));
        });

        return results.ToList();
    }
}
