namespace Cycle.Core;

public interface ISolutionReader
{
    Task<IReadOnlyList<ProjectInfo>> GetProjectsAsync(string solutionPath, CancellationToken ct);
}
