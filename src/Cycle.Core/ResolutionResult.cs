namespace Cycle.Core;

public sealed record ResolutionResult(
    IReadOnlyDictionary<FilePath, ProjectInfo> AffectedProjects,
    int TotalProjectCount,
    int FailedProjectCount,
    IReadOnlyDictionary<FilePath, HashSet<FilePath>> ForwardDependencyMap,
    IReadOnlyDictionary<FilePath, ProjectInfo> ProjectLookup);
