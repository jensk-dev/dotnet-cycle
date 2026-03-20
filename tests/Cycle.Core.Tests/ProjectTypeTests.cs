namespace Cycle.Core.Tests;

[TestFixture]
public class ProjectTypeTests
{
    [TestCase(ProjectType.CsProj, "*.csproj")]
    [TestCase(ProjectType.FsProj, "*.fsproj")]
    [TestCase(ProjectType.VbProj, "*.vbproj")]
    [TestCase(ProjectType.SqlProj, "*.sqlproj")]
    [TestCase(ProjectType.DtProj, "*.dtproj")]
    [TestCase(ProjectType.Proj, "*.proj")]
    public void ToFileGlob_ReturnsCorrectGlob(ProjectType type, string expectedGlob)
    {
        type.ToFileGlob().ShouldBe(expectedGlob);
    }

    [TestCase(ProjectType.CsProj, ".csproj")]
    [TestCase(ProjectType.FsProj, ".fsproj")]
    [TestCase(ProjectType.VbProj, ".vbproj")]
    [TestCase(ProjectType.SqlProj, ".sqlproj")]
    [TestCase(ProjectType.DtProj, ".dtproj")]
    [TestCase(ProjectType.Proj, ".proj")]
    public void ToFileExtension_ReturnsCorrectExtension(ProjectType type, string expectedExtension)
    {
        type.ToFileExtension().ShouldBe(expectedExtension);
    }

    [TestCase(".csproj", ProjectType.CsProj)]
    [TestCase(".fsproj", ProjectType.FsProj)]
    [TestCase(".vbproj", ProjectType.VbProj)]
    [TestCase(".sqlproj", ProjectType.SqlProj)]
    [TestCase(".dtproj", ProjectType.DtProj)]
    [TestCase(".proj", ProjectType.Proj)]
    public void TryFromExtension_WithValidExtension_ReturnsTrueAndCorrectType(string extension, ProjectType expected)
    {
        ProjectTypeExtensions.TryFromExtension(extension, out var result).ShouldBeTrue();
        result.ShouldBe(expected);
    }

    [TestCase(".CSPROJ", ProjectType.CsProj)]
    [TestCase(".CsProj", ProjectType.CsProj)]
    public void TryFromExtension_IsCaseInsensitive(string extension, ProjectType expected)
    {
        ProjectTypeExtensions.TryFromExtension(extension, out var result).ShouldBeTrue();
        result.ShouldBe(expected);
    }

    [TestCase(".txt")]
    [TestCase(".sln")]
    [TestCase("")]
    public void TryFromExtension_WithInvalidExtension_ReturnsFalse(string extension)
    {
        ProjectTypeExtensions.TryFromExtension(extension, out _).ShouldBeFalse();
    }
}
