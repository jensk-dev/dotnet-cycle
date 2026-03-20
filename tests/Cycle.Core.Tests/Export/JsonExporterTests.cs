using System.Collections.Immutable;
using System.Text.Json;
using Cycle.Core.Export;

namespace Cycle.Core.Tests.Export;

[TestFixture]
public class JsonExporterTests
{
    private JsonExporter _exporter = null!;
    private StringWriter _output = null!;

    [SetUp]
    public void SetUp()
    {
        _exporter = new JsonExporter();
        _output = new StringWriter();
    }

    [TearDown]
    public void TearDown()
    {
        _output.Dispose();
    }

    [Test]
    public async Task ExportAsync_WithProjects_WritesValidJson()
    {
        var projects = CreateTestProjects();

        await _exporter.ExportAsync(projects, _output, CancellationToken.None);

        var json = _output.ToString().Trim();
        var doc = JsonDocument.Parse(json);
        doc.RootElement.ValueKind.ShouldBe(JsonValueKind.Array);
        doc.RootElement.GetArrayLength().ShouldBe(2);
    }

    [Test]
    public async Task ExportAsync_IncludesExpectedFields()
    {
        var projects = CreateTestProjects();

        await _exporter.ExportAsync(projects, _output, CancellationToken.None);

        var json = _output.ToString().Trim();
        var doc = JsonDocument.Parse(json);
        var first = doc.RootElement[0];

        first.GetProperty("name").GetString().ShouldBe("ProjectA");
        first.GetProperty("filePath").GetString().ShouldNotBeNullOrWhiteSpace();
        first.GetProperty("type").GetString().ShouldBe("csproj");
    }

    [Test]
    public async Task ExportAsync_WithProperties_IncludesProperties()
    {
        var props = ImmutableDictionary.CreateRange(new[]
        {
            new KeyValuePair<string, string>("TargetFramework", "net10.0"),
        });
        var projects = new List<ProjectInfo>
        {
            new("ProjectA", FilePath.FromString(Path.Combine(Path.GetTempPath(), "A.csproj")), ProjectType.CsProj, props),
        };

        await _exporter.ExportAsync(projects, _output, CancellationToken.None);

        var json = _output.ToString().Trim();
        var doc = JsonDocument.Parse(json);
        var first = doc.RootElement[0];

        first.GetProperty("properties").GetProperty("TargetFramework").GetString().ShouldBe("net10.0");
    }

    [Test]
    public async Task ExportAsync_WithNullProperties_OmitsPropertiesField()
    {
        var projects = new List<ProjectInfo>
        {
            new("ProjectA", FilePath.FromString(Path.Combine(Path.GetTempPath(), "A.csproj")), ProjectType.CsProj),
        };

        await _exporter.ExportAsync(projects, _output, CancellationToken.None);

        var json = _output.ToString().Trim();
        var doc = JsonDocument.Parse(json);
        var first = doc.RootElement[0];

        first.TryGetProperty("properties", out _).ShouldBeFalse();
    }

    [Test]
    public async Task ExportAsync_WithEmptyList_WritesEmptyArray()
    {
        await _exporter.ExportAsync([], _output, CancellationToken.None);

        var json = _output.ToString().Trim();
        var doc = JsonDocument.Parse(json);
        doc.RootElement.GetArrayLength().ShouldBe(0);
    }

    private static List<ProjectInfo> CreateTestProjects() =>
    [
        new("ProjectA", FilePath.FromString(Path.Combine(Path.GetTempPath(), "A.csproj")), ProjectType.CsProj),
        new("ProjectB", FilePath.FromString(Path.Combine(Path.GetTempPath(), "B.fsproj")), ProjectType.FsProj),
    ];
}
