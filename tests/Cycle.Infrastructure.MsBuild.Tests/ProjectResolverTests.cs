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
            d.Dispose();
        _disposables.Clear();

        if (Directory.Exists(_testDir))
            Directory.Delete(_testDir, true);
    }

    [Test]
    public async Task ResolveAffectedProjects_ChangeToFileInProject_ReturnsProject()
    {
        var projectA = CreateProject("ProjectA");
        projectA.Create();
        var (filePath, _) = projectA.AddFileToProject("Class1.cs", ProjectItemType.Compile, false, "class A {}");
        var slnPath = CreateSolution(projectA);

        var resolver = CreateResolver(slnPath);
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

        var resolver = CreateResolver(slnPath);
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

        var resolver = CreateResolver(slnPath);
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

        var resolver = CreateResolver(slnPath);
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

        var resolver = CreateResolver(slnPath);
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

        var resolver = CreateResolver(slnPath);
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

        var resolver = CreateResolver(slnPath);
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

        var resolver = CreateResolver(slnPath);
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

        var resolver = CreateResolver(slnPath);
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

        var resolver = CreateResolver(slnPath);

        var affected = await resolver.ResolveAffectedProjectsAsync(slnPath, [], CancellationToken.None);

        affected.Count.ShouldBe(0);
    }

    private TempCsProj CreateProject(string name)
    {
        var proj = new TempCsProj(Path.Combine(_testDir, name), name);
        _disposables.Add(proj);
        return proj;
    }

    private string CreateSolution(params TempCsProj[] projects)
    {
        var slnPath = Path.Combine(_testDir, "Test.slnx");
        var projectEntries = string.Join(Environment.NewLine,
            projects.Select(p =>
                $"    <Project Path=\"{Path.GetRelativePath(_testDir, p.ProjectFilePath)}\" />"));

        var content = $"""
                       <Solution>
                         <Folder Name="/src/">
                       {projectEntries}
                         </Folder>
                       </Solution>
                       """;

        File.WriteAllText(slnPath, content);
        return slnPath;
    }

    private static ProjectResolver CreateResolver(string slnPath)
    {
        var reader = new MsBuildSolutionReader();
        return new ProjectResolver(reader, NullLogger<ProjectResolver>.Instance);
    }
}
