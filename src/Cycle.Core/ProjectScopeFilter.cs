namespace Cycle.Core;

public sealed class ProjectScopeFilter : IProjectScopeFilter
{
    public IReadOnlyList<ProjectInfo> Apply(
        IReadOnlyList<ProjectInfo> projects,
        IReadOnlySet<FilePath>? scope)
    {
        ArgumentNullException.ThrowIfNull(projects);

        if (scope is null)
        {
            return projects;
        }

        return projects.Where(p => scope.Contains(p.FilePath)).ToList();
    }
}
