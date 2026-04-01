using Cycle.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cycle.Infrastructure.Tests;

public sealed class PhantomFileManagerTests : IDisposable
{
    private readonly string _testDir;

    public PhantomFileManagerTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), $"phantom-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDir))
        {
            Directory.Delete(_testDir, true);
        }
    }

    [Fact]
    public void CreatePhantomFiles_FileDoesNotExist_CreatesFile()
    {
        using var manager = new PhantomFileManager(NullLoggerFactory.Instance.CreateLogger<PhantomFileManager>());
        var filePath = FilePath.FromString(Path.Combine(_testDir, "phantom.cs"));

        manager.CreatePhantomFiles([filePath], new HashSet<FilePath>());

        File.Exists(filePath.FullPath).ShouldBeTrue();
        File.ReadAllText(filePath.FullPath).ShouldBeEmpty();
    }

    [Fact]
    public void CreatePhantomFiles_FileAlreadyExists_DoesNotOverwrite()
    {
        var fullPath = Path.Combine(_testDir, "existing.cs");
        File.WriteAllText(fullPath, "original content");
        using var manager = new PhantomFileManager(NullLoggerFactory.Instance.CreateLogger<PhantomFileManager>());
        var filePath = FilePath.FromString(fullPath);

        manager.CreatePhantomFiles([filePath], new HashSet<FilePath>());

        File.ReadAllText(fullPath).ShouldBe("original content");
    }

    [Fact]
    public void CreatePhantomFiles_FileIsProjectFile_SkipsCreation()
    {
        using var manager = new PhantomFileManager(NullLoggerFactory.Instance.CreateLogger<PhantomFileManager>());
        var filePath = FilePath.FromString(Path.Combine(_testDir, "Project.csproj"));
        var projectFiles = new HashSet<FilePath> { filePath };

        manager.CreatePhantomFiles([filePath], projectFiles);

        File.Exists(filePath.FullPath).ShouldBeFalse();
    }

    [Fact]
    public void CreatePhantomFiles_DuplicateFile_CreatesOnce()
    {
        using var manager = new PhantomFileManager(NullLoggerFactory.Instance.CreateLogger<PhantomFileManager>());
        var filePath = FilePath.FromString(Path.Combine(_testDir, "dup.cs"));

        manager.CreatePhantomFiles([filePath, filePath], new HashSet<FilePath>());

        File.Exists(filePath.FullPath).ShouldBeTrue();
    }

    [Fact]
    public void CreatePhantomFiles_DirectoryDoesNotExist_CreatesDirectory()
    {
        using var manager = new PhantomFileManager(NullLoggerFactory.Instance.CreateLogger<PhantomFileManager>());
        var filePath = FilePath.FromString(Path.Combine(_testDir, "subdir", "deep", "file.cs"));

        manager.CreatePhantomFiles([filePath], new HashSet<FilePath>());

        File.Exists(filePath.FullPath).ShouldBeTrue();
        Directory.Exists(Path.Combine(_testDir, "subdir", "deep")).ShouldBeTrue();
    }

    [Fact]
    public void Dispose_DeletesAllCreatedFiles()
    {
        var manager = new PhantomFileManager(NullLoggerFactory.Instance.CreateLogger<PhantomFileManager>());
        var file1 = FilePath.FromString(Path.Combine(_testDir, "a.cs"));
        var file2 = FilePath.FromString(Path.Combine(_testDir, "b.cs"));

        manager.CreatePhantomFiles([file1, file2], new HashSet<FilePath>());
        File.Exists(file1.FullPath).ShouldBeTrue();
        File.Exists(file2.FullPath).ShouldBeTrue();

        manager.Dispose();

        File.Exists(file1.FullPath).ShouldBeFalse();
        File.Exists(file2.FullPath).ShouldBeFalse();
    }

    [Fact]
    public void Dispose_CalledTwice_IsIdempotent()
    {
        var manager = new PhantomFileManager(NullLoggerFactory.Instance.CreateLogger<PhantomFileManager>());
        var filePath = FilePath.FromString(Path.Combine(_testDir, "idem.cs"));

        manager.CreatePhantomFiles([filePath], new HashSet<FilePath>());
        manager.Dispose();

        Should.NotThrow(() => manager.Dispose());
    }

    [Fact]
    public void Dispose_FileAlreadyDeleted_DoesNotThrow()
    {
        var manager = new PhantomFileManager(NullLoggerFactory.Instance.CreateLogger<PhantomFileManager>());
        var filePath = FilePath.FromString(Path.Combine(_testDir, "gone.cs"));

        manager.CreatePhantomFiles([filePath], new HashSet<FilePath>());
        File.Delete(filePath.FullPath);

        Should.NotThrow(() => manager.Dispose());
    }

    [Fact]
    public void Dispose_DeletesCreatedDirectories()
    {
        var manager = new PhantomFileManager(NullLoggerFactory.Instance.CreateLogger<PhantomFileManager>());
        var subDir = Path.Combine(_testDir, "newdir", "deep");
        var filePath = FilePath.FromString(Path.Combine(subDir, "phantom.cs"));

        manager.CreatePhantomFiles([filePath], new HashSet<FilePath>());
        Directory.Exists(subDir).ShouldBeTrue();

        manager.Dispose();

        File.Exists(filePath.FullPath).ShouldBeFalse();
        Directory.Exists(subDir).ShouldBeFalse();
    }

    [Fact]
    public void Dispose_ReadOnlyFile_LogsWarningAndContinues()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), "Windows only");

        var manager = new PhantomFileManager(NullLoggerFactory.Instance.CreateLogger<PhantomFileManager>());
        var filePath = FilePath.FromString(Path.Combine(_testDir, "readonly.cs"));

        manager.CreatePhantomFiles([filePath], new HashSet<FilePath>());
        File.SetAttributes(filePath.FullPath, FileAttributes.ReadOnly);

        try
        {
            Should.NotThrow(() => manager.Dispose());
        }
        finally
        {
            // Clean up read-only attribute so Dispose can delete the directory
            if (File.Exists(filePath.FullPath))
            {
                File.SetAttributes(filePath.FullPath, FileAttributes.Normal);
            }
        }
    }
}
