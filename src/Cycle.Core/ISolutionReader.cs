namespace Cycle.Core;

public interface ISolutionReader
{
    Task<IReadOnlyList<ProjectInfo>> GetProjectsAsync(SolutionPath solutionPath, CancellationToken ct);
}
