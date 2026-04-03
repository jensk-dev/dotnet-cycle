using Cycle.Core;

namespace Cycle.Infrastructure;

public interface IOutputStreamFactory
{
    Stream Create(FilePath path);
}
