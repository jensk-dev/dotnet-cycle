namespace Cycle.Core;

public interface IProjectResolver
{
    Task<IReadOnlyList<ProjectInfo>> ResolveAffectedProjectsAsync(
        string solutionPath,
        IReadOnlyList<FilePath> changedFiles,
        CancellationToken ct);
}
