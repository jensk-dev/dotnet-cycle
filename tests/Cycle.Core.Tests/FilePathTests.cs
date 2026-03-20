namespace Cycle.Core.Tests;

[TestFixture]
public class FilePathTests
{
    [Test]
    public void FromString_WithValidPath_CreatesFilePath()
    {
        var path = FilePath.FromString(Path.GetTempFileName());

        path.FullPath.ShouldNotBeNullOrWhiteSpace();
        path.FileName.ShouldNotBeNullOrWhiteSpace();
        path.DirectoryName.ShouldNotBeNullOrWhiteSpace();
    }

    [Test]
    public void FromString_WithRelativePath_ResolvesToFullPath()
    {
        var path = FilePath.FromString("somefile.txt");

        Path.IsPathRooted(path.FullPath).ShouldBeTrue();
    }

    [Test]
    public void FromString_ExtractsComponents()
    {
        var tempFile = Path.GetTempFileName();
        var path = FilePath.FromString(tempFile);

        path.FileName.ShouldBe(Path.GetFileName(tempFile));
        path.Extension.ShouldBe(Path.GetExtension(tempFile));
        path.DirectoryName.ShouldBe(Path.GetDirectoryName(tempFile));
    }

    [Test]
    public void FromString_WithNullPath_ThrowsArgumentException()
    {
        Should.Throw<ArgumentException>(() => FilePath.FromString(null!));
    }

    [Test]
    public void FromString_WithEmptyPath_ThrowsArgumentException()
    {
        Should.Throw<ArgumentException>(() => FilePath.FromString(""));
    }

    [Test]
    public void FromString_WithWhitespacePath_ThrowsArgumentException()
    {
        Should.Throw<ArgumentException>(() => FilePath.FromString("   "));
    }

    [Test]
    public void FromString_WithDirectoryPath_ThrowsArgumentException()
    {
        var dir = Path.GetTempPath();
        Should.Throw<ArgumentException>(() => FilePath.FromString(dir));
    }

    [Test]
    [Platform(Include = "Win")]
    public void Equals_IsCaseInsensitive()
    {
        var lower = FilePath.FromString(Path.Combine(Path.GetTempPath(), "test.txt"));
        var upper = FilePath.FromString(Path.Combine(Path.GetTempPath(), "TEST.TXT"));

        lower.ShouldBe(upper);
    }

    [Test]
    [Platform(Include = "Linux")]
    public void Equals_IsCaseSensitive()
    {
        var lower = FilePath.FromString(Path.Combine(Path.GetTempPath(), "test.txt"));
        var upper = FilePath.FromString(Path.Combine(Path.GetTempPath(), "TEST.TXT"));

        lower.ShouldNotBe(upper);
    }

    [Test]
    [Platform(Include = "Win")]
    public void GetHashCode_IsCaseInsensitive()
    {
        var lower = FilePath.FromString(Path.Combine(Path.GetTempPath(), "test.txt"));
        var upper = FilePath.FromString(Path.Combine(Path.GetTempPath(), "TEST.TXT"));

        lower.GetHashCode().ShouldBe(upper.GetHashCode());
    }

    [Test]
    [Platform(Include = "Linux")]
    public void GetHashCode_IsCaseSensitive()
    {
        var lower = FilePath.FromString(Path.Combine(Path.GetTempPath(), "test.txt"));
        var upper = FilePath.FromString(Path.Combine(Path.GetTempPath(), "TEST.TXT"));

        lower.GetHashCode().ShouldNotBe(upper.GetHashCode());
    }

    [Test]
    public void TryFromString_WithValidPath_ReturnsTrue()
    {
        var result = FilePath.TryFromString("somefile.txt", out var filePath);

        result.ShouldBeTrue();
        filePath.ShouldNotBeNull();
    }

    [Test]
    public void TryFromString_WithEmptyPath_ReturnsFalse()
    {
        var result = FilePath.TryFromString("", out var filePath);

        result.ShouldBeFalse();
        filePath.ShouldBeNull();
    }

    [Test]
    public void TryFromCombinedStrings_WithRelativeSubPath_CombinesPaths()
    {
        var basePath = Path.GetTempPath();
        var result = FilePath.TryFromCombinedStrings(basePath, "subdir/file.cs", out var filePath);

        result.ShouldBeTrue();
        filePath!.Value.FullPath.ShouldContain("subdir");
        filePath.Value.FileName.ShouldBe("file.cs");
    }

    [Test]
    public void TryFromCombinedStrings_WithRootedSubPath_IgnoresBasePath()
    {
        var tempFile = Path.GetTempFileName();
        var result = FilePath.TryFromCombinedStrings("/some/other/base", tempFile, out var filePath);

        result.ShouldBeTrue();
        filePath!.Value.FullPath.ShouldBe(Path.GetFullPath(tempFile));
    }

    [Test]
    public void ExplicitStringCast_ReturnsFullPath()
    {
        var path = FilePath.FromString(Path.Combine(Path.GetTempPath(), "test.txt"));

        ((string)path).ShouldBe(path.FullPath);
    }

    [Test]
    public void ToString_ReturnsFullPath()
    {
        var path = FilePath.FromString(Path.Combine(Path.GetTempPath(), "test.txt"));

        path.ToString().ShouldBe(path.FullPath);
    }

    [Test]
    public void FromCombinedStrings_CombinesPaths()
    {
        var basePath = Path.GetTempPath();
        var path = FilePath.FromCombinedStrings(basePath, "myfile.cs");

        path.FileName.ShouldBe("myfile.cs");
        path.FullPath.ShouldStartWith(Path.GetFullPath(basePath).TrimEnd(Path.DirectorySeparatorChar));
    }

    [Test]
    public void Default_IsDefault_ReturnsTrue()
    {
        var path = default(FilePath);

        path.IsDefault.ShouldBeTrue();
    }

    [Test]
    public void Default_FullPath_IsNull()
    {
        var path = default(FilePath);

        path.FullPath.ShouldBeNull();
    }

    [Test]
    public void Default_Equals_AnotherDefault()
    {
        var a = default(FilePath);
        var b = default(FilePath);

        a.ShouldBe(b);
    }

    [Test]
    public void Default_GetHashCode_ReturnsZero()
    {
        var path = default(FilePath);

        path.GetHashCode().ShouldBe(0);
    }

    [Test]
    public void Default_DoesNotEqual_ValidPath()
    {
        var valid = FilePath.FromString(Path.Combine(Path.GetTempPath(), "test.txt"));
        var def = default(FilePath);

        def.ShouldNotBe(valid);
    }
}
