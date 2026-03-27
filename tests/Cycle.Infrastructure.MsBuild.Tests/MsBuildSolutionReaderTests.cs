using Cycle.Infrastructure.MsBuild.Tests.Helpers;

namespace Cycle.Infrastructure.MsBuild.Tests;

[TestFixture]
public class MsBuildSolutionReaderTests
{
    private string _testDir = null!;
    private MsBuildSolutionReader _reader = null!;
    private readonly List<IDisposable> _disposables = [];

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        MsBuildBootstrap.Initialize();
    }

    [SetUp]
    public void SetUp()
    {
        _testDir = Path.Combine(Path.GetTempPath(), $"slnreader-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testDir);
        _reader = new MsBuildSolutionReader();
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
    public void GetProjectsAsync_WithNullPath_ThrowsArgumentException()
    {
        Should.ThrowAsync<ArgumentException>(
            () => _reader.GetProjectsAsync(null!, CancellationToken.None));
    }

    [Test]
    public void GetProjectsAsync_WithEmptyPath_ThrowsArgumentException()
    {
        Should.ThrowAsync<ArgumentException>(
            () => _reader.GetProjectsAsync("", CancellationToken.None));
    }

    [Test]
    public void GetProjectsAsync_WithWhitespacePath_ThrowsArgumentException()
    {
        Should.ThrowAsync<ArgumentException>(
            () => _reader.GetProjectsAsync("   ", CancellationToken.None));
    }

    [Test]
    public void GetProjectsAsync_WithUnsupportedExtension_ThrowsArgumentException()
    {
        Should.ThrowAsync<ArgumentException>(
            () => _reader.GetProjectsAsync("something.txt", CancellationToken.None));
    }

    [Test]
    public async Task GetProjectsAsync_WithValidSlnx_ReturnsProjectInfos()
    {
        var projectA = CreateProject("ProjectA");
        projectA.Create();
        var projectB = CreateProject("ProjectB");
        projectB.Create();
        var slnPath = CreateSolution(projectA, projectB);

        var results = await _reader.GetProjectsAsync(slnPath, CancellationToken.None);

        results.Count.ShouldBe(2);
        results.Select(r => r.Name).OrderBy(n => n).ShouldBe(["ProjectA", "ProjectB"]);
    }

    [Test]
    public async Task GetProjectsAsync_WithEmptySolution_ReturnsEmptyList()
    {
        var slnPath = Path.Combine(_testDir, "Empty.slnx");
        File.WriteAllText(slnPath, "<Solution></Solution>");

        var results = await _reader.GetProjectsAsync(slnPath, CancellationToken.None);

        results.ShouldBeEmpty();
    }

    [Test]
    public async Task GetProjectsAsync_ReturnsFullyQualifiedPaths()
    {
        var projectA = CreateProject("ProjectA");
        projectA.Create();
        var slnPath = CreateSolution(projectA);

        var results = await _reader.GetProjectsAsync(slnPath, CancellationToken.None);

        results.Count.ShouldBe(1);
        Path.IsPathRooted(results[0].FilePath.FullPath).ShouldBeTrue();
    }

    private TempCsProj CreateProject(string name)
    {
        var proj = new TempCsProj(Path.Combine(_testDir, name), name);
        _disposables.Add(proj);
        return proj;
    }

    private string CreateSolution(params TempCsProj[] projects) =>
        TempSlnx.Create(_testDir, projects);
}
