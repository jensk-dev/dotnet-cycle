using Microsoft.Extensions.Logging.Abstractions;

namespace Cycle.Presentation.Cli.Tests;

public sealed class ReadChangedFilesAsyncTests : IDisposable
{
    private readonly string _testDir;

    public ReadChangedFilesAsyncTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), $"cli-test-{Guid.NewGuid():N}");
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
    public async Task WithFileContainingPaths_ReturnsParsedFilePaths()
    {
        var file1 = Path.Combine(_testDir, "a.cs");
        var file2 = Path.Combine(_testDir, "b.cs");
        File.WriteAllText(file1, "");
        File.WriteAllText(file2, "");

        var changedFilesPath = Path.Combine(_testDir, "changed.txt");
        await File.WriteAllLinesAsync(changedFilesPath, [file1, file2], TestContext.Current.CancellationToken);

        var result = await Program.ReadChangedFilesAsync(
            new FileInfo(changedFilesPath), TextReader.Null, false, NullLogger.Instance, TestContext.Current.CancellationToken);

        result.Count.ShouldBe(2);
    }

    [Fact]
    public async Task WithFileContainingBlankLines_SkipsBlanks()
    {
        var file1 = Path.Combine(_testDir, "a.cs");
        File.WriteAllText(file1, "");

        var changedFilesPath = Path.Combine(_testDir, "changed.txt");
        await File.WriteAllLinesAsync(changedFilesPath, ["", file1, "  ", "", file1], TestContext.Current.CancellationToken);

        var result = await Program.ReadChangedFilesAsync(
            new FileInfo(changedFilesPath), TextReader.Null, false, NullLogger.Instance, TestContext.Current.CancellationToken);

        result.Count.ShouldBe(2);
        result.All(fp => fp.FullPath == Path.GetFullPath(file1)).ShouldBeTrue();
    }

    [Fact]
    public async Task WithFileContainingInvalidPaths_SkipsInvalid()
    {
        var validFile = Path.Combine(_testDir, "valid.cs");
        File.WriteAllText(validFile, "");

        var changedFilesPath = Path.Combine(_testDir, "changed.txt");
        await File.WriteAllLinesAsync(changedFilesPath, [validFile, "", "   "], TestContext.Current.CancellationToken);

        var result = await Program.ReadChangedFilesAsync(
            new FileInfo(changedFilesPath), TextReader.Null, false, NullLogger.Instance, TestContext.Current.CancellationToken);

        result.Count.ShouldBe(1);
    }

    [Fact]
    public async Task WithNullFileAndRedirectedStdin_ReadsFromStdin()
    {
        var file1 = Path.Combine(_testDir, "stdin.cs");
        var stdinContent = file1 + Environment.NewLine;
        using var stdinReader = new StringReader(stdinContent);

        var result = await Program.ReadChangedFilesAsync(
            null, stdinReader, true, NullLogger.Instance, TestContext.Current.CancellationToken);

        result.Count.ShouldBe(1);
        result[0].FullPath.ShouldBe(Path.GetFullPath(file1));
    }

    [Fact]
    public async Task WithNullFileAndNoRedirection_ReturnsEmpty()
    {
        var result = await Program.ReadChangedFilesAsync(
            null, TextReader.Null, false, NullLogger.Instance, TestContext.Current.CancellationToken);

        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task WithEmptyFile_ReturnsEmpty()
    {
        var changedFilesPath = Path.Combine(_testDir, "empty.txt");
        File.WriteAllText(changedFilesPath, "");

        var result = await Program.ReadChangedFilesAsync(
            new FileInfo(changedFilesPath), TextReader.Null, false, NullLogger.Instance, TestContext.Current.CancellationToken);

        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task WithCancellation_ThrowsOperationCanceled()
    {
        var file1 = Path.Combine(_testDir, "a.cs");
        var changedFilesPath = Path.Combine(_testDir, "changed.txt");
        File.WriteAllText(changedFilesPath, file1);

        var cts = new CancellationTokenSource();
        cts.Cancel();

        await Should.ThrowAsync<OperationCanceledException>(
            () => Program.ReadChangedFilesAsync(
                new FileInfo(changedFilesPath), TextReader.Null, false, NullLogger.Instance, cts.Token));
    }
}
