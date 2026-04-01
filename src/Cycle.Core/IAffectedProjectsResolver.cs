namespace Cycle.Core;

public interface IAffectedProjectsResolver
{
    AffectedProjectsResult Resolve(
        IReadOnlyList<LoadedProjectData> projects,
        IReadOnlyDictionary<FilePath, IReadOnlySet<FilePath>> reverseMap,
        IReadOnlyList<FilePath> changedFiles);
}
