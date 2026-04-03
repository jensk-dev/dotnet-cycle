using Cycle.Core;

namespace Cycle.Application;

public sealed record GenerateSolutionFilterResult(
    IReadOnlyList<ProjectInfo> IncludedProjects,
    IReadOnlyList<UnresolvedReference> UnresolvedReferences,
    int TotalProjectCount,
    int FailedProjectCount,
    int AffectedProjectCount,
    PipelineTimings Timings);
