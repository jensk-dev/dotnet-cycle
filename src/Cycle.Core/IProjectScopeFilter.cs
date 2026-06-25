namespace Cycle.Core;

public interface IProjectScopeFilter
{
    IReadOnlyList<ProjectInfo> Apply(IReadOnlyList<ProjectInfo> projects, IReadOnlySet<FilePath>? scope);
}
