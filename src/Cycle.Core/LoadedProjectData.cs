namespace Cycle.Core;

public sealed class LoadedProjectData
{
    private readonly IReadOnlySet<FilePath> _resolvedItemPaths;
    private readonly IReadOnlySet<FilePath> _importPaths;

    private LoadedProjectData(
        ProjectInfo info,
        bool failedToLoad,
        IReadOnlySet<FilePath> resolvedItemPaths,
        IReadOnlySet<FilePath> importPaths)
    {
        Info = info;
        FailedToLoad = failedToLoad;
        _resolvedItemPaths = resolvedItemPaths;
        _importPaths = importPaths;
    }

    public ProjectInfo Info { get; }

    public bool FailedToLoad { get; }

    public bool ContainsFile(FilePath path)
        => !FailedToLoad
           && (_resolvedItemPaths.Contains(path) || _importPaths.Contains(path));

    public static LoadedProjectData Loaded(
        ProjectInfo info,
        IReadOnlySet<FilePath> resolvedItemPaths,
        IReadOnlySet<FilePath> importPaths)
        => new(info, false, resolvedItemPaths, importPaths);

    public static LoadedProjectData Failed(ProjectInfo info)
        => new(info, true, new HashSet<FilePath>(), new HashSet<FilePath>());
}
