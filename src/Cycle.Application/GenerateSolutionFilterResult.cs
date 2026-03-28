using Cycle.Core;

namespace Cycle.Application;

public sealed record GenerateSolutionFilterResult(
    SolutionFilter Filter,
    IReadOnlyList<ProjectInfo> IncludedProjects,
    int TotalProjectCount,
    int FailedProjectCount,
    IReadOnlyList<UnresolvedReference> UnresolvedReferences);
