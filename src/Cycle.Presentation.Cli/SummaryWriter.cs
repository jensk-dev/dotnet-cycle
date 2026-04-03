using Cycle.Application;
using Cycle.Core;

namespace Cycle.Presentation.Cli;

internal static class SummaryWriter
{
    public static void Write(
        TextWriter writer,
        GenerateSolutionFilterResult result,
        SolutionPath solutionPath,
        FilePath outputFile,
        LogVerbosity verbosity,
        int inputFileCount)
    {
        if (verbosity is LogVerbosity.Quiet)
        {
            return;
        }

        if (verbosity is LogVerbosity.Normal or LogVerbosity.Verbose)
        {
            WriteDetailed(writer, result, solutionPath, outputFile, inputFileCount, verbosity is LogVerbosity.Verbose);
        }
        else
        {
            WriteMinimal(writer, result, outputFile);
        }
    }

    private static void WriteMinimal(
        TextWriter writer,
        GenerateSolutionFilterResult result,
        FilePath outputFile)
    {
        var closureCount = result.IncludedProjects.Count - result.AffectedProjectCount;
        var parts = new List<string>
        {
            $"{result.AffectedProjectCount} affected",
        };

        if (closureCount > 0)
        {
            parts.Add($"{closureCount} closure");
        }

        if (result.FailedProjectCount > 0)
        {
            parts.Add($"{result.FailedProjectCount} failed");
        }

        var detail = string.Join(", ", parts);
        var ms = result.Timings.Total.TotalMilliseconds;

        writer.WriteLine(
            $"cycle: {result.IncludedProjects.Count}/{result.TotalProjectCount} projects included ({detail}) [{ms:F0}ms] -> {outputFile.FileName}");
    }

    private static void WriteDetailed(
        TextWriter writer,
        GenerateSolutionFilterResult result,
        SolutionPath solutionPath,
        FilePath outputFile,
        int inputFileCount,
        bool includeProjectList)
    {
        var t = result.Timings;
        var closureCount = result.IncludedProjects.Count - result.AffectedProjectCount;

        writer.WriteLine("cycle summary");
        writer.WriteLine($"  solution:  {solutionPath.FilePath.FileName} ({result.TotalProjectCount} projects)");
        writer.WriteLine($"  input:     {inputFileCount} changed files");

        var affectedLine = $"  affected:  {result.AffectedProjectCount} projects";
        if (result.FailedProjectCount > 0)
        {
            affectedLine += $" ({result.FailedProjectCount} failed to load)";
        }

        writer.WriteLine(affectedLine);

        if (closureCount > 0)
        {
            writer.WriteLine($"  closure:   +{closureCount} dependencies");
        }

        writer.WriteLine($"  included:  {result.IncludedProjects.Count} / {result.TotalProjectCount} projects");
        writer.WriteLine($"  filtered:  {result.TotalProjectCount - result.IncludedProjects.Count} projects");
        writer.WriteLine($"  output:    {outputFile.FullPath}");
        writer.WriteLine($"  duration:  {t.Total.TotalMilliseconds:F0}ms (read: {t.SolutionRead.TotalMilliseconds:F0}ms, graph: {t.GraphLoad.TotalMilliseconds:F0}ms, resolve: {t.AffectedResolve.TotalMilliseconds:F0}ms, closure: {t.ClosureResolve.TotalMilliseconds:F0}ms, write: {t.ResultWrite.TotalMilliseconds:F0}ms)");

        if (includeProjectList && result.IncludedProjects.Count > 0)
        {
            writer.WriteLine("  projects:");
            foreach (var project in result.IncludedProjects.OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase))
            {
                writer.WriteLine($"    - {project.Name}");
            }
        }
    }
}
