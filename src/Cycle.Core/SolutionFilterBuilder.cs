namespace Cycle.Core;

public static class SolutionFilterBuilder
{
    public static SolutionFilter Build(
        string solutionFilePath,
        string outputDirectory,
        IReadOnlyList<ProjectInfo> affectedProjects)
    {
        var absoluteSolutionPath = Path.GetFullPath(solutionFilePath);
        var absoluteOutputDir = Path.GetFullPath(outputDirectory);
        var solutionDir = Path.GetDirectoryName(absoluteSolutionPath)!;

        var relativeSolutionPath = Path.GetRelativePath(absoluteOutputDir, absoluteSolutionPath);

        var relativeProjectPaths = affectedProjects
            .Select(p => Path.GetRelativePath(solutionDir, p.FilePath.FullPath))
            .ToList();

        return new SolutionFilter(relativeSolutionPath, relativeProjectPaths);
    }
}
