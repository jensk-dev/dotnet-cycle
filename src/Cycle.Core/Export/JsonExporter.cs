using System.Text.Json;
using System.Text.Json.Serialization;

namespace Cycle.Core.Export;

public sealed class JsonExporter : IProjectExporter
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public async Task ExportAsync(IReadOnlyList<ProjectInfo> projects, TextWriter output, CancellationToken ct)
    {
        var exported = projects.Select(p => new ExportedProject
        {
            Name = p.Name,
            FilePath = p.FilePath.FullPath,
            Type = p.Type.ToString().ToLowerInvariant(),
            Properties = p.Properties,
        }).ToList();

        var json = JsonSerializer.Serialize(exported, SerializerOptions);
        await output.WriteLineAsync(json.AsMemory(), ct);
    }

    private sealed class ExportedProject
    {
        [JsonInclude]
        public required string Name { get; init; }

        [JsonInclude]
        public required string FilePath { get; init; }

        [JsonInclude]
        public required string Type { get; init; }

        [JsonInclude]
        public IReadOnlyDictionary<string, string>? Properties { get; init; }
    }
}
