namespace Cycle.Core.Tests;

public sealed class AffectedProjectsResolverTests
{
    private readonly AffectedProjectsResolver _sut = new();

    [Fact]
    public void Resolve_ChangedFileInProject_ReturnsProject()
    {
        var a = MakeProject("A");
        var changedFile = FilePath.FromString(Path.Combine(Path.GetTempPath(), "A", "File.cs"));
        var projects = new[] { MakeLoadedProject(a, itemPaths: MakePathSet(a.FilePath.FullPath, changedFile.FullPath)) };
        var reverseMap = new Dictionary<FilePath, HashSet<FilePath>>();

        var result = _sut.Resolve(projects, reverseMap, [changedFile]);

        result.AffectedProjects.Count.ShouldBe(1);
        result.AffectedProjects.ShouldContainKey(a.FilePath);
    }

    [Fact]
    public void Resolve_ChangedFileNotInAnyProject_ReturnsEmpty()
    {
        var a = MakeProject("A");
        var changedFile = FilePath.FromString(Path.Combine(Path.GetTempPath(), "Other", "File.cs"));
        var projects = new[] { MakeLoadedProject(a) };
        var reverseMap = new Dictionary<FilePath, HashSet<FilePath>>();

        var result = _sut.Resolve(projects, reverseMap, [changedFile]);

        result.AffectedProjects.ShouldBeEmpty();
        result.FailedToLoadProjects.ShouldBeEmpty();
    }

    [Fact]
    public void Resolve_FailedProject_AlwaysIncluded()
    {
        var a = MakeProject("A");
        var projects = new[] { MakeFailedProject(a) };
        var reverseMap = new Dictionary<FilePath, HashSet<FilePath>>();

        var result = _sut.Resolve(projects, reverseMap, Array.Empty<FilePath>());

        result.AffectedProjects.Count.ShouldBe(1);
        result.AffectedProjects.ShouldContainKey(a.FilePath);
        result.FailedToLoadProjects.Count.ShouldBe(1);
        result.FailedToLoadProjects.ShouldContainKey(a.FilePath);
    }

    [Fact]
    public void Resolve_TransitiveDependents_IncludedViaBfs()
    {
        var a = MakeProject("A");
        var b = MakeProject("B");
        var c = MakeProject("C");

        var changedFile = FilePath.FromString(Path.Combine(Path.GetTempPath(), "C", "File.cs"));
        var projects = new[]
        {
            MakeLoadedProject(a),
            MakeLoadedProject(b),
            MakeLoadedProject(c, itemPaths: MakePathSet(c.FilePath.FullPath, changedFile.FullPath)),
        };

        // B depends on C, A depends on B
        var reverseMap = new Dictionary<FilePath, HashSet<FilePath>>
        {
            [c.FilePath] = [b.FilePath],
            [b.FilePath] = [a.FilePath],
        };

        var result = _sut.Resolve(projects, reverseMap, [changedFile]);

        result.AffectedProjects.Count.ShouldBe(3);
        result.AffectedProjects.ShouldContainKey(a.FilePath);
        result.AffectedProjects.ShouldContainKey(b.FilePath);
        result.AffectedProjects.ShouldContainKey(c.FilePath);
    }

    [Fact]
    public void Resolve_ImportFileChanged_ReturnsProject()
    {
        var a = MakeProject("A");
        var propsFile = FilePath.FromString(Path.Combine(Path.GetTempPath(), "Directory.Build.props"));
        var projects = new[]
        {
            MakeLoadedProject(a, importPaths: MakePathSet(propsFile.FullPath)),
        };
        var reverseMap = new Dictionary<FilePath, HashSet<FilePath>>();

        var result = _sut.Resolve(projects, reverseMap, [propsFile]);

        result.AffectedProjects.Count.ShouldBe(1);
        result.AffectedProjects.ShouldContainKey(a.FilePath);
    }

    [Fact]
    public void Resolve_MultipleChangedFiles_ReturnsUnion()
    {
        var a = MakeProject("A");
        var b = MakeProject("B");
        var fileA = FilePath.FromString(Path.Combine(Path.GetTempPath(), "A", "File.cs"));
        var fileB = FilePath.FromString(Path.Combine(Path.GetTempPath(), "B", "File.cs"));

        var projects = new[]
        {
            MakeLoadedProject(a, itemPaths: MakePathSet(a.FilePath.FullPath, fileA.FullPath)),
            MakeLoadedProject(b, itemPaths: MakePathSet(b.FilePath.FullPath, fileB.FullPath)),
        };
        var reverseMap = new Dictionary<FilePath, HashSet<FilePath>>();

        var result = _sut.Resolve(projects, reverseMap, [fileA, fileB]);

        result.AffectedProjects.Count.ShouldBe(2);
        result.AffectedProjects.ShouldContainKey(a.FilePath);
        result.AffectedProjects.ShouldContainKey(b.FilePath);
    }

    [Fact]
    public void Resolve_NoChangedFilesNoFailures_ReturnsEmpty()
    {
        var a = MakeProject("A");
        var projects = new[] { MakeLoadedProject(a) };
        var reverseMap = new Dictionary<FilePath, HashSet<FilePath>>();

        var result = _sut.Resolve(projects, reverseMap, Array.Empty<FilePath>());

        result.AffectedProjects.ShouldBeEmpty();
        result.FailedToLoadProjects.ShouldBeEmpty();
    }

    [Fact]
    public void Resolve_SharedDependent_NoDuplicates()
    {
        var a = MakeProject("A");
        var b = MakeProject("B");
        var shared = MakeProject("Shared");
        var fileA = FilePath.FromString(Path.Combine(Path.GetTempPath(), "A", "File.cs"));
        var fileB = FilePath.FromString(Path.Combine(Path.GetTempPath(), "B", "File.cs"));

        var projects = new[]
        {
            MakeLoadedProject(a, itemPaths: MakePathSet(a.FilePath.FullPath, fileA.FullPath)),
            MakeLoadedProject(b, itemPaths: MakePathSet(b.FilePath.FullPath, fileB.FullPath)),
            MakeLoadedProject(shared),
        };

        // Shared depends on both A and B
        var reverseMap = new Dictionary<FilePath, HashSet<FilePath>>
        {
            [a.FilePath] = [shared.FilePath],
            [b.FilePath] = [shared.FilePath],
        };

        var result = _sut.Resolve(projects, reverseMap, [fileA, fileB]);

        result.AffectedProjects.Count.ShouldBe(3);
        result.AffectedProjects.ShouldContainKey(a.FilePath);
        result.AffectedProjects.ShouldContainKey(b.FilePath);
        result.AffectedProjects.ShouldContainKey(shared.FilePath);
    }

    [Fact]
    public void Resolve_FailedToLoadProjects_MatchesActualFailures()
    {
        var a = MakeProject("A");
        var b = MakeProject("B");
        var c = MakeProject("C");

        var projects = new[]
        {
            MakeLoadedProject(a),
            MakeFailedProject(b),
            MakeFailedProject(c),
        };
        var reverseMap = new Dictionary<FilePath, HashSet<FilePath>>();

        var result = _sut.Resolve(projects, reverseMap, Array.Empty<FilePath>());

        result.FailedToLoadProjects.Count.ShouldBe(2);
        result.FailedToLoadProjects.ShouldContainKey(b.FilePath);
        result.FailedToLoadProjects.ShouldContainKey(c.FilePath);
        result.AffectedProjects.Count.ShouldBe(2);
    }

    [Fact]
    public void Resolve_DependentNotInProjectLookup_Skipped()
    {
        var a = MakeProject("A");
        var external = MakeProject("External");
        var changedFile = FilePath.FromString(Path.Combine(Path.GetTempPath(), "A", "File.cs"));

        var projects = new[]
        {
            MakeLoadedProject(a, itemPaths: MakePathSet(a.FilePath.FullPath, changedFile.FullPath)),
        };

        var reverseMap = new Dictionary<FilePath, HashSet<FilePath>>
        {
            [a.FilePath] = [external.FilePath],
        };

        var result = _sut.Resolve(projects, reverseMap, [changedFile]);

        result.AffectedProjects.Count.ShouldBe(1);
        result.AffectedProjects.ShouldContainKey(a.FilePath);
        result.AffectedProjects.ShouldNotContainKey(external.FilePath);
    }

    [Fact]
    public void Resolve_NullProjects_ThrowsArgumentNullException()
    {
        var reverseMap = new Dictionary<FilePath, HashSet<FilePath>>();
        Should.Throw<ArgumentNullException>(() => _sut.Resolve(null!, reverseMap, Array.Empty<FilePath>()));
    }

    [Fact]
    public void Resolve_NullReverseMap_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => _sut.Resolve(Array.Empty<LoadedProjectData>(), null!, Array.Empty<FilePath>()));
    }

    [Fact]
    public void Resolve_NullChangedFiles_ThrowsArgumentNullException()
    {
        var reverseMap = new Dictionary<FilePath, HashSet<FilePath>>();
        Should.Throw<ArgumentNullException>(() => _sut.Resolve(Array.Empty<LoadedProjectData>(), reverseMap, null!));
    }

    private static ProjectInfo MakeProject(string name)
    {
        var path = Path.Combine(Path.GetTempPath(), name, $"{name}.csproj");
        return new ProjectInfo(name, FilePath.FromString(path));
    }

    private static HashSet<string> MakePathSet(params string[] paths)
    {
        var set = new HashSet<string>(FilePath.PathComparer);
        foreach (var p in paths)
        {
            set.Add(p);
        }
        return set;
    }

    private static LoadedProjectData MakeLoadedProject(
        ProjectInfo info,
        HashSet<string>? itemPaths = null,
        HashSet<string>? importPaths = null)
    {
        itemPaths ??= new HashSet<string>(FilePath.PathComparer) { info.FilePath.FullPath };
        importPaths ??= new HashSet<string>(FilePath.PathComparer);
        return new LoadedProjectData(info, itemPaths, importPaths);
    }

    private static LoadedProjectData MakeFailedProject(ProjectInfo info)
    {
        return new LoadedProjectData(info, null, null);
    }
}
