namespace Cycle.Core;

public static class SolutionFilterBuilder
{
    public static SolutionFilter Build(
        string solutionFilePath,
        string outputDirectory,
        IReadOnlyList<ProjectInfo> affectedProjects)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(solutionFilePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputDirectory);
        ArgumentNullException.ThrowIfNull(affectedProjects);

        var absoluteSolutionPath = Path.GetFullPath(solutionFilePath);
        var absoluteOutputDir = Path.GetFullPath(outputDirectory);
        var solutionDir = Path.GetDirectoryName(absoluteSolutionPath)!;

        var relativeSolutionPath = Path.GetRelativePath(absoluteOutputDir, absoluteSolutionPath)
            .Replace('\\', '/');

        var relativeProjectPaths = affectedProjects
            .Select(p => Path.GetRelativePath(solutionDir, p.FilePath.FullPath)
                .Replace('\\', '/'))
            .OrderBy(p => p, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new SolutionFilter(relativeSolutionPath, relativeProjectPaths);
    }
}
