using System.Text.Json;
using System.Text.Json.Serialization;

namespace Cycle.Core.Export;

public sealed partial class JsonExporter : IProjectExporter
{
    public async Task ExportAsync(IReadOnlyList<ProjectInfo> projects, TextWriter output, CancellationToken ct)
    {
        var exported = projects.Select(p => new ExportedProject
        {
            Name = p.Name,
            FilePath = p.FilePath.FullPath,
            Type = p.Type.ToString().ToLowerInvariant(),
            Properties = p.Properties,
        }).ToList();

        var json = JsonSerializer.Serialize(exported, ExporterJsonContext.Default.ListExportedProject);
        await output.WriteLineAsync(json.AsMemory(), ct);
    }

    private sealed class ExportedProject
    {
        public required string Name { get; init; }

        public required string FilePath { get; init; }

        public required string Type { get; init; }

        public IReadOnlyDictionary<string, string>? Properties { get; init; }
    }

    [JsonSerializable(typeof(List<ExportedProject>))]
    [JsonSourceGenerationOptions(
        WriteIndented = true,
        PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
    private sealed partial class ExporterJsonContext : JsonSerializerContext;
}
