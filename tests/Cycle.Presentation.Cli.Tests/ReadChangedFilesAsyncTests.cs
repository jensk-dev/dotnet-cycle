namespace Cycle.Presentation.Cli.Tests;

[TestFixture]
public class ReadChangedFilesAsyncTests
{
    private string _testDir = null!;

    [SetUp]
    public void SetUp()
    {
        _testDir = Path.Combine(Path.GetTempPath(), $"cli-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testDir);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_testDir))
            Directory.Delete(_testDir, true);
    }

    [Test]
    public async Task WithFileContainingPaths_ReturnsParsedFilePaths()
    {
        var file1 = Path.Combine(_testDir, "a.cs");
        var file2 = Path.Combine(_testDir, "b.cs");
        File.WriteAllText(file1, "");
        File.WriteAllText(file2, "");

        var changedFilesPath = Path.Combine(_testDir, "changed.txt");
        await File.WriteAllLinesAsync(changedFilesPath, [file1, file2]);

        var result = await Program.ReadChangedFilesAsync(
            new FileInfo(changedFilesPath), TextReader.Null, false, CancellationToken.None);

        result.Count.ShouldBe(2);
    }

    [Test]
    public async Task WithFileContainingBlankLines_SkipsBlanks()
    {
        var file1 = Path.Combine(_testDir, "a.cs");
        File.WriteAllText(file1, "");

        var changedFilesPath = Path.Combine(_testDir, "changed.txt");
        await File.WriteAllLinesAsync(changedFilesPath, ["", file1, "  ", "", file1]);

        var result = await Program.ReadChangedFilesAsync(
            new FileInfo(changedFilesPath), TextReader.Null, false, CancellationToken.None);

        result.Count.ShouldBe(2);
        result.All(fp => fp.FullPath == Path.GetFullPath(file1)).ShouldBeTrue();
    }

    [Test]
    public async Task WithFileContainingInvalidPaths_SkipsInvalid()
    {
        var validFile = Path.Combine(_testDir, "valid.cs");
        File.WriteAllText(validFile, "");

        var changedFilesPath = Path.Combine(_testDir, "changed.txt");
        await File.WriteAllLinesAsync(changedFilesPath, [validFile, "", "   "]);

        var result = await Program.ReadChangedFilesAsync(
            new FileInfo(changedFilesPath), TextReader.Null, false, CancellationToken.None);

        result.Count.ShouldBe(1);
    }

    [Test]
    public async Task WithNullFileAndRedirectedStdin_ReadsFromStdin()
    {
        var file1 = Path.Combine(_testDir, "stdin.cs");
        var stdinContent = file1 + Environment.NewLine;
        using var stdinReader = new StringReader(stdinContent);

        var result = await Program.ReadChangedFilesAsync(
            null, stdinReader, true, CancellationToken.None);

        result.Count.ShouldBe(1);
        result[0].FullPath.ShouldBe(Path.GetFullPath(file1));
    }

    [Test]
    public async Task WithNullFileAndNoRedirection_ReturnsEmpty()
    {
        var result = await Program.ReadChangedFilesAsync(
            null, TextReader.Null, false, CancellationToken.None);

        result.ShouldBeEmpty();
    }

    [Test]
    public async Task WithEmptyFile_ReturnsEmpty()
    {
        var changedFilesPath = Path.Combine(_testDir, "empty.txt");
        File.WriteAllText(changedFilesPath, "");

        var result = await Program.ReadChangedFilesAsync(
            new FileInfo(changedFilesPath), TextReader.Null, false, CancellationToken.None);

        result.ShouldBeEmpty();
    }

    [Test]
    public void WithCancellation_ThrowsOperationCanceled()
    {
        var file1 = Path.Combine(_testDir, "a.cs");
        var changedFilesPath = Path.Combine(_testDir, "changed.txt");
        File.WriteAllText(changedFilesPath, file1);

        var cts = new CancellationTokenSource();
        cts.Cancel();

        Should.ThrowAsync<OperationCanceledException>(
            () => Program.ReadChangedFilesAsync(
                new FileInfo(changedFilesPath), TextReader.Null, false, cts.Token));
    }
}
