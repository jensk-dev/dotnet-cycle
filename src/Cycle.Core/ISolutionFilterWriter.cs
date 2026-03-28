namespace Cycle.Core;

public interface ISolutionFilterWriter
{
    Task WriteAsync(SolutionFilter filter, TextWriter output, CancellationToken ct);
}
