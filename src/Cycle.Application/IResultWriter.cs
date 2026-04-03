using Cycle.Core;

namespace Cycle.Application;

public interface IResultWriter
{
    Task WriteAsync(
        IReadOnlyList<ProjectInfo> projects,
        FilePath outputFile,
        CancellationToken ct);
}
