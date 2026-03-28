using System.Diagnostics.CodeAnalysis;

namespace Cycle.Core;

public readonly record struct SolutionPath
{
    private SolutionPath(FilePath filePath)
    {
        FilePath = filePath;
    }

    public FilePath FilePath { get; }

    public bool IsDefault => FilePath.IsDefault;

    public static SolutionPath FromString(string path) => new(FilePath.FromString(path));

    public static SolutionPath FromFilePath(FilePath filePath) => new(filePath);

    public static bool TryFromString(string path, [NotNullWhen(true)] out SolutionPath? solutionPath)
    {
        solutionPath = null;

        if (!FilePath.TryFromString(path, out var filePath))
        {
            return false;
        }

        solutionPath = new SolutionPath(filePath.Value);
        return true;
    }

    public static explicit operator string(SolutionPath path) => path.FilePath.FullPath;

    public static explicit operator SolutionPath(string path) => FromString(path);

    public override string ToString() => FilePath.FullPath;
}
