using System.Text;
using Cycle.Core;

namespace Cycle.Infrastructure.Tests;

public sealed class TxtResultWriterTests : IDisposable
{
    public void Dispose() => _output.Dispose();

    private static readonly FilePath DummyOutputFile = FilePath.FromString(
        Path.Combine(Path.GetTempPath(), "dummy.txt"));

    private readonly MemoryStream _output = new();
    private readonly TxtResultWriter _sut;

    public TxtResultWriterTests()
    {
        _sut = new TxtResultWriter(new StubOutputStreamFactory(_output));
    }

    [Fact]
    public async Task WriteAsync_WritesOnePathPerLine()
    {
        var projects = CreateProjects("A", "B");

        await _sut.WriteAsync(projects, DummyOutputFile, CancellationToken.None);

        var lines = GetNonEmptyLines();
        lines.Length.ShouldBe(2);
    }

    [Fact]
    public async Task WriteAsync_WritesAbsolutePaths()
    {
        var projects = CreateProjects("A");

        await _sut.WriteAsync(projects, DummyOutputFile, CancellationToken.None);

        var lines = GetNonEmptyLines();
        Path.IsPathRooted(lines[0]).ShouldBeTrue();
    }

    [Fact]
    public async Task WriteAsync_SortsCaseInsensitively()
    {
        var basePath = Path.GetTempPath();
        var projects = new List<ProjectInfo>
        {
            new("C", FilePath.FromString(Path.Combine(basePath, "c", "C.csproj"))),
            new("A", FilePath.FromString(Path.Combine(basePath, "a", "A.csproj"))),
            new("B", FilePath.FromString(Path.Combine(basePath, "b", "B.csproj"))),
        };

        await _sut.WriteAsync(projects, DummyOutputFile, CancellationToken.None);

        var lines = GetNonEmptyLines();
        lines.Length.ShouldBe(3);
        lines[0].ShouldContain(Path.Combine("a", "A.csproj"));
        lines[1].ShouldContain(Path.Combine("b", "B.csproj"));
        lines[2].ShouldContain(Path.Combine("c", "C.csproj"));
    }

    [Fact]
    public async Task WriteAsync_WithEmptyProjects_WritesNothing()
    {
        await _sut.WriteAsync([], DummyOutputFile, CancellationToken.None);

        ReadOutput().ShouldBeEmpty();
    }

    [Fact]
    public async Task WriteAsync_DoesNotIncludeSolutionPath()
    {
        var projects = CreateProjects("A");

        await _sut.WriteAsync(projects, DummyOutputFile, CancellationToken.None);

        ReadOutput().ShouldNotContain("Test.sln");
    }

    [Fact]
    public async Task WriteAsync_WithNullProjects_ThrowsArgumentNullException()
    {
        await Should.ThrowAsync<ArgumentNullException>(
            () => _sut.WriteAsync(null!, DummyOutputFile, CancellationToken.None));
    }

    private static List<ProjectInfo> CreateProjects(params string[] names) =>
        names.Select(n => new ProjectInfo(n,
            FilePath.FromString(Path.Combine(Path.GetTempPath(), n, $"{n}.csproj")))).ToList();

    private string ReadOutput() => Encoding.UTF8.GetString(_output.ToArray());

    private string[] GetNonEmptyLines() =>
        ReadOutput().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);

    private sealed class StubOutputStreamFactory(Stream stream) : IOutputStreamFactory
    {
        public Stream Create(FilePath path) => stream;
    }
}
