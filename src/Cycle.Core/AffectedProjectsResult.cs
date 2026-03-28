namespace Cycle.Core;

public sealed record AffectedProjectsResult(
    IReadOnlyDictionary<FilePath, ProjectInfo> AffectedProjects,
    IReadOnlyDictionary<FilePath, ProjectInfo> FailedToLoadProjects);
