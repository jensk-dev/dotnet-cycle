using System.Diagnostics;
using Cycle.Core;
using Microsoft.Extensions.Logging;

namespace Cycle.Application;

public sealed partial class GenerateSolutionFilterUseCase(
    ISolutionReader solutionReader,
    IProjectGraphLoader graphLoader,
    IResultWriter resultWriter,
    ILogger<GenerateSolutionFilterUseCase> logger)
{
    public async Task<GenerateSolutionFilterResult> ExecuteAsync(
        GenerateSolutionFilterUseCaseOptions options,
        CancellationToken ct)
    {
        var total = Stopwatch.StartNew();
        var sw = Stopwatch.StartNew();

        var projects = await solutionReader.GetProjectsAsync(options.SolutionPath, ct);
        var solutionReadTime = sw.Elapsed;
        LogSolutionRead(projects.Count);

        sw.Restart();
        var graph = graphLoader.Load(projects, options.FilesToTrace, ct);
        var graphLoadTime = sw.Elapsed;
        LogGraphLoaded(graphLoadTime.TotalMilliseconds);

        sw.Restart();
        var affectedResult = AffectedProjectsResolver.Resolve(
            graph.Projects, graph.ReverseDependencyMap, options.FilesToTrace);
        var affectedResolveTime = sw.Elapsed;
        LogAffectedResolved(affectedResult.AffectedProjects.Count, affectedResult.FailedToLoadProjects.Count, affectedResolveTime.TotalMilliseconds);

        var (includedProjects, unresolvedReferences, closureResolveTime) =
            ResolveClosure(options.IncludeClosure, affectedResult, graph);

        LogWritingOutput(options.OutputFile.FullPath);
        sw.Restart();
        await resultWriter.WriteAsync(includedProjects, options.OutputFile, ct);
        var resultWriteTime = sw.Elapsed;
        total.Stop();

        var timings = new PipelineTimings(
            solutionReadTime, graphLoadTime, affectedResolveTime,
            closureResolveTime, resultWriteTime, total.Elapsed);

        return new GenerateSolutionFilterResult(
            includedProjects,
            unresolvedReferences,
            graph.Projects.Count,
            affectedResult.FailedToLoadProjects.Count,
            affectedResult.AffectedProjects.Count, timings);
    }

    private (IReadOnlyList<ProjectInfo> Projects, IReadOnlyList<UnresolvedReference> Unresolved, TimeSpan Elapsed)
        ResolveClosure(bool includeClosure, AffectedProjectsResult affectedResult, ProjectGraph graph)
    {
        if (!includeClosure)
        {
            return (affectedResult.AffectedProjects, Array.Empty<UnresolvedReference>(), TimeSpan.Zero);
        }

        var sw = Stopwatch.StartNew();
        var closure = DependencyClosureResolver.Resolve(
            affectedResult.AffectedProjects,
            graph.ForwardDependencyMap,
            graph.ProjectLookup);
        var elapsed = sw.Elapsed;
        LogClosureResolved(closure.Projects.Count, elapsed.TotalMilliseconds);
        return (closure.Projects, closure.UnresolvedReferences, elapsed);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Solution contains {ProjectCount} project(s)")]
    private partial void LogSolutionRead(int projectCount);

    [LoggerMessage(Level = LogLevel.Information, Message = "Project graph loaded ({ElapsedMs:F0}ms)")]
    private partial void LogGraphLoaded(double elapsedMs);

    [LoggerMessage(Level = LogLevel.Information, Message = "Resolved {AffectedCount} affected project(s), {FailedCount} failed to load ({ElapsedMs:F0}ms)")]
    private partial void LogAffectedResolved(int affectedCount, int failedCount, double elapsedMs);

    [LoggerMessage(Level = LogLevel.Information, Message = "Closure expanded to {ClosureCount} project(s) ({ElapsedMs:F0}ms)")]
    private partial void LogClosureResolved(int closureCount, double elapsedMs);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Writing output to {OutputPath}")]
    private partial void LogWritingOutput(string outputPath);
}
