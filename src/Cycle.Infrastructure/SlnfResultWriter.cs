using System.Text.Json;
using System.Text.Json.Serialization;
using Cycle.Application;
using Cycle.Core;

namespace Cycle.Infrastructure;

public sealed partial class SlnfResultWriter : IResultWriter
{
    private readonly SolutionPath _solutionPath;
    private readonly IOutputStreamFactory _streamFactory;

    public SlnfResultWriter(SolutionPath solutionPath, IOutputStreamFactory streamFactory)
    {
        if (solutionPath.IsDefault)
        {
            throw new ArgumentException("Solution path must not be default.", nameof(solutionPath));
        }

        ArgumentNullException.ThrowIfNull(streamFactory);
        _solutionPath = solutionPath;
        _streamFactory = streamFactory;
    }

    public async Task WriteAsync(
        IReadOnlyList<ProjectInfo> projects,
        FilePath outputFile,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(projects);

        var solutionDir = _solutionPath.FilePath.DirectoryName;
        var relativeSolutionPath = Path.GetRelativePath(solutionDir, _solutionPath.FilePath.FullPath)
            .Replace('\\', '/');

        var relativeProjectPaths = projects
            .Select(p => Path.GetRelativePath(solutionDir, p.FilePath.FullPath)
                .Replace('\\', '/'))
            .OrderBy(p => p, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var dto = new SlnfRoot
        {
            Solution = new SlnfSolution
            {
                Path = relativeSolutionPath,
                Projects = relativeProjectPaths,
            },
        };

        await using var output = _streamFactory.Create(outputFile);
        await JsonSerializer.SerializeAsync(output, dto, SlnfJsonContext.Default.SlnfRoot, ct);
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
