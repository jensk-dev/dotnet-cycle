namespace Cycle.Core.Export;

public sealed class PathsExporter : IProjectExporter
{
    public async Task ExportAsync(IReadOnlyList<ProjectInfo> projects, TextWriter output, CancellationToken ct)
    {
        foreach (var project in projects)
        {
            ct.ThrowIfCancellationRequested();
            await output.WriteLineAsync(project.FilePath.FullPath.AsMemory(), ct);
        }
    }
}
