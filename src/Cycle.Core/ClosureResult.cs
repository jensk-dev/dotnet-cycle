namespace Cycle.Core;

public sealed record ClosureResult(
    IReadOnlyList<ProjectInfo> Projects,
    IReadOnlyList<UnresolvedReference> UnresolvedReferences);
