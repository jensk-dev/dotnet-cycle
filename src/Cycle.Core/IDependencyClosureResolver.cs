namespace Cycle.Core;

public interface IDependencyClosureResolver
{
    ClosureResult Resolve(
        IReadOnlyDictionary<FilePath, ProjectInfo> affected,
        IReadOnlyDictionary<FilePath, HashSet<FilePath>> forwardMap,
        IReadOnlyDictionary<FilePath, ProjectInfo> projectLookup);
}
