using System.Xml;

namespace Cycle.Core.Export;

public sealed class BuildExporter : IProjectExporter
{
    public async Task ExportAsync(IReadOnlyList<ProjectInfo> projects, TextWriter output, CancellationToken ct)
    {
        var settings = new XmlWriterSettings
        {
            Indent = true,
            Async = true,
            OmitXmlDeclaration = true,
        };

        await using var writer = XmlWriter.Create(output, settings);

        await writer.WriteStartElementAsync(null, "Project", null);
        await writer.WriteAttributeStringAsync(null, "Sdk", null, "Microsoft.Build.Traversal/4.1.0");

        await writer.WriteStartElementAsync(null, "ItemGroup", null);

        foreach (var project in projects)
        {
            ct.ThrowIfCancellationRequested();
            await writer.WriteStartElementAsync(null, "ProjectReference", null);
            await writer.WriteAttributeStringAsync(null, "Include", null, project.FilePath.FullPath);
            await writer.WriteEndElementAsync();
        }

        await writer.WriteEndElementAsync(); // ItemGroup
        await writer.WriteEndElementAsync(); // Project

        await writer.FlushAsync();
    }
}
