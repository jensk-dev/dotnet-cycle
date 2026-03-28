using Cycle.Core;

namespace Cycle.Application;

public sealed record GenerateSolutionFilterResult(
    SolutionFilter Filter,
    ResolutionResult Resolution);
