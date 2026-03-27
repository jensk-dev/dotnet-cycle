namespace Cycle.Core;

public sealed record ResolutionResult(
    IReadOnlyList<ProjectInfo> AffectedProjects,
    int TotalProjectCount,
    int FailedProjectCount,
    IReadOnlyList<UnresolvedReference> UnresolvedReferences);
