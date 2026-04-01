namespace Cycle.Core;

public sealed record ProjectGraph(
    IReadOnlyList<LoadedProjectData> Projects,
    IReadOnlyDictionary<FilePath, IReadOnlySet<FilePath>> ForwardDependencyMap,
    IReadOnlyDictionary<FilePath, IReadOnlySet<FilePath>> ReverseDependencyMap,
    IReadOnlyDictionary<FilePath, ProjectInfo> ProjectLookup);
