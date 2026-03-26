namespace Cycle.Core.Tests;

[TestFixture]
public class SolutionFilterBuilderTests
{
    [Test]
    public void Build_SolutionInSameDir_PathIsFilename()
    {
        var solutionPath = Path.Combine(Path.GetTempPath(), "repo", "MySolution.sln");
        var outputDir = Path.Combine(Path.GetTempPath(), "repo");
        var projects = CreateProjects(Path.Combine(Path.GetTempPath(), "repo", "src", "A", "A.csproj"));

        var result = SolutionFilterBuilder.Build(solutionPath, outputDir, projects);

        result.SolutionPath.ShouldBe("MySolution.sln");
    }

    [Test]
    public void Build_SlnfInSubdir_SolutionPathNavigatesUp()
    {
        var solutionPath = Path.Combine(Path.GetTempPath(), "repo", "MySolution.sln");
        var outputDir = Path.Combine(Path.GetTempPath(), "repo", "output");
        var projects = CreateProjects(Path.Combine(Path.GetTempPath(), "repo", "src", "A", "A.csproj"));

        var result = SolutionFilterBuilder.Build(solutionPath, outputDir, projects);

        result.SolutionPath.ShouldContain("..");
        result.SolutionPath.ShouldEndWith("MySolution.sln");
    }

    [Test]
    public void Build_ProjectPaths_AreRelativeToSolutionDir()
    {
        var repoDir = Path.Combine(Path.GetTempPath(), "repo");
        var solutionPath = Path.Combine(repoDir, "MySolution.sln");
        var outputDir = repoDir;
        var projects = CreateProjects(
            Path.Combine(repoDir, "src", "A", "A.csproj"),
            Path.Combine(repoDir, "src", "B", "B.fsproj"));

        var result = SolutionFilterBuilder.Build(solutionPath, outputDir, projects);

        result.Projects.Count.ShouldBe(2);
        result.Projects[0].ShouldBe(Path.Combine("src", "A", "A.csproj"));
        result.Projects[1].ShouldBe(Path.Combine("src", "B", "B.fsproj"));
    }

    [Test]
    public void Build_WithNoProjects_ReturnsEmptyList()
    {
        var solutionPath = Path.Combine(Path.GetTempPath(), "repo", "MySolution.sln");
        var outputDir = Path.Combine(Path.GetTempPath(), "repo");

        var result = SolutionFilterBuilder.Build(solutionPath, outputDir, []);

        result.Projects.ShouldBeEmpty();
    }

    [Test]
    public void Build_SolutionPath_IsNotEmpty()
    {
        var solutionPath = Path.Combine(Path.GetTempPath(), "repo", "MySolution.sln");
        var outputDir = Path.Combine(Path.GetTempPath(), "repo");

        var result = SolutionFilterBuilder.Build(solutionPath, outputDir, []);

        result.SolutionPath.ShouldNotBeNullOrWhiteSpace();
    }

    private static List<ProjectInfo> CreateProjects(params string[] paths) =>
        paths.Select(p => new ProjectInfo(
            Path.GetFileNameWithoutExtension(p),
            FilePath.FromString(p))).ToList();
}
