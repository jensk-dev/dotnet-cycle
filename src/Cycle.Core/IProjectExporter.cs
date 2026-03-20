namespace Cycle.Core;

public interface IProjectExporter
{
    Task ExportAsync(IReadOnlyList<ProjectInfo> projects, TextWriter output, CancellationToken ct);
}
