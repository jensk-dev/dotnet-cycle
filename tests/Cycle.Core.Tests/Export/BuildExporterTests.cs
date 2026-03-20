using System.Xml.Linq;
using Cycle.Core.Export;

namespace Cycle.Core.Tests.Export;

[TestFixture]
public class BuildExporterTests
{
    private BuildExporter _exporter = null!;
    private StringWriter _output = null!;

    [SetUp]
    public void SetUp()
    {
        _exporter = new BuildExporter();
        _output = new StringWriter();
    }

    [TearDown]
    public void TearDown()
    {
        _output.Dispose();
    }

    [Test]
    public async Task ExportAsync_WritesValidXml()
    {
        var projects = CreateTestProjects();

        await _exporter.ExportAsync(projects, _output, CancellationToken.None);

        var xml = _output.ToString();
        Should.NotThrow(() => XDocument.Parse(xml));
    }

    [Test]
    public async Task ExportAsync_UsesTraversalSdk()
    {
        var projects = CreateTestProjects();

        await _exporter.ExportAsync(projects, _output, CancellationToken.None);

        var doc = XDocument.Parse(_output.ToString());
        doc.Root!.Attribute("Sdk")!.Value.ShouldBe("Microsoft.Build.Traversal/4.1.0");
    }

    [Test]
    public async Task ExportAsync_AddsProjectReferences()
    {
        var projects = CreateTestProjects();

        await _exporter.ExportAsync(projects, _output, CancellationToken.None);

        var doc = XDocument.Parse(_output.ToString());
        var projectReferences = doc.Descendants("ProjectReference").ToList();
        projectReferences.Count.ShouldBe(2);
    }

    [Test]
    public async Task ExportAsync_ProjectReferencesHaveIncludeAttribute()
    {
        var projects = CreateTestProjects();

        await _exporter.ExportAsync(projects, _output, CancellationToken.None);

        var doc = XDocument.Parse(_output.ToString());
        var includes = doc.Descendants("ProjectReference")
            .Select(e => e.Attribute("Include")?.Value)
            .ToList();

        includes.ShouldAllBe(v => v != null && Path.IsPathRooted(v));
    }

    [Test]
    public async Task ExportAsync_WithEmptyList_WritesProjectWithEmptyItemGroup()
    {
        await _exporter.ExportAsync([], _output, CancellationToken.None);

        var doc = XDocument.Parse(_output.ToString());
        doc.Root!.Name.LocalName.ShouldBe("Project");
        doc.Descendants("ProjectReference").ShouldBeEmpty();
    }

    private static List<ProjectInfo> CreateTestProjects() =>
    [
        new("ProjectA", FilePath.FromString(Path.Combine(Path.GetTempPath(), "A.csproj")), ProjectType.CsProj),
        new("ProjectB", FilePath.FromString(Path.Combine(Path.GetTempPath(), "B.fsproj")), ProjectType.FsProj),
    ];
}
