namespace Cycle.Core;

public sealed record LoadedProjectData(
    ProjectInfo Info,
    IReadOnlySet<string>? ResolvedItemPaths,
    IReadOnlySet<string>? ImportPaths);
