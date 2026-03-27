namespace Cycle.Core;

public interface IProjectResolver
{
    Task<ResolutionResult> ResolveAffectedProjectsAsync(
        string solutionPath,
        IReadOnlyList<FilePath> changedFiles,
        CancellationToken ct);
}
