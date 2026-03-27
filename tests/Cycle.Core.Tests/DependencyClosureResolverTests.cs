namespace Cycle.Core.Tests;

[TestFixture]
public class DependencyClosureResolverTests
{
    [Test]
    public void Resolve_DirectDependency_IncludesDependency()
    {
        var a = MakeProject("A");
        var b = MakeProject("B");

        var affected = new Dictionary<FilePath, ProjectInfo> { [a.FilePath] = a };
        var forwardMap = new Dictionary<FilePath, HashSet<FilePath>>
        {
            [a.FilePath] = [b.FilePath],
        };
        var projectLookup = new Dictionary<FilePath, ProjectInfo>
        {
            [a.FilePath] = a,
            [b.FilePath] = b,
        };

        var result = DependencyClosureResolver.Resolve(affected, forwardMap, projectLookup);

        result.Projects.Count.ShouldBe(2);
        var names = result.Projects.Select(p => p.Name).OrderBy(n => n).ToList();
        names.ShouldBe(["A", "B"]);
        result.UnresolvedReferences.ShouldBeEmpty();
    }

    [Test]
    public void Resolve_TransitiveChain_IncludesAllDependencies()
    {
        var a = MakeProject("A");
        var b = MakeProject("B");
        var c = MakeProject("C");

        var affected = new Dictionary<FilePath, ProjectInfo> { [a.FilePath] = a };
        var forwardMap = new Dictionary<FilePath, HashSet<FilePath>>
        {
            [a.FilePath] = [b.FilePath],
            [b.FilePath] = [c.FilePath],
        };
        var projectLookup = new Dictionary<FilePath, ProjectInfo>
        {
            [a.FilePath] = a,
            [b.FilePath] = b,
            [c.FilePath] = c,
        };

        var result = DependencyClosureResolver.Resolve(affected, forwardMap, projectLookup);

        result.Projects.Count.ShouldBe(3);
        var names = result.Projects.Select(p => p.Name).OrderBy(n => n).ToList();
        names.ShouldBe(["A", "B", "C"]);
    }

    [Test]
    public void Resolve_SharedDependency_NoDuplicates()
    {
        var a = MakeProject("A");
        var b = MakeProject("B");
        var shared = MakeProject("Shared");

        var affected = new Dictionary<FilePath, ProjectInfo>
        {
            [a.FilePath] = a,
            [b.FilePath] = b,
        };
        var forwardMap = new Dictionary<FilePath, HashSet<FilePath>>
        {
            [a.FilePath] = [shared.FilePath],
            [b.FilePath] = [shared.FilePath],
        };
        var projectLookup = new Dictionary<FilePath, ProjectInfo>
        {
            [a.FilePath] = a,
            [b.FilePath] = b,
            [shared.FilePath] = shared,
        };

        var result = DependencyClosureResolver.Resolve(affected, forwardMap, projectLookup);

        result.Projects.Count.ShouldBe(3);
        var names = result.Projects.Select(p => p.Name).OrderBy(n => n).ToList();
        names.ShouldBe(["A", "B", "Shared"]);
    }

    [Test]
    public void Resolve_UnresolvedReference_TrackedInResult()
    {
        var a = MakeProject("A");
        var missingPath = FilePath.FromString(Path.Combine(Path.GetTempPath(), "Missing", "Missing.csproj"));

        var affected = new Dictionary<FilePath, ProjectInfo> { [a.FilePath] = a };
        var forwardMap = new Dictionary<FilePath, HashSet<FilePath>>
        {
            [a.FilePath] = [missingPath],
        };
        var projectLookup = new Dictionary<FilePath, ProjectInfo>
        {
            [a.FilePath] = a,
        };

        var result = DependencyClosureResolver.Resolve(affected, forwardMap, projectLookup);

        result.Projects.Count.ShouldBe(1);
        result.UnresolvedReferences.Count.ShouldBe(1);
        result.UnresolvedReferences[0].ReferencedBy.ShouldBe(a.FilePath);
        result.UnresolvedReferences[0].ReferencePath.ShouldBe(missingPath);
    }

    [Test]
    public void Resolve_EmptyInput_ReturnsEmpty()
    {
        var affected = new Dictionary<FilePath, ProjectInfo>();
        var forwardMap = new Dictionary<FilePath, HashSet<FilePath>>();
        var projectLookup = new Dictionary<FilePath, ProjectInfo>();

        var result = DependencyClosureResolver.Resolve(affected, forwardMap, projectLookup);

        result.Projects.ShouldBeEmpty();
        result.UnresolvedReferences.ShouldBeEmpty();
    }

    [Test]
    public void Resolve_NoDependencies_ReturnsAffectedOnly()
    {
        var a = MakeProject("A");

        var affected = new Dictionary<FilePath, ProjectInfo> { [a.FilePath] = a };
        var forwardMap = new Dictionary<FilePath, HashSet<FilePath>>();
        var projectLookup = new Dictionary<FilePath, ProjectInfo>
        {
            [a.FilePath] = a,
        };

        var result = DependencyClosureResolver.Resolve(affected, forwardMap, projectLookup);

        result.Projects.Count.ShouldBe(1);
        result.Projects[0].Name.ShouldBe("A");
    }

    private static ProjectInfo MakeProject(string name)
    {
        var path = Path.Combine(Path.GetTempPath(), name, $"{name}.csproj");
        return new ProjectInfo(name, FilePath.FromString(path));
    }
}
