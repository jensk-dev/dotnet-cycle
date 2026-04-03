using Cycle.Core;

namespace Cycle.Infrastructure.Tests;

public sealed class FileOutputStreamFactoryTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), $"stream-factory-test-{Guid.NewGuid():N}");
    private readonly FileOutputStreamFactory _sut = new();

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
    }

    [Fact]
    public void Create_ReturnsWritableStream()
    {
        var path = OutputFile("test.txt");

        using var stream = _sut.Create(path);

        stream.CanWrite.ShouldBeTrue();
    }

    [Fact]
    public void Create_WrittenDataIsPersisted()
    {
        var path = OutputFile("test.txt");
        var expected = "hello"u8.ToArray();

        using (var stream = _sut.Create(path))
        {
            stream.Write(expected);
        }

        File.ReadAllBytes(path.FullPath).ShouldBe(expected);
    }

    [Fact]
    public void Create_CreatesParentDirectory()
    {
        var path = FilePath.FromString(Path.Combine(_tempDir, "nested", "dir", "test.txt"));

        using var stream = _sut.Create(path);

        Directory.Exists(Path.Combine(_tempDir, "nested", "dir")).ShouldBeTrue();
    }

    [Fact]
    public void Create_ExistingDirectory_DoesNotThrow()
    {
        Directory.CreateDirectory(_tempDir);
        var path = OutputFile("test.txt");

        using var stream = _sut.Create(path);

        stream.CanWrite.ShouldBeTrue();
    }

    [Fact]
    public void Create_OverwritesExistingFile()
    {
        var path = OutputFile("test.txt");
        Directory.CreateDirectory(_tempDir);
        File.WriteAllText(path.FullPath, "old content");

        using (var stream = _sut.Create(path))
        {
            stream.Write("new"u8);
        }

        File.ReadAllText(path.FullPath).ShouldBe("new");
    }

    [Fact]
    public void Create_ReturnsAsyncCapableStream()
    {
        var path = OutputFile("test.txt");

        using var stream = _sut.Create(path);

        stream.ShouldBeOfType<FileStream>();
        ((FileStream)stream).IsAsync.ShouldBeTrue();
    }

    private FilePath OutputFile(string name) =>
        FilePath.FromString(Path.Combine(_tempDir, name));
}
