using Cycle.Core;
using Microsoft.Extensions.Logging;

namespace Cycle.Infrastructure.MsBuild;

public sealed partial class PhantomFileManager(ILogger logger) : IDisposable
{
    private readonly HashSet<string> _createdFiles = new(FilePath.PathComparer);
    private bool _disposed;

    public void CreatePhantomFiles(IReadOnlyList<FilePath> changedFiles, IReadOnlySet<FilePath> projectFiles)
    {
        foreach (var file in changedFiles)
        {
            if (File.Exists(file.FullPath))
                continue;

            if (projectFiles.Contains(file))
            {
                LogSkippingPhantomFile(file.FullPath);
                continue;
            }

            if (_createdFiles.Contains(file.FullPath))
                continue;

            var dir = file.DirectoryName;
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            File.WriteAllText(file.FullPath, string.Empty);
            _createdFiles.Add(file.FullPath);
            LogCreatedPhantomFile(file.FullPath);
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        foreach (var path in _createdFiles)
        {
            if (!File.Exists(path))
                continue;

            try
            {
                File.Delete(path);
                LogDeletedPhantomFile(path);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                LogFailedToDeletePhantomFile(path, ex);
            }
        }

        _disposed = true;
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Skipping phantom file for project file {FilePath}")]
    private partial void LogSkippingPhantomFile(string filePath);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Created phantom file {FilePath}")]
    private partial void LogCreatedPhantomFile(string filePath);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Deleted phantom file {FilePath}")]
    private partial void LogDeletedPhantomFile(string filePath);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to delete phantom file {FilePath}")]
    private partial void LogFailedToDeletePhantomFile(string filePath, Exception ex);
}
