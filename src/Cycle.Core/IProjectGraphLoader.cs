namespace Cycle.Core;

public interface IProjectGraphLoader
{
    ProjectGraph Load(
        IReadOnlyList<ProjectInfo> projects,
        IReadOnlyList<FilePath> changedFiles,
        CancellationToken ct);
}
