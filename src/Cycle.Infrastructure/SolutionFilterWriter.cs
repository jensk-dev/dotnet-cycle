using System.Text.Json;
using System.Text.Json.Serialization;
using Cycle.Application;

namespace Cycle.Infrastructure;

public sealed partial class SolutionFilterWriter : ISolutionFilterWriter
{
    public async Task WriteAsync(SolutionFilter filter, TextWriter output, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(filter);
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(filter.SolutionPath);
        ArgumentNullException.ThrowIfNull(filter.Projects);

        var dto = new SlnfRoot
        {
            Solution = new SlnfSolution
            {
                Path = filter.SolutionPath,
                Projects = filter.Projects,
            },
        };

        var json = JsonSerializer.Serialize(dto, SlnfJsonContext.Default.SlnfRoot);
        await output.WriteAsync(json.AsMemory(), ct);
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
