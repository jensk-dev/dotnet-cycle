using Cycle.Core;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cycle.Application.Tests;

public sealed class GenerateSolutionFilterUseCaseTests
{
    private static readonly string TempBase = Path.Combine(Path.GetTempPath(), "cycle-usecase-test");

    private static readonly SolutionPath TestSolutionPath =
        SolutionPath.FromString(Path.Combine(TempBase, "Test.sln"));

    private static readonly FilePath OutputFile = FilePath.FromString(
        Path.Combine(TempBase, "output", "out.slnf"));

    private static readonly FilePath ProjectAPath = FilePath.FromString(
        Path.Combine(TempBase, "src", "A", "A.csproj"));

    private static readonly FilePath ProjectBPath = FilePath.FromString(
        Path.Combine(TempBase, "src", "B", "B.csproj"));

    private static readonly ProjectInfo ProjectA = new("A", ProjectAPath);
    private static readonly ProjectInfo ProjectB = new("B", ProjectBPath);

    private static GenerateSolutionFilterUseCase CreateUseCase(
        ISolutionReader solutionReader,
        IProjectGraphLoader graphLoader,
        IResultWriter resultWriter) =>
        new(
            solutionReader,
            graphLoader,
            resultWriter,
            new ProjectScopeFilter(),
            new AffectedProjectsResolver(),
            new DependencyClosureResolver(),
            NullLogger<GenerateSolutionFilterUseCase>.Instance);

    [Fact]
    public async Task ExecuteAsync_WithoutClosure_ReturnsAffectedOnly()
    {
        var changedFile = FilePath.FromString(
            Path.Combine(TempBase, "src", "A", "File.cs"));
        var loadedA = LoadedProjectData.Loaded(
            ProjectA,
            new HashSet<FilePath> { ProjectAPath, changedFile },
            new HashSet<FilePath>());
        var loadedB = LoadedProjectData.Loaded(
            ProjectB,
            new HashSet<FilePath> { ProjectBPath },
            new HashSet<FilePath>());

        // Forward map: A depends on B. If closure ran, B would be included.
        var forwardMap = new Dictionary<FilePath, IReadOnlySet<FilePath>>
        {
            [ProjectAPath] = new HashSet<FilePath> { ProjectBPath },
        };
        var graph = new ProjectGraph(
            [loadedA, loadedB],
            forwardMap,
            new Dictionary<FilePath, IReadOnlySet<FilePath>>(),
            new Dictionary<FilePath, ProjectInfo>
            {
                [ProjectAPath] = ProjectA,
                [ProjectBPath] = ProjectB,
            });

        var solutionReader = new StubSolutionReader([ProjectA, ProjectB]);
        var graphLoader = new StubProjectGraphLoader(graph);
        var useCase = CreateUseCase(solutionReader, graphLoader, new StubResultWriter());

        var result = await useCase.ExecuteAsync(
            new GenerateSolutionFilterUseCaseOptions
            {
                SolutionPath = TestSolutionPath,
                FilesToTrace = [changedFile],
                IncludeClosure = false,
                OutputFile = OutputFile,
            }, CancellationToken.None);

        result.IncludedProjects.Count.ShouldBe(1);
        result.IncludedProjects[0].Name.ShouldBe("A");
        result.UnresolvedReferences.ShouldBeEmpty();
    }

    [Fact]
    public async Task ExecuteAsync_WithClosure_ReturnsClosureProjects()
    {
        var changedFile = FilePath.FromString(
            Path.Combine(TempBase, "src", "A", "File.cs"));
        var loadedA = LoadedProjectData.Loaded(
            ProjectA,
            new HashSet<FilePath> { ProjectAPath, changedFile },
            new HashSet<FilePath>());
        var loadedB = LoadedProjectData.Loaded(
            ProjectB,
            new HashSet<FilePath> { ProjectBPath },
            new HashSet<FilePath>());

        var forwardMap = new Dictionary<FilePath, IReadOnlySet<FilePath>>
        {
            [ProjectAPath] = new HashSet<FilePath> { ProjectBPath },
        };
        var graph = new ProjectGraph(
            [loadedA, loadedB],
            forwardMap,
            new Dictionary<FilePath, IReadOnlySet<FilePath>>(),
            new Dictionary<FilePath, ProjectInfo>
            {
                [ProjectAPath] = ProjectA,
                [ProjectBPath] = ProjectB,
            });

        var solutionReader = new StubSolutionReader([ProjectA, ProjectB]);
        var graphLoader = new StubProjectGraphLoader(graph);
        var useCase = CreateUseCase(solutionReader, graphLoader, new StubResultWriter());

        var result = await useCase.ExecuteAsync(
            new GenerateSolutionFilterUseCaseOptions
            {
                SolutionPath = TestSolutionPath,
                FilesToTrace = [changedFile],
                IncludeClosure = true,
                OutputFile = OutputFile,
            }, CancellationToken.None);

        result.IncludedProjects.Count.ShouldBe(2);
    }

    [Fact]
    public async Task ExecuteAsync_WithClosure_PropagatesUnresolvedReferences()
    {
        var changedFile = FilePath.FromString(
            Path.Combine(TempBase, "src", "A", "File.cs"));
        var missingPath = FilePath.FromString(
            Path.Combine(TempBase, "src", "Missing", "Missing.csproj"));
        var loadedA = LoadedProjectData.Loaded(
            ProjectA,
            new HashSet<FilePath> { ProjectAPath, changedFile },
            new HashSet<FilePath>());

        var forwardMap = new Dictionary<FilePath, IReadOnlySet<FilePath>>
        {
            [ProjectAPath] = new HashSet<FilePath> { missingPath },
        };
        var graph = new ProjectGraph(
            [loadedA],
            forwardMap,
            new Dictionary<FilePath, IReadOnlySet<FilePath>>(),
            new Dictionary<FilePath, ProjectInfo>
            {
                [ProjectAPath] = ProjectA,
            });

        var solutionReader = new StubSolutionReader([ProjectA]);
        var graphLoader = new StubProjectGraphLoader(graph);
        var useCase = CreateUseCase(solutionReader, graphLoader, new StubResultWriter());

        var result = await useCase.ExecuteAsync(
            new GenerateSolutionFilterUseCaseOptions
            {
                SolutionPath = TestSolutionPath,
                FilesToTrace = [changedFile],
                IncludeClosure = true,
                OutputFile = OutputFile,
            }, CancellationToken.None);

        result.UnresolvedReferences.Count.ShouldBe(1);
        result.UnresolvedReferences[0].ReferencedBy.ShouldBe(ProjectAPath);
        result.UnresolvedReferences[0].ReferencePath.ShouldBe(missingPath);
    }

    [Fact]
    public async Task ExecuteAsync_WithProjectScope_FiltersToScopedProjects()
    {
        var changedFile = FilePath.FromString(
            Path.Combine(TempBase, "src", "A", "File.cs"));
        var loadedA = LoadedProjectData.Loaded(
            ProjectA,
            new HashSet<FilePath> { ProjectAPath, changedFile },
            new HashSet<FilePath>());
        var loadedB = LoadedProjectData.Loaded(
            ProjectB,
            new HashSet<FilePath> { ProjectBPath },
            new HashSet<FilePath>());

        var graph = new ProjectGraph(
            [loadedA],
            new Dictionary<FilePath, IReadOnlySet<FilePath>>(),
            new Dictionary<FilePath, IReadOnlySet<FilePath>>(),
            new Dictionary<FilePath, ProjectInfo>
            {
                [ProjectAPath] = ProjectA,
            });

        var solutionReader = new StubSolutionReader([ProjectA, ProjectB]);
        var graphLoader = new StubProjectGraphLoader(graph);
        var resultWriter = new StubResultWriter();
        var useCase = CreateUseCase(solutionReader, graphLoader, resultWriter);

        var result = await useCase.ExecuteAsync(
            new GenerateSolutionFilterUseCaseOptions
            {
                SolutionPath = TestSolutionPath,
                FilesToTrace = [changedFile],
                IncludeClosure = false,
                OutputFile = OutputFile,
                ProjectScope = new HashSet<FilePath> { ProjectAPath },
            }, CancellationToken.None);

        result.TotalProjectCount.ShouldBe(1);
        result.IncludedProjects.Count.ShouldBe(1);
        result.IncludedProjects[0].Name.ShouldBe("A");
    }

    [Fact]
    public async Task ExecuteAsync_CallsFilterWriter()
    {
        var changedFile = FilePath.FromString(
            Path.Combine(TempBase, "src", "A", "File.cs"));
        var loadedA = LoadedProjectData.Loaded(
            ProjectA,
            new HashSet<FilePath> { ProjectAPath, changedFile },
            new HashSet<FilePath>());
        var graph = new ProjectGraph(
            [loadedA],
            new Dictionary<FilePath, IReadOnlySet<FilePath>>(),
            new Dictionary<FilePath, IReadOnlySet<FilePath>>(),
            new Dictionary<FilePath, ProjectInfo>
            {
                [ProjectAPath] = ProjectA,
            });

        var solutionReader = new StubSolutionReader([ProjectA]);
        var graphLoader = new StubProjectGraphLoader(graph);
        var resultWriter = new StubResultWriter();
        var useCase = CreateUseCase(solutionReader, graphLoader, resultWriter);

        await useCase.ExecuteAsync(
            new GenerateSolutionFilterUseCaseOptions
            {
                SolutionPath = TestSolutionPath,
                FilesToTrace = [changedFile],
                IncludeClosure = false,
                OutputFile = OutputFile,
            }, CancellationToken.None);

        resultWriter.LastProjects.ShouldNotBeNull();
    }

    private sealed class StubResultWriter : IResultWriter
    {
        public IReadOnlyList<ProjectInfo>? LastProjects { get; private set; }

        public Task WriteAsync(IReadOnlyList<ProjectInfo> projects,
            FilePath outputFile, CancellationToken ct)
        {
            LastProjects = projects;
            return Task.CompletedTask;
        }
    }

    private sealed class StubSolutionReader(
        IReadOnlyList<ProjectInfo> projects) : ISolutionReader
    {
        public Task<IReadOnlyList<ProjectInfo>> GetProjectsAsync(
            SolutionPath solutionPath, CancellationToken ct)
        {
            return Task.FromResult(projects);
        }
    }

    private sealed class StubProjectGraphLoader(ProjectGraph graph) : IProjectGraphLoader
    {
        public ProjectGraph Load(
            IReadOnlyList<ProjectInfo> projects,
            IReadOnlyList<FilePath> changedFiles,
            CancellationToken ct)
        {
            return graph;
        }
    }
}
