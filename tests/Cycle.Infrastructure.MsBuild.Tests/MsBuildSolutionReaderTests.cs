using Cycle.Core;
using Cycle.Infrastructure.MsBuild.Tests.Helpers;
using Cycle.Tests.Common;

namespace Cycle.Infrastructure.MsBuild.Tests;

public sealed class MsBuildSolutionReaderTests : IClassFixture<MsBuildFixture>, IDisposable
{
    private readonly string _testDir;
    private readonly MsBuildSolutionReader _reader;
    private readonly List<IDisposable> _disposables = [];

    public MsBuildSolutionReaderTests(MsBuildFixture _)
    {
        _testDir = Path.Combine(Path.GetTempPath(), $"slnreader-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testDir);
        _reader = new MsBuildSolutionReader();
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
    public async Task GetProjectsAsync_WithValidSlnx_ReturnsProjectInfos()
    {
        var projectA = CreateProject("ProjectA");
        projectA.Create();
        var projectB = CreateProject("ProjectB");
        projectB.Create();
        var slnPath = CreateSolution(projectA, projectB);

        var results = await _reader.GetProjectsAsync(SolutionPath.FromString(slnPath), CancellationToken.None);

        results.Count.ShouldBe(2);
        results.Select(r => r.Name).OrderBy(n => n).ShouldBe(["ProjectA", "ProjectB"]);
    }

    [Fact]
    public async Task GetProjectsAsync_WithEmptySolution_ReturnsEmptyList()
    {
        var slnPath = Path.Combine(_testDir, "Empty.slnx");
        File.WriteAllText(slnPath, "<Solution></Solution>");

        var results = await _reader.GetProjectsAsync(SolutionPath.FromString(slnPath), CancellationToken.None);

        results.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetProjectsAsync_ReturnsFullyQualifiedPaths()
    {
        var projectA = CreateProject("ProjectA");
        projectA.Create();
        var slnPath = CreateSolution(projectA);

        var results = await _reader.GetProjectsAsync(SolutionPath.FromString(slnPath), CancellationToken.None);

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
