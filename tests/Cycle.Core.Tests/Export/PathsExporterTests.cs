using Cycle.Core.Export;

namespace Cycle.Core.Tests.Export;

[TestFixture]
public class PathsExporterTests
{
    private PathsExporter _exporter = null!;
    private StringWriter _output = null!;

    [SetUp]
    public void SetUp()
    {
        _exporter = new PathsExporter();
        _output = new StringWriter();
    }

    [TearDown]
    public void TearDown()
    {
        _output.Dispose();
    }

    [Test]
    public async Task ExportAsync_WritesOnePathPerLine()
    {
        var projects = CreateTestProjects();

        await _exporter.ExportAsync(projects, _output, CancellationToken.None);

        var lines = _output.ToString().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        lines.Length.ShouldBe(2);
    }

    [Test]
    public async Task ExportAsync_WritesFullPaths()
    {
        var projects = CreateTestProjects();

        await _exporter.ExportAsync(projects, _output, CancellationToken.None);

        var lines = _output.ToString().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        foreach (var line in lines)
        {
            Path.IsPathRooted(line).ShouldBeTrue();
        }
    }

    [Test]
    public async Task ExportAsync_WithEmptyList_WritesNothing()
    {
        await _exporter.ExportAsync([], _output, CancellationToken.None);

        _output.ToString().ShouldBeEmpty();
    }

    [Test]
    public async Task ExportAsync_RespectsCanncellation()
    {
        var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var projects = CreateTestProjects();

        await Should.ThrowAsync<OperationCanceledException>(
            () => _exporter.ExportAsync(projects, _output, cts.Token));
    }

    private static List<ProjectInfo> CreateTestProjects() =>
    [
        new("ProjectA", FilePath.FromString(Path.Combine(Path.GetTempPath(), "A.csproj")), ProjectType.CsProj),
        new("ProjectB", FilePath.FromString(Path.Combine(Path.GetTempPath(), "B.fsproj")), ProjectType.FsProj),
    ];
}
