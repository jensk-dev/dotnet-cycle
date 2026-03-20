using System.Diagnostics.CodeAnalysis;
using System.Security;

namespace Cycle.Core;

public readonly record struct FilePath
{
    private FilePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Path cannot be null or empty", nameof(path));

        try
        {
            FullPath = Path.GetFullPath(path);
            DirectoryName = Path.GetDirectoryName(FullPath)!;
            FileName = Path.GetFileName(FullPath);
            Extension = Path.GetExtension(FullPath);

            if (Path.EndsInDirectorySeparator(FullPath))
                throw new ArgumentException("Path must point to a file, not a directory", nameof(path));
        }
        catch (Exception ex) when (ex is ArgumentException
                                       or SecurityException
                                       or NotSupportedException
                                       or PathTooLongException)
        {
            throw new ArgumentException($"Invalid file path: {path}", nameof(path), ex);
        }
    }

    public string FullPath { get; }

    public string DirectoryName { get; }

    public string FileName { get; }

    public string Extension { get; }

    public bool Equals(FilePath other) => string.Equals(FullPath, other.FullPath, StringComparison.OrdinalIgnoreCase);

    public override string ToString() => FullPath;

    public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(FullPath);

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

    public static FilePath FromCombinedStrings(string basePath, string subPath) =>
        new(Path.Combine(basePath, subPath));
}
