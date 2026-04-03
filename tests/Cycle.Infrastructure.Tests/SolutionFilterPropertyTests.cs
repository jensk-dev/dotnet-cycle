using System.Text;
using System.Text.Json;
using Cycle.Core;
using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;

namespace Cycle.Infrastructure.Tests;

public sealed class SolutionFilterPropertyTests
{
    private static readonly string RepoDir = Path.Combine(Path.GetTempPath(), "repo");

    private static readonly FilePath DummyOutputFile = FilePath.FromString(
        Path.Combine(Path.GetTempPath(), "dummy.slnf"));

    private static Arbitrary<IReadOnlyList<ProjectInfo>> ProjectLists()
    {
        var projectGen =
            from name in Gen.Elements("Alpha", "Beta", "Gamma", "Delta", "Epsilon")
            from ext in Gen.Elements(".csproj", ".fsproj", ".vbproj")
            select new ProjectInfo(name, FilePath.FromString(
                Path.Combine(RepoDir, "src", name, $"{name}{ext}")));

        return projectGen.ListOf()
            .Select(IReadOnlyList<ProjectInfo> (l) => [.. l.DistinctBy(p => p.FilePath)])
            .ToArbitrary();
    }

    [Property]
    public Property WriteAsync_OutputParsesAsValidJsonWithCorrectProjectCount()
    {
        return Prop.ForAll(ProjectLists(), projects =>
        {
            var output = new MemoryStream();
            var solutionPath = SolutionPath.FromString(Path.Combine(RepoDir, "MySolution.sln"));
            var sut = new SlnfResultWriter(solutionPath, new StubOutputStreamFactory(output));
            sut.WriteAsync(projects, DummyOutputFile, CancellationToken.None).GetAwaiter().GetResult();

            var json = Encoding.UTF8.GetString(output.ToArray()).Trim();
            var doc = JsonDocument.Parse(json);
            var jsonProjects = doc.RootElement
                .GetProperty("solution")
                .GetProperty("projects");

            return jsonProjects.GetArrayLength() == projects.Count;
        });
    }

    [Property]
    public Property WriteAsync_SolutionPathIsPreserved()
    {
        return Prop.ForAll(ProjectLists(), projects =>
        {
            var output = new MemoryStream();
            var solutionPath = SolutionPath.FromString(Path.Combine(RepoDir, "MySolution.sln"));
            var sut = new SlnfResultWriter(solutionPath, new StubOutputStreamFactory(output));
            sut.WriteAsync(projects, DummyOutputFile, CancellationToken.None).GetAwaiter().GetResult();

            var json = Encoding.UTF8.GetString(output.ToArray()).Trim();
            var doc = JsonDocument.Parse(json);
            var path = doc.RootElement
                .GetProperty("solution")
                .GetProperty("path")
                .GetString();

            return string.Equals(path, "MySolution.sln", StringComparison.Ordinal);
        });
    }

    private sealed class StubOutputStreamFactory(Stream stream) : IOutputStreamFactory
    {
        public Stream Create(FilePath path) => stream;
    }
}
