using Cycle.Core;

namespace Cycle.Application;

public interface ISolutionReader
{
    Task<IReadOnlyList<ProjectInfo>> GetProjectsAsync(SolutionPath solutionPath, CancellationToken ct);
}
