using Cycle.Core;

namespace Cycle.Application;

public interface ISlnfInputReader
{
    Task<SlnfInput> ReadAsync(FilePath slnfPath, CancellationToken ct);
}
