namespace Cycle.Core;

public sealed record ProjectGraph(
    IReadOnlyList<LoadedProjectData> Projects,
    IReadOnlyDictionary<FilePath, HashSet<FilePath>> ForwardDependencyMap,
    IReadOnlyDictionary<FilePath, HashSet<FilePath>> ReverseDependencyMap,
    IReadOnlyDictionary<FilePath, ProjectInfo> ProjectLookup);
