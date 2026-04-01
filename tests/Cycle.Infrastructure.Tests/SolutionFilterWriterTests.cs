using System.Text.Json;
using Cycle.Application;

namespace Cycle.Infrastructure.Tests;

public sealed class SolutionFilterWriterTests : IDisposable
{
    private readonly StringWriter _output = new();
    private readonly SolutionFilterWriter _sut = new();

    public void Dispose()
    {
        _output.Dispose();
    }

    [Fact]
    public async Task WriteAsync_WithProjects_WritesValidJson()
    {
        var filter = new SolutionFilter("MySolution.sln", ["src/A/A.csproj", "src/B/B.csproj"]);

        await _sut.WriteAsync(filter, _output, CancellationToken.None);

        var json = _output.ToString().Trim();
        var doc = JsonDocument.Parse(json);
        doc.RootElement.ValueKind.ShouldBe(JsonValueKind.Object);
    }

    [Fact]
    public async Task WriteAsync_SolutionPathMatchesModel()
    {
        var filter = new SolutionFilter("MySolution.sln", ["src/A/A.csproj"]);

        await _sut.WriteAsync(filter, _output, CancellationToken.None);

        var doc = JsonDocument.Parse(_output.ToString().Trim());
        doc.RootElement
            .GetProperty("solution")
            .GetProperty("path")
            .GetString()
            .ShouldBe("MySolution.sln");
    }

    [Fact]
    public async Task WriteAsync_ProjectPathsMatchModel()
    {
        var projects = new[] { "src/A/A.csproj", "src/B/B.fsproj" };
        var filter = new SolutionFilter("MySolution.sln", projects);

        await _sut.WriteAsync(filter, _output, CancellationToken.None);

        var doc = JsonDocument.Parse(_output.ToString().Trim());
        var array = doc.RootElement
            .GetProperty("solution")
            .GetProperty("projects");

        array.GetArrayLength().ShouldBe(2);
        array[0].GetString().ShouldBe("src/A/A.csproj");
        array[1].GetString().ShouldBe("src/B/B.fsproj");
    }

    [Fact]
    public async Task WriteAsync_WithEmptyProjects_WritesEmptyArray()
    {
        var filter = new SolutionFilter("MySolution.sln", []);

        await _sut.WriteAsync(filter, _output, CancellationToken.None);

        var doc = JsonDocument.Parse(_output.ToString().Trim());
        doc.RootElement
            .GetProperty("solution")
            .GetProperty("projects")
            .GetArrayLength()
            .ShouldBe(0);
    }

    [Fact]
    public async Task WriteAsync_OutputIsIndented()
    {
        var filter = new SolutionFilter("MySolution.sln", ["src/A/A.csproj"]);

        await _sut.WriteAsync(filter, _output, CancellationToken.None);

        var json = _output.ToString();
        json.ShouldContain(Environment.NewLine);
    }

    [Fact]
    public async Task WriteAsync_WithNullFilter_ThrowsArgumentNullException()
    {
        await Should.ThrowAsync<ArgumentNullException>(
            () => _sut.WriteAsync(null!, _output, CancellationToken.None));
    }

    [Fact]
    public async Task WriteAsync_WithNullOutput_ThrowsArgumentNullException()
    {
        var filter = new SolutionFilter("MySolution.sln", []);

        await Should.ThrowAsync<ArgumentNullException>(
            () => _sut.WriteAsync(filter, null!, CancellationToken.None));
    }

    [Fact]
    public async Task WriteAsync_HasCorrectSlnfStructure()
    {
        var filter = new SolutionFilter("MySolution.sln", ["src/A/A.csproj"]);

        await _sut.WriteAsync(filter, _output, CancellationToken.None);

        var doc = JsonDocument.Parse(_output.ToString().Trim());
        doc.RootElement.TryGetProperty("solution", out var solution).ShouldBeTrue();
        solution.TryGetProperty("path", out _).ShouldBeTrue();
        solution.TryGetProperty("projects", out _).ShouldBeTrue();
    }
}
