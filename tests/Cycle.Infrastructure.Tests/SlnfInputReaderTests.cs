using System.Text.Json;
using Cycle.Core;

namespace Cycle.Infrastructure.Tests;

public sealed class SlnfInputReaderTests : IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly string _testDir;

    public SlnfInputReaderTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), $"slnf-input-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDir))
        {
            Directory.Delete(_testDir, true);
        }
    }

    [Fact]
    public async Task ReadAsync_ResolvesParentSolutionPath()
    {
        var slnPath = Path.Combine(_testDir, "MySolution.sln");
        File.WriteAllText(slnPath, "");
        var slnfPath = WriteSlnf("MySolution.sln", []);

        var (parentSolution, _) = await SlnfInputReader.ReadAsync(
            FilePath.FromString(slnfPath), CancellationToken.None);

        parentSolution.FilePath.FullPath.ShouldBe(Path.GetFullPath(slnPath));
    }

    [Fact]
    public async Task ReadAsync_ResolvesProjectPathsRelativeToSolutionDir()
    {
        var solutionDir = Path.Combine(_testDir, "solutions");
        Directory.CreateDirectory(solutionDir);
        var slnPath = Path.Combine(solutionDir, "MySolution.sln");
        File.WriteAllText(slnPath, "");

        var projectPath = Path.Combine(solutionDir, "src", "A", "A.csproj");
        Directory.CreateDirectory(Path.GetDirectoryName(projectPath)!);
        File.WriteAllText(projectPath, "");

        var slnfPath = WriteSlnf("solutions/MySolution.sln", ["src/A/A.csproj"]);

        var (_, scope) = await SlnfInputReader.ReadAsync(
            FilePath.FromString(slnfPath), CancellationToken.None);

        scope.Count.ShouldBe(1);
        scope.ShouldContain(FilePath.FromString(projectPath));
    }

    [Fact]
    public async Task ReadAsync_WithEmptyProjects_ReturnsEmptyScope()
    {
        var slnPath = Path.Combine(_testDir, "MySolution.sln");
        File.WriteAllText(slnPath, "");
        var slnfPath = WriteSlnf("MySolution.sln", []);

        var (_, scope) = await SlnfInputReader.ReadAsync(
            FilePath.FromString(slnfPath), CancellationToken.None);

        scope.ShouldBeEmpty();
    }

    [Fact]
    public async Task ReadAsync_WithMultipleProjects_ReturnsAllInScope()
    {
        var slnPath = Path.Combine(_testDir, "MySolution.sln");
        File.WriteAllText(slnPath, "");

        var projectA = Path.Combine(_testDir, "src", "A", "A.csproj");
        var projectB = Path.Combine(_testDir, "src", "B", "B.csproj");
        Directory.CreateDirectory(Path.GetDirectoryName(projectA)!);
        Directory.CreateDirectory(Path.GetDirectoryName(projectB)!);
        File.WriteAllText(projectA, "");
        File.WriteAllText(projectB, "");

        var slnfPath = WriteSlnf("MySolution.sln", ["src/A/A.csproj", "src/B/B.csproj"]);

        var (_, scope) = await SlnfInputReader.ReadAsync(
            FilePath.FromString(slnfPath), CancellationToken.None);

        scope.Count.ShouldBe(2);
        scope.ShouldContain(FilePath.FromString(projectA));
        scope.ShouldContain(FilePath.FromString(projectB));
    }

    [Fact]
    public async Task ReadAsync_WithMalformedJson_Throws()
    {
        var slnfPath = Path.Combine(_testDir, "bad.slnf");
        File.WriteAllText(slnfPath, "not json");

        await Should.ThrowAsync<JsonException>(
            () => SlnfInputReader.ReadAsync(
                FilePath.FromString(slnfPath), CancellationToken.None));
    }

    [Fact]
    public async Task ReadAsync_WithMissingSolutionProperty_Throws()
    {
        var slnfPath = Path.Combine(_testDir, "missing.slnf");
        File.WriteAllText(slnfPath, """{"other": {}}""");

        await Should.ThrowAsync<KeyNotFoundException>(
            () => SlnfInputReader.ReadAsync(
                FilePath.FromString(slnfPath), CancellationToken.None));
    }

    private string WriteSlnf(string solutionPath, string[] projects)
    {
        var slnfPath = Path.Combine(_testDir, "filter.slnf");
        var json = JsonSerializer.Serialize(new
        {
            solution = new
            {
                path = solutionPath,
                projects,
            },
        }, JsonOptions);

        File.WriteAllText(slnfPath, json);
        return slnfPath;
    }
}
