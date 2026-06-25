namespace Cycle.Core;

public interface IDependencyClosureResolver
{
    ClosureResult Resolve(
        IReadOnlyList<ProjectInfo> affected,
        IReadOnlyDictionary<FilePath, IReadOnlySet<FilePath>> forwardMap,
        IReadOnlyDictionary<FilePath, ProjectInfo> projectLookup);
}
