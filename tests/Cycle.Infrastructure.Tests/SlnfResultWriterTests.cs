using System.Text;
using System.Text.Json;
using Cycle.Core;

namespace Cycle.Infrastructure.Tests;

public sealed class SlnfResultWriterTests : IDisposable
{
    public void Dispose() => _output.Dispose();

    private static readonly FilePath DummyOutputFile = FilePath.FromString(
        Path.Combine(Path.GetTempPath(), "dummy.slnf"));

    private readonly MemoryStream _output = new();

    [Fact]
    public async Task WriteAsync_SolutionInSameDir_PathIsFilename()
    {
        var repoDir = Path.Combine(Path.GetTempPath(), "repo");
        var solutionPath = SolutionPath.FromString(Path.Combine(repoDir, "MySolution.sln"));
        var sut = CreateWriter(solutionPath);

        await sut.WriteAsync([], DummyOutputFile, CancellationToken.None);

        GetSolutionPath().ShouldBe("MySolution.sln");
    }

    [Fact]
    public async Task WriteAsync_ProjectPaths_AreRelativeToSolutionDir()
    {
        var repoDir = Path.Combine(Path.GetTempPath(), "repo");
        var solutionPath = SolutionPath.FromString(Path.Combine(repoDir, "MySolution.sln"));
        var sut = CreateWriter(solutionPath);
        var projects = CreateProjects(
            Path.Combine(repoDir, "src", "A", "A.csproj"),
            Path.Combine(repoDir, "src", "B", "B.fsproj"));

        await sut.WriteAsync(projects, DummyOutputFile, CancellationToken.None);

        var paths = GetProjectPaths();
        paths.Count.ShouldBe(2);
        paths[0].ShouldBe("src/A/A.csproj");
        paths[1].ShouldBe("src/B/B.fsproj");
    }

    [Fact]
    public async Task WriteAsync_WithNoProjects_WritesEmptyArray()
    {
        var repoDir = Path.Combine(Path.GetTempPath(), "repo");
        var solutionPath = SolutionPath.FromString(Path.Combine(repoDir, "MySolution.sln"));
        var sut = CreateWriter(solutionPath);

        await sut.WriteAsync([], DummyOutputFile, CancellationToken.None);

        GetProjectPaths().ShouldBeEmpty();
    }

    [Fact]
    public async Task WriteAsync_ProjectPaths_AreSortedAlphabetically()
    {
        var repoDir = Path.Combine(Path.GetTempPath(), "repo");
        var solutionPath = SolutionPath.FromString(Path.Combine(repoDir, "MySolution.sln"));
        var sut = CreateWriter(solutionPath);
        var projects = CreateProjects(
            Path.Combine(repoDir, "src", "C", "C.csproj"),
            Path.Combine(repoDir, "src", "A", "A.csproj"),
            Path.Combine(repoDir, "src", "B", "B.csproj"));

        await sut.WriteAsync(projects, DummyOutputFile, CancellationToken.None);

        var paths = GetProjectPaths();
        paths.Count.ShouldBe(3);
        paths[0].ShouldBe("src/A/A.csproj");
        paths[1].ShouldBe("src/B/B.csproj");
        paths[2].ShouldBe("src/C/C.csproj");
    }

    [Fact]
    public async Task WriteAsync_ProjectPaths_UseForwardSlashes()
    {
        var repoDir = Path.Combine(Path.GetTempPath(), "repo");
        var solutionPath = SolutionPath.FromString(Path.Combine(repoDir, "MySolution.sln"));
        var sut = CreateWriter(solutionPath);
        var projects = CreateProjects(Path.Combine(repoDir, "src", "A", "A.csproj"));

        await sut.WriteAsync(projects, DummyOutputFile, CancellationToken.None);

        GetProjectPaths()[0].ShouldNotContain("\\");
    }

    [Fact]
    public async Task WriteAsync_OutputIsIndented()
    {
        var repoDir = Path.Combine(Path.GetTempPath(), "repo");
        var solutionPath = SolutionPath.FromString(Path.Combine(repoDir, "MySolution.sln"));
        var sut = CreateWriter(solutionPath);

        await sut.WriteAsync([], DummyOutputFile, CancellationToken.None);

        ReadOutput().ShouldContain(Environment.NewLine);
    }

    [Fact]
    public void Constructor_WithDefaultSolutionPath_ThrowsArgumentException()
    {
        Should.Throw<ArgumentException>(
            () => new SlnfResultWriter(default, new StubOutputStreamFactory(new MemoryStream())));
    }

    [Fact]
    public async Task WriteAsync_WithNullProjects_ThrowsArgumentNullException()
    {
        var solutionPath = SolutionPath.FromString(Path.Combine(Path.GetTempPath(), "Test.sln"));
        var sut = CreateWriter(solutionPath);

        await Should.ThrowAsync<ArgumentNullException>(
            () => sut.WriteAsync(null!, DummyOutputFile, CancellationToken.None));
    }

    [Fact]
    public async Task WriteAsync_OutputIsValidSlnfJson()
    {
        var repoDir = Path.Combine(Path.GetTempPath(), "repo");
        var solutionPath = SolutionPath.FromString(Path.Combine(repoDir, "MySolution.sln"));
        var sut = CreateWriter(solutionPath);
        var projects = CreateProjects(Path.Combine(repoDir, "src", "A", "A.csproj"));

        await sut.WriteAsync(projects, DummyOutputFile, CancellationToken.None);

        var doc = JsonDocument.Parse(ReadOutput().Trim());
        doc.RootElement.TryGetProperty("solution", out var solution).ShouldBeTrue();
        solution.TryGetProperty("path", out _).ShouldBeTrue();
        solution.TryGetProperty("projects", out _).ShouldBeTrue();
    }

    private SlnfResultWriter CreateWriter(SolutionPath solutionPath) =>
        new(solutionPath, new StubOutputStreamFactory(_output));

    private static List<ProjectInfo> CreateProjects(params string[] paths) =>
        paths.Select(p => new ProjectInfo(
            Path.GetFileNameWithoutExtension(p),
            FilePath.FromString(p))).ToList();

    private string ReadOutput() => Encoding.UTF8.GetString(_output.ToArray());

    private string GetSolutionPath()
    {
        var doc = JsonDocument.Parse(ReadOutput().Trim());
        return doc.RootElement.GetProperty("solution").GetProperty("path").GetString()!;
    }

    private List<string> GetProjectPaths()
    {
        var doc = JsonDocument.Parse(ReadOutput().Trim());
        return doc.RootElement.GetProperty("solution").GetProperty("projects")
            .EnumerateArray().Select(e => e.GetString()!).ToList();
    }

    private sealed class StubOutputStreamFactory(Stream stream) : IOutputStreamFactory
    {
        public Stream Create(FilePath path) => stream;
    }
}
