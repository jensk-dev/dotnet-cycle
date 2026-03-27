namespace Cycle.Core;

public record ProjectInfo
{
    public ProjectInfo(string name, FilePath filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        if (filePath.IsDefault)
        {
            throw new ArgumentException("FilePath cannot be default", nameof(filePath));
        }

        Name = name;
        FilePath = filePath;
    }

    public string Name { get; }
    public FilePath FilePath { get; }
}
