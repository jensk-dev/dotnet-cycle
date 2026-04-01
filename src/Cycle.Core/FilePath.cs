using System.Diagnostics.CodeAnalysis;
using System.Security;

namespace Cycle.Core;

public readonly record struct FilePath
{
    private FilePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Path cannot be null or empty", nameof(path));
        }

        try
        {
            FullPath = Path.GetFullPath(path);
            DirectoryName = Path.GetDirectoryName(FullPath)!;
            FileName = Path.GetFileName(FullPath);
            Extension = Path.GetExtension(FullPath);
        }
        catch (Exception ex) when (ex is ArgumentException
                                       or SecurityException
                                       or NotSupportedException
                                       or PathTooLongException)
        {
            throw new ArgumentException($"Invalid file path: {path}", nameof(path), ex);
        }

        if (Path.EndsInDirectorySeparator(FullPath))
        {
            throw new ArgumentException("Path must point to a file, not a directory", nameof(path));
        }
    }

    public string FullPath { get; }

    public string DirectoryName { get; }

    public string FileName { get; }

    public string Extension { get; }

    public bool IsDefault => FullPath is null;

    public static StringComparer PathComparer { get; } =
        OperatingSystem.IsWindows()
            ? StringComparer.OrdinalIgnoreCase
            : StringComparer.Ordinal;

    public static StringComparison PathComparison { get; } =
        OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

    public bool Equals(FilePath other) => string.Equals(FullPath, other.FullPath, PathComparison);

    public override string ToString() => FullPath;

    public override int GetHashCode() => FullPath is null ? 0 : PathComparer.GetHashCode(FullPath);

    public static explicit operator string(FilePath path) => path.FullPath;

    public static explicit operator FilePath(string path) => new(path);

    public static bool TryFromString(string path, [NotNullWhen(true)] out FilePath? filePath)
    {
        filePath = null;

        try
        {
            filePath = new FilePath(path);
            return true;
        }
        catch (ArgumentException)
        {
            filePath = null;
            return false;
        }
    }

    public static bool TryFromCombinedStrings(
        string basePath,
        string subPath,
        [NotNullWhen(true)] out FilePath? filePath)
    {
        filePath = null;

        try
        {
            if (Path.IsPathRooted(subPath))
            {
                filePath = new FilePath(subPath);
                return true;
            }

            filePath = new FilePath(Path.Combine(basePath, subPath));
            return true;
        }
        catch (ArgumentException)
        {
            filePath = null;
            return false;
        }
    }

    public static FilePath FromString(string path) => new(path);

    public static FilePath FromCombinedStrings(string basePath, string subPath)
    {
        if (!TryFromCombinedStrings(basePath, subPath, out var filePath))
        {
            throw new ArgumentException($"Invalid combined path: {basePath} + {subPath}");
        }

        return filePath.Value;
    }
}
