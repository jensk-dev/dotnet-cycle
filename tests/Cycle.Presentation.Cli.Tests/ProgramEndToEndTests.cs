using System.Text.Json;
using Cycle.Infrastructure.MsBuild;

namespace Cycle.Presentation.Cli.Tests;

[TestFixture]
public class ProgramEndToEndTests
{
    private string _testDir = null!;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        MsBuildBootstrap.Initialize();
    }

    [SetUp]
    public void SetUp()
    {
        _testDir = Path.Combine(Path.GetTempPath(), $"e2e-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testDir);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_testDir))
            Directory.Delete(_testDir, true);
    }

    [Test]
    public async Task WithValidArgs_WritesSlnfAndReturnsZero()
    {
        var (slnPath, changedFilePath, outputPath) = SetUpProject("ClassA.cs", "class A {}");

        var exitCode = await Program.Main([slnPath, outputPath, "--changed-files", changedFilePath]);

        exitCode.ShouldBe(0);
        File.Exists(outputPath).ShouldBeTrue();

        var json = await File.ReadAllTextAsync(outputPath);
        var doc = JsonDocument.Parse(json);
        var projects = doc.RootElement.GetProperty("solution").GetProperty("projects");
        projects.GetArrayLength().ShouldBe(1);
    }

    [Test]
    public async Task WithNoChangedFiles_WritesEmptySlnf()
    {
        var projectDir = Path.Combine(_testDir, "ProjectA");
        Directory.CreateDirectory(projectDir);
        var csprojPath = Path.Combine(projectDir, "ProjectA.csproj");
        File.WriteAllText(csprojPath, """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
              </PropertyGroup>
            </Project>
            """);

        var slnPath = CreateSlnx(csprojPath);
        var changedFilePath = Path.Combine(_testDir, "changed.txt");
        File.WriteAllText(changedFilePath, "");
        var outputPath = Path.Combine(_testDir, "output.slnf");

        var exitCode = await Program.Main([slnPath, outputPath, "--changed-files", changedFilePath]);

        exitCode.ShouldBe(0);
        var json = await File.ReadAllTextAsync(outputPath);
        var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("solution").GetProperty("projects").GetArrayLength().ShouldBe(0);
    }

    [Test]
    public async Task WithInvalidSolutionPath_ReturnsOne()
    {
        var outputPath = Path.Combine(_testDir, "output.slnf");
        var changedFilePath = Path.Combine(_testDir, "changed.txt");
        File.WriteAllText(changedFilePath, "");

        var exitCode = await Program.Main(
            [Path.Combine(_testDir, "nonexistent.slnx"), outputPath, "--changed-files", changedFilePath]);

        exitCode.ShouldBe(1);
    }

    [Test]
    public async Task WithVerboseLogging_Succeeds()
    {
        var (slnPath, changedFilePath, outputPath) = SetUpProject("ClassA.cs", "class A {}");

        var exitCode = await Program.Main(
            [slnPath, outputPath, "--changed-files", changedFilePath, "--log-level", "verbose"]);

        exitCode.ShouldBe(0);
    }

    private (string slnPath, string changedFilePath, string outputPath) SetUpProject(string fileName, string content)
    {
        var projectDir = Path.Combine(_testDir, "ProjectA");
        Directory.CreateDirectory(projectDir);
        var csprojPath = Path.Combine(projectDir, "ProjectA.csproj");
        var filePath = Path.Combine(projectDir, fileName);
        File.WriteAllText(filePath, content);
        File.WriteAllText(csprojPath, $"""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
              </PropertyGroup>
              <ItemGroup>
                <Compile Include="{fileName}" />
              </ItemGroup>
            </Project>
            """);

        var slnPath = CreateSlnx(csprojPath);
        var changedFilePath = Path.Combine(_testDir, "changed.txt");
        File.WriteAllText(changedFilePath, filePath);
        var outputPath = Path.Combine(_testDir, "output.slnf");

        return (slnPath, changedFilePath, outputPath);
    }

    private string CreateSlnx(params string[] projectPaths)
    {
        var slnPath = Path.Combine(_testDir, "Test.slnx");
        var projectEntries = string.Join(Environment.NewLine,
            projectPaths.Select(p =>
                $"    <Project Path=\"{Path.GetRelativePath(_testDir, p)}\" />"));

        var content = $"""
                       <Solution>
                         <Folder Name="/src/">
                       {projectEntries}
                         </Folder>
                       </Solution>
                       """;

        File.WriteAllText(slnPath, content);
        return slnPath;
    }
}
