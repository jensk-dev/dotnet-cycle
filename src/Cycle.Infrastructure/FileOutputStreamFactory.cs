using Cycle.Core;

namespace Cycle.Infrastructure;

public sealed class FileOutputStreamFactory : IOutputStreamFactory
{
    public Stream Create(FilePath path)
    {
        var dir = path.DirectoryName;
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        return new FileStream(
            path.FullPath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            4096,
            useAsync: true);
    }
}
