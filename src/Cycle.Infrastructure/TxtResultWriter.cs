using Cycle.Application;
using Cycle.Core;

namespace Cycle.Infrastructure;

public sealed class TxtResultWriter : IResultWriter
{
    private readonly IOutputStreamFactory _streamFactory;

    public TxtResultWriter(IOutputStreamFactory streamFactory)
    {
        ArgumentNullException.ThrowIfNull(streamFactory);
        _streamFactory = streamFactory;
    }

    public async Task WriteAsync(
        IReadOnlyList<ProjectInfo> projects,
        FilePath outputFile,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(projects);

        await using var stream = _streamFactory.Create(outputFile);
        await using var writer = new StreamWriter(stream);

        foreach (var project in projects.OrderBy(p => p.FilePath.FullPath, StringComparer.OrdinalIgnoreCase))
        {
            await writer.WriteLineAsync(project.FilePath.FullPath.AsMemory(), ct);
        }
    }
}
