namespace Cycle.Application;

public sealed record PipelineTimings(
    TimeSpan SolutionRead,
    TimeSpan GraphLoad,
    TimeSpan AffectedResolve,
    TimeSpan ClosureResolve,
    TimeSpan ResultWrite,
    TimeSpan Total);
