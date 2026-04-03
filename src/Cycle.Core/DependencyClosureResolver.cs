namespace Cycle.Core;

public static class DependencyClosureResolver
{
    public static ClosureResult Resolve(
        IReadOnlyList<ProjectInfo> affected,
        IReadOnlyDictionary<FilePath, IReadOnlySet<FilePath>> forwardMap,
        IReadOnlyDictionary<FilePath, ProjectInfo> projectLookup)
    {
        ArgumentNullException.ThrowIfNull(affected);
        ArgumentNullException.ThrowIfNull(forwardMap);
        ArgumentNullException.ThrowIfNull(projectLookup);

        var result = affected.ToDictionary(p => p.FilePath);
        var unresolved = new List<UnresolvedReference>();

        var queue = new Queue<FilePath>(result.Keys);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();

            if (!forwardMap.TryGetValue(current, out var dependencies))
            {
                continue;
            }

            foreach (var dep in dependencies)
            {
                if (result.ContainsKey(dep))
                {
                    continue;
                }

                if (!projectLookup.TryGetValue(dep, out var info))
                {
                    unresolved.Add(new UnresolvedReference(current, dep));
                    continue;
                }

                result.TryAdd(dep, info);
                queue.Enqueue(dep);
            }
        }

        return new ClosureResult(result.Values.ToList(), unresolved);
    }
}
