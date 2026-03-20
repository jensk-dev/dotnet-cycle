using Cycle.Core;
using Microsoft.Build.Definition;
using Microsoft.Build.Evaluation;
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

        using var projectCollection = new ProjectCollection();
        var results = new List<ProjectInfo>();

        foreach (var solutionProject in solution.SolutionProjects)
        {
            var fullPath = Path.IsPathRooted(solutionProject.FilePath)
                ? Path.GetFullPath(solutionProject.FilePath)
                : Path.GetFullPath(Path.Combine(solutionDir, solutionProject.FilePath));

            var project = Project.FromFile(fullPath, new ProjectOptions
            {
                ProjectCollection = projectCollection,
            });

            var name = project.GetPropertyValue("MSBuildProjectName");
            var filePath = FilePath.FromString(fullPath);
            var extension = Path.GetExtension(fullPath);

            if (!ProjectTypeExtensions.TryFromExtension(extension, out var projectType))
            {
                continue;
            }

            results.Add(new ProjectInfo(name, filePath, projectType));
        }

        return results;
    }
}
