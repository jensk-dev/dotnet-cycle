using Cycle.Core;

namespace Cycle.Application.Tests;

public sealed class GenerateSolutionFilterUseCaseTests
{
    private static readonly string TempBase = Path.Combine(Path.GetTempPath(), "cycle-usecase-test");

    private static readonly SolutionPath TestSolutionPath =
        SolutionPath.FromString(Path.Combine(TempBase, "Test.sln"));

    private static readonly string OutputDir = Path.Combine(TempBase, "output");

    private static readonly FilePath ProjectAPath = FilePath.FromString(
        Path.Combine(TempBase, "src", "A", "A.csproj"));

    private static readonly FilePath ProjectBPath = FilePath.FromString(
        Path.Combine(TempBase, "src", "B", "B.csproj"));

    private static readonly ProjectInfo ProjectA = new("A", ProjectAPath);
    private static readonly ProjectInfo ProjectB = new("B", ProjectBPath);

    [Fact]
    public async Task ExecuteAsync_WithoutClosure_ReturnsAffectedOnly()
    {
        var affected = new Dictionary<FilePath, ProjectInfo> { [ProjectAPath] = ProjectA };
        var solutionReader = new StubSolutionReader([]);
        var graphLoader = new StubProjectGraphLoader(CreateEmptyGraph());
        var affectedResolver = new StubAffectedProjectsResolver(
            new AffectedProjectsResult(affected, new Dictionary<FilePath, ProjectInfo>()));
        var closureResolver = new SpyClosureResolver();
        var useCase = new GenerateSolutionFilterUseCase(solutionReader, graphLoader, affectedResolver, closureResolver);

        var result = await useCase.ExecuteAsync(
            TestSolutionPath, [], false, OutputDir, CancellationToken.None);

        result.IncludedProjects.Count.ShouldBe(1);
        result.IncludedProjects[0].Name.ShouldBe("A");
        result.UnresolvedReferences.ShouldBeEmpty();
        closureResolver.WasCalled.ShouldBeFalse();
    }

    [Fact]
    public async Task ExecuteAsync_WithClosure_CallsClosureResolverAndReturnsClosureProjects()
    {
        var affected = new Dictionary<FilePath, ProjectInfo> { [ProjectAPath] = ProjectA };
        var closureProjects = new List<ProjectInfo> { ProjectA, ProjectB };
        var solutionReader = new StubSolutionReader([]);
        var graphLoader = new StubProjectGraphLoader(CreateEmptyGraph());
        var affectedResolver = new StubAffectedProjectsResolver(
            new AffectedProjectsResult(affected, new Dictionary<FilePath, ProjectInfo>()));
        var closureResolver = new SpyClosureResolver(
            new ClosureResult(closureProjects, []));
        var useCase = new GenerateSolutionFilterUseCase(solutionReader, graphLoader, affectedResolver, closureResolver);

        var result = await useCase.ExecuteAsync(
            TestSolutionPath, [], true, OutputDir, CancellationToken.None);

        result.IncludedProjects.Count.ShouldBe(2);
        closureResolver.WasCalled.ShouldBeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_WithClosure_PropagatesUnresolvedReferences()
    {
        var affected = new Dictionary<FilePath, ProjectInfo> { [ProjectAPath] = ProjectA };
        var unresolved = new UnresolvedReference(ProjectAPath, ProjectBPath);
        var solutionReader = new StubSolutionReader([]);
        var graphLoader = new StubProjectGraphLoader(CreateEmptyGraph());
        var affectedResolver = new StubAffectedProjectsResolver(
            new AffectedProjectsResult(affected, new Dictionary<FilePath, ProjectInfo>()));
        var closureResolver = new SpyClosureResolver(
            new ClosureResult([ProjectA], [unresolved]));
        var useCase = new GenerateSolutionFilterUseCase(solutionReader, graphLoader, affectedResolver, closureResolver);

        var result = await useCase.ExecuteAsync(
            TestSolutionPath, [], true, OutputDir, CancellationToken.None);

        result.UnresolvedReferences.Count.ShouldBe(1);
        result.UnresolvedReferences[0].ShouldBe(unresolved);
    }

    private static ProjectGraph CreateEmptyGraph() =>
        new([], new Dictionary<FilePath, HashSet<FilePath>>(),
            new Dictionary<FilePath, HashSet<FilePath>>(),
            new Dictionary<FilePath, ProjectInfo>());

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

    private sealed class StubAffectedProjectsResolver(
        AffectedProjectsResult result) : IAffectedProjectsResolver
    {
        public AffectedProjectsResult Resolve(
            IReadOnlyList<LoadedProjectData> projects,
            IReadOnlyDictionary<FilePath, HashSet<FilePath>> reverseMap,
            IReadOnlyList<FilePath> changedFiles)
        {
            return result;
        }
    }

    private sealed class SpyClosureResolver : IDependencyClosureResolver
    {
        private readonly ClosureResult _result;
        public bool WasCalled { get; private set; }

        public SpyClosureResolver(ClosureResult? result = null)
        {
            _result = result ?? new ClosureResult([], []);
        }

        public ClosureResult Resolve(
            IReadOnlyDictionary<FilePath, ProjectInfo> affected,
            IReadOnlyDictionary<FilePath, HashSet<FilePath>> forwardMap,
            IReadOnlyDictionary<FilePath, ProjectInfo> projectLookup)
        {
            WasCalled = true;
            return _result;
        }
    }
}
