using System.Text.Json;

namespace Cycle.Core.Tests;

[TestFixture]
public class SolutionFilterWriterTests
{
    private StringWriter _output = null!;

    [SetUp]
    public void SetUp()
    {
        _output = new StringWriter();
    }

    [TearDown]
    public void TearDown()
    {
        _output.Dispose();
    }

    [Test]
    public async Task WriteAsync_WithProjects_WritesValidJson()
    {
        var filter = new SolutionFilter("MySolution.sln", ["src/A/A.csproj", "src/B/B.csproj"]);

        await SolutionFilterWriter.WriteAsync(filter, _output, CancellationToken.None);

        var json = _output.ToString().Trim();
        var doc = JsonDocument.Parse(json);
        doc.RootElement.ValueKind.ShouldBe(JsonValueKind.Object);
    }

    [Test]
    public async Task WriteAsync_SolutionPathMatchesModel()
    {
        var filter = new SolutionFilter("MySolution.sln", ["src/A/A.csproj"]);

        await SolutionFilterWriter.WriteAsync(filter, _output, CancellationToken.None);

        var doc = JsonDocument.Parse(_output.ToString().Trim());
        doc.RootElement
            .GetProperty("solution")
            .GetProperty("path")
            .GetString()
            .ShouldBe("MySolution.sln");
    }

    [Test]
    public async Task WriteAsync_ProjectPathsMatchModel()
    {
        var projects = new[] { "src/A/A.csproj", "src/B/B.fsproj" };
        var filter = new SolutionFilter("MySolution.sln", projects);

        await SolutionFilterWriter.WriteAsync(filter, _output, CancellationToken.None);

        var doc = JsonDocument.Parse(_output.ToString().Trim());
        var array = doc.RootElement
            .GetProperty("solution")
            .GetProperty("projects");

        array.GetArrayLength().ShouldBe(2);
        array[0].GetString().ShouldBe("src/A/A.csproj");
        array[1].GetString().ShouldBe("src/B/B.fsproj");
    }

    [Test]
    public async Task WriteAsync_WithEmptyProjects_WritesEmptyArray()
    {
        var filter = new SolutionFilter("MySolution.sln", []);

        await SolutionFilterWriter.WriteAsync(filter, _output, CancellationToken.None);

        var doc = JsonDocument.Parse(_output.ToString().Trim());
        doc.RootElement
            .GetProperty("solution")
            .GetProperty("projects")
            .GetArrayLength()
            .ShouldBe(0);
    }

    [Test]
    public async Task WriteAsync_OutputIsIndented()
    {
        var filter = new SolutionFilter("MySolution.sln", ["src/A/A.csproj"]);

        await SolutionFilterWriter.WriteAsync(filter, _output, CancellationToken.None);

        var json = _output.ToString();
        json.ShouldContain(Environment.NewLine);
    }

    [Test]
    public void WriteAsync_WithNullFilter_ThrowsArgumentNullException()
    {
        Should.ThrowAsync<ArgumentNullException>(
            () => SolutionFilterWriter.WriteAsync(null!, _output, CancellationToken.None));
    }

    [Test]
    public void WriteAsync_WithNullOutput_ThrowsArgumentNullException()
    {
        var filter = new SolutionFilter("MySolution.sln", []);

        Should.ThrowAsync<ArgumentNullException>(
            () => SolutionFilterWriter.WriteAsync(filter, null!, CancellationToken.None));
    }

    [Test]
    public async Task WriteAsync_HasCorrectSlnfStructure()
    {
        var filter = new SolutionFilter("MySolution.sln", ["src/A/A.csproj"]);

        await SolutionFilterWriter.WriteAsync(filter, _output, CancellationToken.None);

        var doc = JsonDocument.Parse(_output.ToString().Trim());
        doc.RootElement.TryGetProperty("solution", out var solution).ShouldBeTrue();
        solution.TryGetProperty("path", out _).ShouldBeTrue();
        solution.TryGetProperty("projects", out _).ShouldBeTrue();
    }
}
