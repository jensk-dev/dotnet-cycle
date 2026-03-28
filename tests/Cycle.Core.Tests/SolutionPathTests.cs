namespace Cycle.Core.Tests;

public sealed class SolutionPathTests
{
    [Fact]
    public void FromString_WithValidPath_CreatesSolutionPath()
    {
        var path = SolutionPath.FromString(Path.Combine(Path.GetTempPath(), "test.sln"));

        path.FilePath.FullPath.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public void FromString_WithAnyExtension_CreatesSolutionPath()
    {
        var path = SolutionPath.FromString(Path.Combine(Path.GetTempPath(), "test.csproj"));

        path.FilePath.FullPath.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public void FromString_WithNullPath_ThrowsArgumentException()
    {
        Should.Throw<ArgumentException>(() => SolutionPath.FromString(null!));
    }

    [Fact]
    public void FromString_WithEmptyPath_ThrowsArgumentException()
    {
        Should.Throw<ArgumentException>(() => SolutionPath.FromString(""));
    }

    [Fact]
    public void FromString_WithWhitespacePath_ThrowsArgumentException()
    {
        Should.Throw<ArgumentException>(() => SolutionPath.FromString("   "));
    }

    [Fact]
    public void FromString_WithRelativePath_ResolvesToFullPath()
    {
        var path = SolutionPath.FromString("mysolution.sln");

        Path.IsPathRooted(path.FilePath.FullPath).ShouldBeTrue();
    }

    [Fact]
    public void TryFromString_WithValidPath_ReturnsTrue()
    {
        var result = SolutionPath.TryFromString(
            Path.Combine(Path.GetTempPath(), "test.sln"), out var solutionPath);

        result.ShouldBeTrue();
        solutionPath.ShouldNotBeNull();
    }

    [Fact]
    public void TryFromString_WithEmptyPath_ReturnsFalse()
    {
        var result = SolutionPath.TryFromString("", out var solutionPath);

        result.ShouldBeFalse();
        solutionPath.ShouldBeNull();
    }

    [Fact]
    public void TryFromString_WithNullPath_ReturnsFalse()
    {
        var result = SolutionPath.TryFromString(null!, out var solutionPath);

        result.ShouldBeFalse();
        solutionPath.ShouldBeNull();
    }

    [Fact]
    public void FromFilePath_CreatesSolutionPath()
    {
        var filePath = FilePath.FromString(Path.Combine(Path.GetTempPath(), "test.sln"));

        var solutionPath = SolutionPath.FromFilePath(filePath);

        solutionPath.FilePath.ShouldBe(filePath);
    }

    [Fact]
    public void Equals_SamePath_ReturnsTrue()
    {
        var a = SolutionPath.FromString(Path.Combine(Path.GetTempPath(), "same.sln"));
        var b = SolutionPath.FromString(Path.Combine(Path.GetTempPath(), "same.sln"));

        a.ShouldBe(b);
    }

    [Fact]
    public void Equals_DifferentPaths_ReturnsFalse()
    {
        var a = SolutionPath.FromString(Path.Combine(Path.GetTempPath(), "one.sln"));
        var b = SolutionPath.FromString(Path.Combine(Path.GetTempPath(), "two.sln"));

        a.ShouldNotBe(b);
    }

    [Fact]
    public void Equals_IsCaseInsensitive()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), "Windows only");

        var lower = SolutionPath.FromString(Path.Combine(Path.GetTempPath(), "test.sln"));
        var upper = SolutionPath.FromString(Path.Combine(Path.GetTempPath(), "TEST.SLN"));

        lower.ShouldBe(upper);
    }

    [Fact]
    public void Equals_IsCaseSensitive()
    {
        Assert.SkipUnless(OperatingSystem.IsLinux(), "Linux only");

        var lower = SolutionPath.FromString(Path.Combine(Path.GetTempPath(), "test.sln"));
        var upper = SolutionPath.FromString(Path.Combine(Path.GetTempPath(), "TEST.SLN"));

        lower.ShouldNotBe(upper);
    }

    [Fact]
    public void Default_IsDefault_ReturnsTrue()
    {
        var path = default(SolutionPath);

        path.IsDefault.ShouldBeTrue();
    }

    [Fact]
    public void Default_Equals_AnotherDefault()
    {
        var a = default(SolutionPath);
        var b = default(SolutionPath);

        a.ShouldBe(b);
    }

    [Fact]
    public void Default_DoesNotEqual_ValidSolutionPath()
    {
        var valid = SolutionPath.FromString(Path.Combine(Path.GetTempPath(), "test.sln"));
        var def = default(SolutionPath);

        def.ShouldNotBe(valid);
    }

    [Fact]
    public void ExplicitStringCast_ReturnsFullPath()
    {
        var path = SolutionPath.FromString(Path.Combine(Path.GetTempPath(), "test.sln"));

        ((string)path).ShouldBe(path.FilePath.FullPath);
    }

    [Fact]
    public void ExplicitCastFromString_CreatesSolutionPath()
    {
        var path = (SolutionPath)Path.Combine(Path.GetTempPath(), "test.sln");

        path.FilePath.FullPath.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public void ToString_ReturnsFullPath()
    {
        var path = SolutionPath.FromString(Path.Combine(Path.GetTempPath(), "test.sln"));

        path.ToString().ShouldBe(path.FilePath.FullPath);
    }
}
