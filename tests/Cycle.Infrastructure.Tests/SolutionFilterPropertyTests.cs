using System.Text.Json;
using Cycle.Application;
using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;

namespace Cycle.Infrastructure.Tests;

public sealed class SolutionFilterPropertyTests
{
    private static Arbitrary<SolutionFilter> SolutionFilters()
    {
        var projectPathGen =
            from name in Gen.Elements("Alpha", "Beta", "Gamma", "Delta", "Epsilon")
            from ext in Gen.Elements(".csproj", ".fsproj", ".vbproj")
            select $"src/{name}/{name}{ext}";

        var filterGen =
            from projects in projectPathGen.ListOf()
            select new SolutionFilter("MySolution.sln", projects.ToList());

        return filterGen.ToArbitrary();
    }

    [Property]
    public Property SolutionFilterWriter_OutputParsesAsValidJsonWithCorrectProjectCount()
    {
        return Prop.ForAll(SolutionFilters(), filter =>
        {
            using var output = new StringWriter();
            var sut = new SolutionFilterWriter();
            sut.WriteAsync(filter, output, CancellationToken.None).GetAwaiter().GetResult();

            var json = output.ToString().Trim();
            var doc = JsonDocument.Parse(json);
            var projects = doc.RootElement
                .GetProperty("solution")
                .GetProperty("projects");

            return projects.GetArrayLength() == filter.Projects.Count;
        });
    }

    [Property]
    public Property SolutionFilterWriter_SolutionPathIsPreserved()
    {
        return Prop.ForAll(SolutionFilters(), filter =>
        {
            using var output = new StringWriter();
            var sut = new SolutionFilterWriter();
            sut.WriteAsync(filter, output, CancellationToken.None).GetAwaiter().GetResult();

            var json = output.ToString().Trim();
            var doc = JsonDocument.Parse(json);
            var path = doc.RootElement
                .GetProperty("solution")
                .GetProperty("path")
                .GetString();

            return path == filter.SolutionPath;
        });
    }
}
