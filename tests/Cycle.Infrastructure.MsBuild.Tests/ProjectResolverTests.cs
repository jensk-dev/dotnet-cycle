using Cycle.Infrastructure.MsBuild.Tests.Helpers;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cycle.Infrastructure.MsBuild.Tests;

[TestFixture]
public class ProjectResolverTests
{
    private string _testDir = null!;
    private readonly List<IDisposable> _disposables = [];

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        MsBuildBootstrap.Initialize();
    }

    [SetUp]
    public void SetUp()
    {
        _testDir = Path.Combine(Path.GetTempPath(), $"cycle-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testDir);
    }

    [TearDown]
    public void TearDown()
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

    [Test]
    public async Task ResolveAffectedProjects_ChangeToFileInProject_ReturnsProject()
    {
        var projectA = CreateProject("ProjectA");
        projectA.Create();
        var (filePath, _) = projectA.AddFileToProject("Class1.cs", ProjectItemType.Compile, false, "class A {}");
        var slnPath = CreateSolution(projectA);

        var resolver = CreateResolver();
        var changed = new[] { Core.FilePath.FromString(filePath) };

        var affected = await resolver.ResolveAffectedProjectsAsync(slnPath, changed, CancellationToken.None);

        affected.Count.ShouldBe(1);
        affected[0].Name.ShouldBe("ProjectA");
    }

    [Test]
    public async Task ResolveAffectedProjects_ChangeToProjectFile_ReturnsProject()
    {
        var projectA = CreateProject("ProjectA");
        projectA.Create();
        var slnPath = CreateSolution(projectA);

        var resolver = CreateResolver();
        var changed = new[] { Core.FilePath.FromString(projectA.ProjectFilePath) };

        var affected = await resolver.ResolveAffectedProjectsAsync(slnPath, changed, CancellationToken.None);

        affected.Count.ShouldBe(1);
        affected[0].Name.ShouldBe("ProjectA");
    }

    [Test]
    public async Task ResolveAffectedProjects_ChangeToUnrelatedFile_ReturnsEmpty()
    {
        var projectA = CreateProject("ProjectA");
        projectA.Create();
        projectA.AddFileToProject("Class1.cs", ProjectItemType.Compile, false, "class A {}");
        var slnPath = CreateSolution(projectA);

        // Create a file that is not in any project
        var unrelatedFile = Path.Combine(_testDir, "unrelated.txt");
        File.WriteAllText(unrelatedFile, "unrelated");

        var resolver = CreateResolver();
        var changed = new[] { Core.FilePath.FromString(unrelatedFile) };

        var affected = await resolver.ResolveAffectedProjectsAsync(slnPath, changed, CancellationToken.None);

        affected.Count.ShouldBe(0);
    }

    [Test]
    public async Task ResolveAffectedProjects_TransitiveDependency_ReturnsAllAffected()
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

        var resolver = CreateResolver();
        var changed = new[] { Core.FilePath.FromString(fileInC) };

        var affected = await resolver.ResolveAffectedProjectsAsync(slnPath, changed, CancellationToken.None);

        affected.Count.ShouldBe(3);
        var names = affected.Select(p => p.Name).OrderBy(n => n).ToList();
        names.ShouldBe(["ProjectA", "ProjectB", "ProjectC"]);
    }

    [Test]
    public async Task ResolveAffectedProjects_ChangeInDependency_AffectsDependents()
    {
        // B references A: change in A affects both A and B
        var projectA = CreateProject("ProjectA");
        projectA.Create();
        var (fileInA, _) = projectA.AddFileToProject("ClassA.cs", ProjectItemType.Compile, false, "class A {}");

        var projectB = CreateProject("ProjectB");
        projectB.Create();
        projectB.AddProjectReference(projectA);

        var slnPath = CreateSolution(projectA, projectB);

        var resolver = CreateResolver();
        var changed = new[] { Core.FilePath.FromString(fileInA) };

        var affected = await resolver.ResolveAffectedProjectsAsync(slnPath, changed, CancellationToken.None);

        affected.Count.ShouldBe(2);
        var names = affected.Select(p => p.Name).OrderBy(n => n).ToList();
        names.ShouldBe(["ProjectA", "ProjectB"]);
    }

    [Test]
    public async Task ResolveAffectedProjects_ContentFile_ReturnsProject()
    {
        var projectA = CreateProject("ProjectA");
        projectA.Create();
        var (filePath, _) = projectA.AddFileToProject("data.json", ProjectItemType.Content, false, "{}");
        var slnPath = CreateSolution(projectA);

        var resolver = CreateResolver();
        var changed = new[] { Core.FilePath.FromString(filePath) };

        var affected = await resolver.ResolveAffectedProjectsAsync(slnPath, changed, CancellationToken.None);

        affected.Count.ShouldBe(1);
        affected[0].Name.ShouldBe("ProjectA");
    }

    [Test]
    public async Task ResolveAffectedProjects_EmbeddedResource_ReturnsProject()
    {
        var projectA = CreateProject("ProjectA");
        projectA.Create();
        var (filePath, _) = projectA.AddFileToProject("resource.resx", ProjectItemType.EmbeddedResource, false, "<root/>");
        var slnPath = CreateSolution(projectA);

        var resolver = CreateResolver();
        var changed = new[] { Core.FilePath.FromString(filePath) };

        var affected = await resolver.ResolveAffectedProjectsAsync(slnPath, changed, CancellationToken.None);

        affected.Count.ShouldBe(1);
    }

    [Test]
    public async Task ResolveAffectedProjects_DeletedFile_HandledViaPhantomFiles()
    {
        var projectA = CreateProject("ProjectA");
        projectA.Create();
        var (filePath, _) = projectA.AddFileToProject("ToDelete.cs", ProjectItemType.Compile, false, "class D {}");
        var slnPath = CreateSolution(projectA);

        // Delete the file to simulate a deleted changed file
        File.Delete(filePath);

        var resolver = CreateResolver();
        var changed = new[] { Core.FilePath.FromString(filePath) };

        var affected = await resolver.ResolveAffectedProjectsAsync(slnPath, changed, CancellationToken.None);

        affected.Count.ShouldBe(1);
        affected[0].Name.ShouldBe("ProjectA");

        // Phantom file should be cleaned up
        File.Exists(filePath).ShouldBeFalse();
    }

    [Test]
    public async Task ResolveAffectedProjects_MultipleChangedFiles_ReturnsUnion()
    {
        var projectA = CreateProject("ProjectA");
        projectA.Create();
        var (fileA, _) = projectA.AddFileToProject("A.cs", ProjectItemType.Compile, false, "class A {}");

        var projectB = CreateProject("ProjectB");
        projectB.Create();
        var (fileB, _) = projectB.AddFileToProject("B.cs", ProjectItemType.Compile, false, "class B {}");

        var slnPath = CreateSolution(projectA, projectB);

        var resolver = CreateResolver();
        var changed = new[]
        {
            Core.FilePath.FromString(fileA),
            Core.FilePath.FromString(fileB),
        };

        var affected = await resolver.ResolveAffectedProjectsAsync(slnPath, changed, CancellationToken.None);

        affected.Count.ShouldBe(2);
        var names = affected.Select(p => p.Name).OrderBy(n => n).ToList();
        names.ShouldBe(["ProjectA", "ProjectB"]);
    }

    [Test]
    public async Task ResolveAffectedProjects_NoChangedFiles_ReturnsEmpty()
    {
        var projectA = CreateProject("ProjectA");
        projectA.Create();
        projectA.AddFileToProject("A.cs", ProjectItemType.Compile, false, "class A {}");
        var slnPath = CreateSolution(projectA);

        var resolver = CreateResolver();

        var affected = await resolver.ResolveAffectedProjectsAsync(slnPath, [], CancellationToken.None);

        affected.Count.ShouldBe(0);
    }

    [Test]
    public async Task ResolveAffectedProjects_ImportFileChanged_ReturnsProject()
    {
        var projectA = CreateProject("ProjectA");
        projectA.Create();
        projectA.AddFileToProject("Class1.cs", ProjectItemType.Compile, false, "class A {}");

        // Create a .props file and import it
        var propsPath = Path.Combine(projectA.ProjectDirectory, "Common.props");
        File.WriteAllText(propsPath, "<Project></Project>");
        projectA.AddImport("Common.props");

        var slnPath = CreateSolution(projectA);

        var resolver = CreateResolver();
        var changed = new[] { Core.FilePath.FromString(propsPath) };

        var affected = await resolver.ResolveAffectedProjectsAsync(slnPath, changed, CancellationToken.None);

        affected.Count.ShouldBe(1);
        affected[0].Name.ShouldBe("ProjectA");
    }

    [Test]
    public async Task ResolveAffectedProjects_TargetsFileChanged_ReturnsProject()
    {
        var projectA = CreateProject("ProjectA");
        projectA.Create();

        var targetsPath = Path.Combine(projectA.ProjectDirectory, "Build.targets");
        File.WriteAllText(targetsPath, "<Project></Project>");
        projectA.AddImport("Build.targets");

        var slnPath = CreateSolution(projectA);

        var resolver = CreateResolver();
        var changed = new[] { Core.FilePath.FromString(targetsPath) };

        var affected = await resolver.ResolveAffectedProjectsAsync(slnPath, changed, CancellationToken.None);

        affected.Count.ShouldBe(1);
        affected[0].Name.ShouldBe("ProjectA");
    }

    [Test]
    public async Task ResolveAffectedProjects_MultiTargetFramework_FileInProject_ReturnsProject()
    {
        var projectA = CreateProject("ProjectA");
        projectA.Create();
        projectA.SetTargetFrameworks("net8.0;net10.0");
        var (filePath, _) = projectA.AddFileToProject("Class1.cs", ProjectItemType.Compile, false, "class A {}");

        var slnPath = CreateSolution(projectA);

        var resolver = CreateResolver();
        var changed = new[] { Core.FilePath.FromString(filePath) };

        var affected = await resolver.ResolveAffectedProjectsAsync(slnPath, changed, CancellationToken.None);

        affected.Count.ShouldBe(1);
        affected[0].Name.ShouldBe("ProjectA");
    }

    [Test]
    public async Task ResolveAffectedProjects_InvalidProjectLoadFailure_TreatedAsAffected()
    {
        var projectA = CreateProject("ProjectA");
        projectA.CreateWithContent("this is not valid xml");

        var slnPath = CreateSolution(projectA);

        var resolver = CreateResolver();
        var affected = await resolver.ResolveAffectedProjectsAsync(slnPath, [], CancellationToken.None);

        // Projects that fail to load are always included
        affected.Count.ShouldBe(1);
        affected[0].Name.ShouldBe("ProjectA");
    }

    [Test]
    public async Task ResolveAffectedProjects_InvalidProjectReference_DoesNotThrow()
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

        var resolver = CreateResolver();
        var changed = new[] { Core.FilePath.FromString(fileInA) };

        // Should not throw, and should still find ProjectA as affected
        var affected = await resolver.ResolveAffectedProjectsAsync(slnPath, changed, CancellationToken.None);
        affected.Count.ShouldBe(1);
        affected[0].Name.ShouldBe("ProjectA");
    }

    // todo: is this correct? should it not resolve both items?
    [Test]
    public async Task ResolveAffectedProjects_MultiTfm_MultipleChangedFiles_FindsAllCorrectly()
    {
        var projectA = CreateProject("ProjectA");
        projectA.Create();
        projectA.SetTargetFrameworks("net8.0;net10.0");
        var (fileA, _) = projectA.AddFileToProject("ClassA.cs", ProjectItemType.Compile, false, "class A {}");
        var (fileB, _) = projectA.AddFileToProject("ClassB.cs", ProjectItemType.Compile, false, "class B {}");

        var slnPath = CreateSolution(projectA);

        var resolver = CreateResolver();
        var changed = new[]
        {
            Core.FilePath.FromString(fileA),
            Core.FilePath.FromString(fileB),
        };

        var affected = await resolver.ResolveAffectedProjectsAsync(slnPath, changed, CancellationToken.None);

        affected.Count.ShouldBe(1);
        affected[0].Name.ShouldBe("ProjectA");
    }

    [Test]
    public void ResolveAffectedProjects_WithCancelledToken_ThrowsOperationCanceled()
    {
        var projectA = CreateProject("ProjectA");
        projectA.Create();
        var slnPath = CreateSolution(projectA);

        var resolver = CreateResolver();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        Should.ThrowAsync<OperationCanceledException>(
            () => resolver.ResolveAffectedProjectsAsync(slnPath, [], cts.Token));
    }

    private TempCsProj CreateProject(string name)
    {
        var proj = new TempCsProj(Path.Combine(_testDir, name), name);
        _disposables.Add(proj);
        return proj;
    }

    private string CreateSolution(params TempCsProj[] projects) =>
        TempSlnx.Create(_testDir, projects);

    private static ProjectResolver CreateResolver()
    {
        var reader = new MsBuildSolutionReader();
        return new ProjectResolver(reader, NullLoggerFactory.Instance);
    }
}
