namespace Cycle.Core;

public static class SolutionFilterBuilder
{
    public static SolutionFilter Build(
        SolutionPath solutionFilePath,
        string outputDirectory,
        IReadOnlyList<ProjectInfo> affectedProjects)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outputDirectory);
        ArgumentNullException.ThrowIfNull(affectedProjects);

        var absoluteSolutionPath = solutionFilePath.FilePath.FullPath;
        var absoluteOutputDir = Path.GetFullPath(outputDirectory);
        var solutionDir = solutionFilePath.FilePath.DirectoryName;

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
