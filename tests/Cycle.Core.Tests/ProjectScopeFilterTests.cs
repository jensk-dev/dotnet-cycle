namespace Cycle.Core.Tests;

public sealed class ProjectScopeFilterTests
{
    private static readonly ProjectScopeFilter Filter = new();

    [Fact]
    public void Apply_NullScope_ReturnsAllProjectsUnchanged()
    {
        var a = MakeProject("A");
        var b = MakeProject("B");
        var projects = new[] { a, b };

        var result = Filter.Apply(projects, null);

        result.ShouldBeSameAs(projects);
    }

    [Fact]
    public void Apply_Scope_FiltersToSubset()
    {
        var a = MakeProject("A");
        var b = MakeProject("B");
        var projects = new[] { a, b };
        var scope = new HashSet<FilePath> { a.FilePath };

        var result = Filter.Apply(projects, scope);

        result.Count.ShouldBe(1);
        result[0].Name.ShouldBe("A");
    }

    [Fact]
    public void Apply_EmptyScope_ReturnsEmpty()
    {
        var projects = new[] { MakeProject("A"), MakeProject("B") };

        var result = Filter.Apply(projects, new HashSet<FilePath>());

        result.ShouldBeEmpty();
    }

    [Fact]
    public void Apply_ScopeWithUnknownPaths_IgnoresThem()
    {
        var a = MakeProject("A");
        var projects = new[] { a };
        var scope = new HashSet<FilePath> { a.FilePath, MakeProject("Missing").FilePath };

        var result = Filter.Apply(projects, scope);

        result.Count.ShouldBe(1);
        result[0].Name.ShouldBe("A");
    }

    [Fact]
    public void Apply_NullProjects_Throws()
    {
        Should.Throw<ArgumentNullException>(() => Filter.Apply(null!, new HashSet<FilePath>()));
    }

    private static ProjectInfo MakeProject(string name)
    {
        var path = Path.Combine(Path.GetTempPath(), name, $"{name}.csproj");
        return new ProjectInfo(name, FilePath.FromString(path));
    }
}
