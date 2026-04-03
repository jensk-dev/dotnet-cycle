using Cycle.Core;

namespace Cycle.Application;

public interface IProjectGraphLoader
{
    ProjectGraph Load(
        IReadOnlyList<ProjectInfo> projects,
        IReadOnlyList<FilePath> changedFiles,
        CancellationToken ct);
}
