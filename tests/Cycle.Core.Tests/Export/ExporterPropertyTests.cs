using System.Text.Json;
using System.Xml.Linq;
using Cycle.Core.Export;
using FsCheck;
using FsCheck.Fluent;

namespace Cycle.Core.Tests.Export;

[TestFixture]
public class ExporterPropertyTests
{
    private static Arbitrary<List<ProjectInfo>> ProjectInfoLists()
    {
        var projectGen =
            from name in Gen.Elements("Alpha", "Beta", "Gamma", "Delta", "Epsilon")
            from type in Gen.Elements(Enum.GetValues<ProjectType>())
            let ext = type.ToFileExtension()
            let path = FilePath.FromString(Path.Combine(Path.GetTempPath(), $"{name}{ext}"))
            select new ProjectInfo(name, path, type);

        var listGen = projectGen.ListOf().Select(l => l.ToList());
        return listGen.ToArbitrary();
    }

    [FsCheck.NUnit.Property]
    public Property JsonExporter_OutputParsesAsJsonArrayWithCorrectLength()
    {
        return Prop.ForAll(ProjectInfoLists(), projects =>
        {
            using var output = new StringWriter();
            var exporter = new JsonExporter();
            exporter.ExportAsync(projects, output, CancellationToken.None).GetAwaiter().GetResult();

            var json = output.ToString().Trim();
            var doc = JsonDocument.Parse(json);
            return doc.RootElement.ValueKind == JsonValueKind.Array
                && doc.RootElement.GetArrayLength() == projects.Count;
        });
    }

    [FsCheck.NUnit.Property]
    public Property PathsExporter_LineCountEqualsProjectCount()
    {
        return Prop.ForAll(ProjectInfoLists(), projects =>
        {
            using var output = new StringWriter();
            var exporter = new PathsExporter();
            exporter.ExportAsync(projects, output, CancellationToken.None).GetAwaiter().GetResult();

            var text = output.ToString();
            if (projects.Count == 0)
                return text.Length == 0;

            var lines = text.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
            return lines.Length == projects.Count;
        });
    }

    [FsCheck.NUnit.Property]
    public Property BuildExporter_OutputParsesAsValidXmlWithCorrectProjectReferenceCount()
    {
        return Prop.ForAll(ProjectInfoLists(), projects =>
        {
            using var output = new StringWriter();
            var exporter = new BuildExporter();
            exporter.ExportAsync(projects, output, CancellationToken.None).GetAwaiter().GetResult();

            var doc = XDocument.Parse(output.ToString());
            var projectReferences = doc.Descendants("ProjectReference").ToList();
            return projectReferences.Count == projects.Count;
        });
    }
}
