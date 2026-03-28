namespace Cycle.Core;

public interface IProjectResolver
{
    Task<ResolutionResult> ResolveAffectedProjectsAsync(
        SolutionPath solutionPath,
        IReadOnlyList<FilePath> changedFiles,
        CancellationToken ct);
}
