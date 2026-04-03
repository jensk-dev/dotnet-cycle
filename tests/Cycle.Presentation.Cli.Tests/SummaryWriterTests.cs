using Cycle.Application;
using Cycle.Core;

namespace Cycle.Presentation.Cli.Tests;

public sealed class SummaryWriterTests
{
    private static readonly string TempBase = Path.Combine(Path.GetTempPath(), "summary-test");

    private static readonly SolutionPath TestSolutionPath =
        SolutionPath.FromString(Path.Combine(TempBase, "MyApp.slnx"));

    private static readonly FilePath OutputFile =
        FilePath.FromString(Path.Combine(TempBase, "output.slnf"));

    private static readonly PipelineTimings TestTimings = new(
        SolutionRead: TimeSpan.FromMilliseconds(10),
        GraphLoad: TimeSpan.FromMilliseconds(500),
        AffectedResolve: TimeSpan.FromMilliseconds(30),
        ClosureResolve: TimeSpan.FromMilliseconds(5),
        ResultWrite: TimeSpan.FromMilliseconds(3),
        Total: TimeSpan.FromMilliseconds(548));

    private static GenerateSolutionFilterResult CreateResult(
        int includedCount = 5,
        int totalCount = 20,
        int failedCount = 0,
        int affectedCount = 3)
    {
        var projects = Enumerable.Range(0, includedCount)
            .Select(i =>
            {
                var path = FilePath.FromString(Path.Combine(TempBase, $"Project{i}", $"Project{i}.csproj"));
                return new ProjectInfo($"Project{i}", path);
            })
            .ToList();

        return new GenerateSolutionFilterResult(
            projects,
            Array.Empty<UnresolvedReference>(),
            totalCount,
            failedCount,
            affectedCount,
            TestTimings);
    }

    [Fact]
    public void Quiet_WritesNothing()
    {
        var writer = new StringWriter();
        var result = CreateResult();

        SummaryWriter.Write(writer, result, TestSolutionPath, OutputFile, LogVerbosity.Quiet, 10);

        writer.ToString().ShouldBeEmpty();
    }

    [Fact]
    public void Minimal_WritesSingleLine()
    {
        var writer = new StringWriter();
        var result = CreateResult(includedCount: 5, totalCount: 20, affectedCount: 3);

        SummaryWriter.Write(writer, result, TestSolutionPath, OutputFile, LogVerbosity.Minimal, 10);

        var output = writer.ToString().Trim();
        output.ShouldStartWith("cycle:");
        output.ShouldContain("5/20 projects included");
        output.ShouldContain("3 affected");
        output.ShouldContain("2 closure");
        output.ShouldContain("output.slnf");
    }

    [Fact]
    public void Minimal_WithFailures_IncludesFailedCount()
    {
        var writer = new StringWriter();
        var result = CreateResult(failedCount: 2, affectedCount: 3);

        SummaryWriter.Write(writer, result, TestSolutionPath, OutputFile, LogVerbosity.Minimal, 10);

        var output = writer.ToString();
        output.ShouldContain("2 failed");
    }

    [Fact]
    public void Normal_WritesMultiLineDetailed()
    {
        var writer = new StringWriter();
        var result = CreateResult(includedCount: 5, totalCount: 20, affectedCount: 3);

        SummaryWriter.Write(writer, result, TestSolutionPath, OutputFile, LogVerbosity.Normal, 15);

        var output = writer.ToString();
        output.ShouldContain("cycle summary");
        output.ShouldContain("MyApp.slnx");
        output.ShouldContain("20 projects");
        output.ShouldContain("15 changed files");
        output.ShouldContain("3 projects");
        output.ShouldContain("+2 dependencies");
        output.ShouldContain("5 / 20 projects");
        output.ShouldContain("15 projects");
        output.ShouldContain("duration:");
        output.ShouldNotContain("projects:");
    }

    [Fact]
    public void Verbose_IncludesProjectList()
    {
        var writer = new StringWriter();
        var result = CreateResult(includedCount: 3, totalCount: 10, affectedCount: 2);

        SummaryWriter.Write(writer, result, TestSolutionPath, OutputFile, LogVerbosity.Verbose, 5);

        var output = writer.ToString();
        output.ShouldContain("cycle summary");
        output.ShouldContain("projects:");
        output.ShouldContain("- Project0");
        output.ShouldContain("- Project1");
        output.ShouldContain("- Project2");
    }

    [Fact]
    public void Minimal_NoClosure_OmitsClosureCount()
    {
        var writer = new StringWriter();
        var result = CreateResult(includedCount: 3, totalCount: 10, affectedCount: 3);

        SummaryWriter.Write(writer, result, TestSolutionPath, OutputFile, LogVerbosity.Minimal, 5);

        var output = writer.ToString();
        output.ShouldNotContain("closure");
    }

    [Fact]
    public void Normal_WithFailures_ShowsFailedInAffectedLine()
    {
        var writer = new StringWriter();
        var result = CreateResult(includedCount: 5, totalCount: 20, failedCount: 1, affectedCount: 3);

        SummaryWriter.Write(writer, result, TestSolutionPath, OutputFile, LogVerbosity.Normal, 10);

        var output = writer.ToString();
        output.ShouldContain("1 failed to load");
    }
}
