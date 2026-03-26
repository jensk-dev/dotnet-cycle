using System.Text.Json;
using System.Text.Json.Serialization;

namespace Cycle.Core;

public static partial class SolutionFilterWriter
{
    public static async Task WriteAsync(SolutionFilter filter, TextWriter output, CancellationToken ct)
    {
        var dto = new SlnfRoot
        {
            Solution = new SlnfSolution
            {
                Path = filter.SolutionPath,
                Projects = filter.Projects,
            },
        };

        var json = JsonSerializer.Serialize(dto, SlnfJsonContext.Default.SlnfRoot);
        await output.WriteLineAsync(json.AsMemory(), ct);
    }

    internal sealed class SlnfRoot
    {
        public required SlnfSolution Solution { get; init; }
    }

    internal sealed class SlnfSolution
    {
        public required string Path { get; init; }

        public required IReadOnlyList<string> Projects { get; init; }
    }

    [JsonSerializable(typeof(SlnfRoot))]
    [JsonSourceGenerationOptions(
        WriteIndented = true,
        PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
    private sealed partial class SlnfJsonContext : JsonSerializerContext;
}
