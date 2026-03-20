using System.Collections.Immutable;

namespace Cycle.Core;

public record ProjectInfo
{
    public ProjectInfo(
        string name,
        FilePath filePath,
        ProjectType type,
        ImmutableDictionary<string, string>? properties = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (filePath.IsDefault) throw new ArgumentException("FilePath cannot be default", nameof(filePath));
        if (!Enum.IsDefined(type)) throw new ArgumentOutOfRangeException(nameof(type), type, "Invalid project type");

        Name = name;
        FilePath = filePath;
        Type = type;
        Properties = properties;
    }

    public string Name { get; init; }
    public FilePath FilePath { get; init; }
    public ProjectType Type { get; init; }
    public ImmutableDictionary<string, string>? Properties { get; init; }
}
