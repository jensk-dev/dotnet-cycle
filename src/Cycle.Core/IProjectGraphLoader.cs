namespace Cycle.Core;

public interface IProjectGraphLoader
{
    Task<ProjectGraph> LoadAsync(
        SolutionPath solutionPath,
        IReadOnlyList<FilePath> changedFiles,
        CancellationToken ct);
}
