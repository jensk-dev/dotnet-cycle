namespace Cycle.Core.Tests;

[TestFixture]
public class ProjectInfoTests
{
    [Test]
    public void Constructor_WithNullName_ThrowsArgumentException()
    {
        Should.Throw<ArgumentException>(() => new ProjectInfo(
            null!,
            FilePath.FromString(Path.Combine(Path.GetTempPath(), "test.csproj"))));
    }

    [Test]
    public void Constructor_WithEmptyName_ThrowsArgumentException()
    {
        Should.Throw<ArgumentException>(() => new ProjectInfo(
            "",
            FilePath.FromString(Path.Combine(Path.GetTempPath(), "test.csproj"))));
    }

    [Test]
    public void Constructor_WithWhitespaceName_ThrowsArgumentException()
    {
        Should.Throw<ArgumentException>(() => new ProjectInfo(
            "   ",
            FilePath.FromString(Path.Combine(Path.GetTempPath(), "test.csproj"))));
    }

    [Test]
    public void Constructor_WithDefaultFilePath_ThrowsArgumentException()
    {
        Should.Throw<ArgumentException>(() => new ProjectInfo(
            "Test",
            default));
    }

    [Test]
    public void Constructor_WithValidArguments_CreatesInstance()
    {
        var info = new ProjectInfo(
            "Test",
            FilePath.FromString(Path.Combine(Path.GetTempPath(), "test.csproj")));

        info.Name.ShouldBe("Test");
        info.FilePath.FileName.ShouldBe("test.csproj");
    }
}
