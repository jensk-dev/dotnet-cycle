namespace Cycle.Core;

public static class AffectedProjectsResolver
{
    public static AffectedProjectsResult Resolve(
        IReadOnlyList<LoadedProjectData> projects,
        IReadOnlyDictionary<FilePath, IReadOnlySet<FilePath>> reverseMap,
        IReadOnlyList<FilePath> changedFiles)
    {
        ArgumentNullException.ThrowIfNull(projects);
        ArgumentNullException.ThrowIfNull(reverseMap);
        ArgumentNullException.ThrowIfNull(changedFiles);

        var affected = new Dictionary<FilePath, ProjectInfo>();
        var projectLookup = projects.ToDictionary(p => p.Info.FilePath, p => p.Info);

        // Projects that fail to load are always added to the output to prevent regression from silently passing CI.
        var failedToLoad = new Dictionary<FilePath, ProjectInfo>();
        foreach (var project in projects)
        {
            if (project.FailedToLoad)
            {
                affected.TryAdd(project.Info.FilePath, project.Info);
                failedToLoad.TryAdd(project.Info.FilePath, project.Info);
            }
        }

        foreach (var changedFile in changedFiles)
        {
            FindAffectedProjects(changedFile, projects, reverseMap, projectLookup, affected);
        }

        return new AffectedProjectsResult(affected, failedToLoad);
    }

    private static void FindAffectedProjects(
        FilePath changedFile,
        IReadOnlyList<LoadedProjectData> projects,
        IReadOnlyDictionary<FilePath, IReadOnlySet<FilePath>> reverseMap,
        Dictionary<FilePath, ProjectInfo> projectLookup,
        Dictionary<FilePath, ProjectInfo> affected)
    {
        var directlyAffected = new HashSet<FilePath>();

        foreach (var loaded in projects)
        {
            if (loaded.FailedToLoad || !loaded.ContainsFile(changedFile))
            {
                continue;
            }

            directlyAffected.Add(loaded.Info.FilePath);
            affected.TryAdd(loaded.Info.FilePath, loaded.Info);
        }

        var queue = new Queue<FilePath>(directlyAffected);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();

            if (!reverseMap.TryGetValue(current, out var dependents))
            {
                continue;
            }

            foreach (var dependent in dependents)
            {
                if (affected.ContainsKey(dependent))
                {
                    continue;
                }

                if (!projectLookup.TryGetValue(dependent, out var info))
                {
                    continue;
                }

                affected.TryAdd(dependent, info);
                queue.Enqueue(dependent);
            }
        }
    }
}
