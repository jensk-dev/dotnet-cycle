namespace Cycle.Core;

public sealed record AffectedProjectsResult(
    IReadOnlyList<ProjectInfo> AffectedProjects,
    IReadOnlyList<ProjectInfo> FailedToLoadProjects);
