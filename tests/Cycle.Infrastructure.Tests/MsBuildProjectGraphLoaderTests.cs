using Cycle.Core;
using Cycle.Infrastructure.Tests.Helpers;
using Cycle.Tests.Common;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cycle.Infrastructure.Tests;

public sealed class MsBuildProjectGraphLoaderTests : IClassFixture<MsBuildFixture>, IDisposable
{
    private readonly string _testDir;
    private readonly List<IDisposable> _disposables = [];

    public MsBuildProjectGraphLoaderTests(MsBuildFixture _)
    {
        _testDir = Path.Combine(Path.GetTempPath(), $"cycle-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testDir);
    }

    public void Dispose()
    {
        foreach (var d in _disposables)
        {
            d.Dispose();
        }

        _disposables.Clear();

        if (Directory.Exists(_testDir))
        {
            Directory.Delete(_testDir, true);
        }
    }

    [Fact]
    public async Task LoadAndResolve_ChangeToFileInProject_ReturnsProject()
    {
        var projectA = CreateProject("ProjectA");
        projectA.Create();
        var (filePath, _) = projectA.AddFileToProject("Class1.cs", ProjectItemType.Compile, false, "class A {}");
        var slnPath = CreateSolution(projectA);

        var changed = new[] { Core.FilePath.FromString(filePath) };
        var affected = await LoadAndResolveAffectedAsync(slnPath, changed);

        affected.AffectedProjects.Count.ShouldBe(1);
        affected.AffectedProjects.Values.Single().Name.ShouldBe("ProjectA");
    }

    [Fact]
    public async Task LoadAndResolve_ChangeToProjectFile_ReturnsProject()
    {
        var projectA = CreateProject("ProjectA");
        projectA.Create();
        var slnPath = CreateSolution(projectA);

        var changed = new[] { Core.FilePath.FromString(projectA.ProjectFilePath) };
        var affected = await LoadAndResolveAffectedAsync(slnPath, changed);

        affected.AffectedProjects.Count.ShouldBe(1);
        affected.AffectedProjects.Values.Single().Name.ShouldBe("ProjectA");
    }

    [Fact]
    public async Task LoadAndResolve_ChangeToUnrelatedFile_ReturnsEmpty()
    {
        var projectA = CreateProject("ProjectA");
        projectA.Create();
        projectA.AddFileToProject("Class1.cs", ProjectItemType.Compile, false, "class A {}");
        var slnPath = CreateSolution(projectA);

        // Create a file that is not in any project
        var unrelatedFile = Path.Combine(_testDir, "unrelated.txt");
        File.WriteAllText(unrelatedFile, "unrelated");

        var changed = new[] { Core.FilePath.FromString(unrelatedFile) };
        var affected = await LoadAndResolveAffectedAsync(slnPath, changed);

        affected.AffectedProjects.Count.ShouldBe(0);
    }

    [Fact]
    public async Task LoadAndResolve_TransitiveDependency_ReturnsAllAffected()
    {
        // C -> B -> A: change in C affects C, B, and A
        var projectC = CreateProject("ProjectC");
        projectC.Create();
        var (fileInC, _) = projectC.AddFileToProject("ClassC.cs", ProjectItemType.Compile, false, "class C {}");

        var projectB = CreateProject("ProjectB");
        projectB.Create();
        projectB.AddProjectReference(projectC);

        var projectA = CreateProject("ProjectA");
        projectA.Create();
        projectA.AddProjectReference(projectB);

        var slnPath = CreateSolution(projectC, projectB, projectA);

        var changed = new[] { Core.FilePath.FromString(fileInC) };
        var affected = await LoadAndResolveAffectedAsync(slnPath, changed);

        affected.AffectedProjects.Count.ShouldBe(3);
        var names = affected.AffectedProjects.Values.Select(p => p.Name).OrderBy(n => n).ToList();
        names.ShouldBe(["ProjectA", "ProjectB", "ProjectC"]);
    }

    [Fact]
    public async Task LoadAndResolve_ChangeInDependency_AffectsDependents()
    {
        // B references A: change in A affects both A and B
        var projectA = CreateProject("ProjectA");
        projectA.Create();
        var (fileInA, _) = projectA.AddFileToProject("ClassA.cs", ProjectItemType.Compile, false, "class A {}");

        var projectB = CreateProject("ProjectB");
        projectB.Create();
        projectB.AddProjectReference(projectA);

        var slnPath = CreateSolution(projectA, projectB);

        var changed = new[] { Core.FilePath.FromString(fileInA) };
        var affected = await LoadAndResolveAffectedAsync(slnPath, changed);

        affected.AffectedProjects.Count.ShouldBe(2);
        var names = affected.AffectedProjects.Values.Select(p => p.Name).OrderBy(n => n).ToList();
        names.ShouldBe(["ProjectA", "ProjectB"]);
    }

    [Fact]
    public async Task LoadAndResolve_ContentFile_ReturnsProject()
    {
        var projectA = CreateProject("ProjectA");
        projectA.Create();
        var (filePath, _) = projectA.AddFileToProject("data.json", ProjectItemType.Content, false, "{}");
        var slnPath = CreateSolution(projectA);

        var changed = new[] { Core.FilePath.FromString(filePath) };
        var affected = await LoadAndResolveAffectedAsync(slnPath, changed);

        affected.AffectedProjects.Count.ShouldBe(1);
        affected.AffectedProjects.Values.Single().Name.ShouldBe("ProjectA");
    }

    [Fact]
    public async Task LoadAndResolve_EmbeddedResource_ReturnsProject()
    {
        var projectA = CreateProject("ProjectA");
        projectA.Create();
        var (filePath, _) = projectA.AddFileToProject("resource.resx", ProjectItemType.EmbeddedResource, false, "<root/>");
        var slnPath = CreateSolution(projectA);

        var changed = new[] { Core.FilePath.FromString(filePath) };
        var affected = await LoadAndResolveAffectedAsync(slnPath, changed);

        affected.AffectedProjects.Count.ShouldBe(1);
    }

    [Fact]
    public async Task LoadAndResolve_DeletedFile_HandledViaPhantomFiles()
    {
        var projectA = CreateProject("ProjectA");
        projectA.Create();
        var (filePath, _) = projectA.AddFileToProject("ToDelete.cs", ProjectItemType.Compile, false, "class D {}");
        var slnPath = CreateSolution(projectA);

        // Delete the file to simulate a deleted changed file
        File.Delete(filePath);

        var changed = new[] { Core.FilePath.FromString(filePath) };
        var affected = await LoadAndResolveAffectedAsync(slnPath, changed);

        affected.AffectedProjects.Count.ShouldBe(1);
        affected.AffectedProjects.Values.Single().Name.ShouldBe("ProjectA");

        // Phantom file should be cleaned up
        File.Exists(filePath).ShouldBeFalse();
    }

    [Fact]
    public async Task LoadAndResolve_MultipleChangedFiles_ReturnsUnion()
    {
        var projectA = CreateProject("ProjectA");
        projectA.Create();
        var (fileA, _) = projectA.AddFileToProject("A.cs", ProjectItemType.Compile, false, "class A {}");

        var projectB = CreateProject("ProjectB");
        projectB.Create();
        var (fileB, _) = projectB.AddFileToProject("B.cs", ProjectItemType.Compile, false, "class B {}");

        var slnPath = CreateSolution(projectA, projectB);

        var changed = new[]
        {
            Core.FilePath.FromString(fileA),
            Core.FilePath.FromString(fileB),
        };
        var affected = await LoadAndResolveAffectedAsync(slnPath, changed);

        affected.AffectedProjects.Count.ShouldBe(2);
        var names = affected.AffectedProjects.Values.Select(p => p.Name).OrderBy(n => n).ToList();
        names.ShouldBe(["ProjectA", "ProjectB"]);
    }

    [Fact]
    public async Task LoadAndResolve_NoChangedFiles_ReturnsEmpty()
    {
        var projectA = CreateProject("ProjectA");
        projectA.Create();
        projectA.AddFileToProject("A.cs", ProjectItemType.Compile, false, "class A {}");
        var slnPath = CreateSolution(projectA);

        var affected = await LoadAndResolveAffectedAsync(slnPath, []);

        affected.AffectedProjects.Count.ShouldBe(0);
    }

    [Fact]
    public async Task LoadAndResolve_ImportFileChanged_ReturnsProject()
    {
        var projectA = CreateProject("ProjectA");
        projectA.Create();
        projectA.AddFileToProject("Class1.cs", ProjectItemType.Compile, false, "class A {}");

        // Create a .props file and import it
        var propsPath = Path.Combine(projectA.ProjectDirectory, "Common.props");
        File.WriteAllText(propsPath, "<Project></Project>");
        projectA.AddImport("Common.props");

        var slnPath = CreateSolution(projectA);

        var changed = new[] { Core.FilePath.FromString(propsPath) };
        var affected = await LoadAndResolveAffectedAsync(slnPath, changed);

        affected.AffectedProjects.Count.ShouldBe(1);
        affected.AffectedProjects.Values.Single().Name.ShouldBe("ProjectA");
    }

    [Fact]
    public async Task LoadAndResolve_TargetsFileChanged_ReturnsProject()
    {
        var projectA = CreateProject("ProjectA");
        projectA.Create();

        var targetsPath = Path.Combine(projectA.ProjectDirectory, "Build.targets");
        File.WriteAllText(targetsPath, "<Project></Project>");
        projectA.AddImport("Build.targets");

        var slnPath = CreateSolution(projectA);

        var changed = new[] { Core.FilePath.FromString(targetsPath) };
        var affected = await LoadAndResolveAffectedAsync(slnPath, changed);

        affected.AffectedProjects.Count.ShouldBe(1);
        affected.AffectedProjects.Values.Single().Name.ShouldBe("ProjectA");
    }

    [Fact]
    public async Task LoadAndResolve_MultiTargetFramework_FileInProject_ReturnsProject()
    {
        var projectA = CreateProject("ProjectA");
        projectA.Create();
        projectA.SetTargetFrameworks("net8.0;net10.0");
        var (filePath, _) = projectA.AddFileToProject("Class1.cs", ProjectItemType.Compile, false, "class A {}");

        var slnPath = CreateSolution(projectA);

        var changed = new[] { Core.FilePath.FromString(filePath) };
        var affected = await LoadAndResolveAffectedAsync(slnPath, changed);

        affected.AffectedProjects.Count.ShouldBe(1);
        affected.AffectedProjects.Values.Single().Name.ShouldBe("ProjectA");
    }

    [Fact]
    public async Task LoadAndResolve_InvalidProjectLoadFailure_TreatedAsAffected()
    {
        var projectA = CreateProject("ProjectA");
        projectA.CreateWithContent("this is not valid xml");

        var slnPath = CreateSolution(projectA);

        var (graph, affected) = await LoadAndResolveWithGraphAsync(slnPath, []);

        // Projects that fail to load are always included
        affected.AffectedProjects.Count.ShouldBe(1);
        affected.AffectedProjects.Values.Single().Name.ShouldBe("ProjectA");
        affected.FailedToLoadProjects.Count.ShouldBe(1);
    }

    [Fact]
    public async Task LoadAndResolve_InvalidProjectReference_DoesNotThrow()
    {
        var projectA = CreateProject("ProjectA");
        projectA.Create();
        var (fileInA, _) = projectA.AddFileToProject("ClassA.cs", ProjectItemType.Compile, false, "class A {}");

        // Add a reference to a project not in the solution
        var csProjContent = File.ReadAllText(projectA.ProjectFilePath);
        csProjContent = csProjContent.Replace("</Project>",
            """
            <ItemGroup>
              <ProjectReference Include="..\NonExistent\NonExistent.csproj" />
            </ItemGroup>
            </Project>
            """);
        File.WriteAllText(projectA.ProjectFilePath, csProjContent);

        var slnPath = CreateSolution(projectA);

        var changed = new[] { Core.FilePath.FromString(fileInA) };

        // Should not throw, and should still find ProjectA as affected
        var affected = await LoadAndResolveAffectedAsync(slnPath, changed);
        affected.AffectedProjects.Count.ShouldBe(1);
        affected.AffectedProjects.Values.Single().Name.ShouldBe("ProjectA");
    }

    // todo: is this correct? should it not resolve both items?
    [Fact]
    public async Task LoadAndResolve_MultiTfm_MultipleChangedFiles_FindsAllCorrectly()
    {
        var projectA = CreateProject("ProjectA");
        projectA.Create();
        projectA.SetTargetFrameworks("net8.0;net10.0");
        var (fileA, _) = projectA.AddFileToProject("ClassA.cs", ProjectItemType.Compile, false, "class A {}");
        var (fileB, _) = projectA.AddFileToProject("ClassB.cs", ProjectItemType.Compile, false, "class B {}");

        var slnPath = CreateSolution(projectA);

        var changed = new[]
        {
            Core.FilePath.FromString(fileA),
            Core.FilePath.FromString(fileB),
        };
        var affected = await LoadAndResolveAffectedAsync(slnPath, changed);

        affected.AffectedProjects.Count.ShouldBe(1);
        affected.AffectedProjects.Values.Single().Name.ShouldBe("ProjectA");
    }

    [Fact]
    public async Task LoadAndResolve_DoesNotIncludeForwardDependencies()
    {
        // A references B: change in A, without closure, should only include A
        var projectB = CreateProject("ProjectB");
        projectB.Create();

        var projectA = CreateProject("ProjectA");
        projectA.Create();
        projectA.AddProjectReference(projectB);
        var (fileInA, _) = projectA.AddFileToProject("ClassA.cs", ProjectItemType.Compile, false, "class A {}");

        var slnPath = CreateSolution(projectA, projectB);

        var changed = new[] { Core.FilePath.FromString(fileInA) };
        var affected = await LoadAndResolveAffectedAsync(slnPath, changed);

        affected.AffectedProjects.Count.ShouldBe(1);
        affected.AffectedProjects.Values.Single().Name.ShouldBe("ProjectA");
    }

    [Fact]
    public void Load_WithCancelledToken_ThrowsOperationCanceled()
    {
        var loader = CreateLoader();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        Should.Throw<OperationCanceledException>(
            () => loader.Load([], [], cts.Token));
    }

    private TempCsProj CreateProject(string name)
    {
        var proj = new TempCsProj(Path.Combine(_testDir, name), name);
        _disposables.Add(proj);
        return proj;
    }

    private SolutionPath CreateSolution(params TempCsProj[] projects) =>
        SolutionPath.FromString(TempSlnx.Create(_testDir, projects));

    private static MsBuildProjectGraphLoader CreateLoader() =>
        new(NullLoggerFactory.Instance);

    private static async Task<AffectedProjectsResult> LoadAndResolveAffectedAsync(
        SolutionPath slnPath, IReadOnlyList<Core.FilePath> changed)
    {
        var (_, affected) = await LoadAndResolveWithGraphAsync(slnPath, changed);
        return affected;
    }

    private static async Task<(ProjectGraph Graph, AffectedProjectsResult Affected)> LoadAndResolveWithGraphAsync(
        SolutionPath slnPath, IReadOnlyList<Core.FilePath> changed)
    {
        var reader = new MsBuildSolutionReader();
        var projects = await reader.GetProjectsAsync(slnPath, CancellationToken.None);
        var loader = CreateLoader();
        var graph = loader.Load(projects, changed, CancellationToken.None);
        var affected = AffectedProjectsResolver.Resolve(graph.Projects, graph.ReverseDependencyMap, changed);
        return (graph, affected);
    }
}
