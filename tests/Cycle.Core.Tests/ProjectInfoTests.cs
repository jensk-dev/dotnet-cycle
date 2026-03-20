namespace Cycle.Core.Tests;

[TestFixture]
public class ProjectInfoTests
{
    [Test]
    public void Constructor_WithNullName_ThrowsArgumentException()
    {
        Should.Throw<ArgumentException>(() => new ProjectInfo(
            null!,
            FilePath.FromString(Path.Combine(Path.GetTempPath(), "test.csproj")),
            ProjectType.CsProj));
    }

    [Test]
    public void Constructor_WithEmptyName_ThrowsArgumentException()
    {
        Should.Throw<ArgumentException>(() => new ProjectInfo(
            "",
            FilePath.FromString(Path.Combine(Path.GetTempPath(), "test.csproj")),
            ProjectType.CsProj));
    }

    [Test]
    public void Constructor_WithWhitespaceName_ThrowsArgumentException()
    {
        Should.Throw<ArgumentException>(() => new ProjectInfo(
            "   ",
            FilePath.FromString(Path.Combine(Path.GetTempPath(), "test.csproj")),
            ProjectType.CsProj));
    }

    [Test]
    public void Constructor_WithDefaultFilePath_ThrowsArgumentException()
    {
        Should.Throw<ArgumentException>(() => new ProjectInfo(
            "Test",
            default,
            ProjectType.CsProj));
    }

    [Test]
    public void Constructor_WithInvalidProjectType_ThrowsArgumentOutOfRangeException()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => new ProjectInfo(
            "Test",
            FilePath.FromString(Path.Combine(Path.GetTempPath(), "test.csproj")),
            (ProjectType)999));
    }

    [Test]
    public void Constructor_WithValidArguments_CreatesInstance()
    {
        var info = new ProjectInfo(
            "Test",
            FilePath.FromString(Path.Combine(Path.GetTempPath(), "test.csproj")),
            ProjectType.CsProj);

        info.Name.ShouldBe("Test");
        info.Type.ShouldBe(ProjectType.CsProj);
    }
}
