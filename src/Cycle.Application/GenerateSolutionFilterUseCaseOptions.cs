using Cycle.Core;

namespace Cycle.Application;

public sealed class GenerateSolutionFilterUseCaseOptions
{
    public required SolutionPath SolutionPath { get; init; }
    public required IReadOnlyList<FilePath> FilesToTrace { get; init; }
    public required bool IncludeClosure { get; init; }
    public required FilePath OutputFile { get; init; }
}
