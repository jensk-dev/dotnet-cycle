using Cycle.Core;

namespace Cycle.Application;

public sealed class GenerateSolutionFilterUseCase
{
    private readonly IProjectGraphLoader _graphLoader;
    private readonly IAffectedProjectsResolver _affectedResolver;
    private readonly IDependencyClosureResolver _closureResolver;

    public GenerateSolutionFilterUseCase(
        IProjectGraphLoader graphLoader,
        IAffectedProjectsResolver affectedResolver,
        IDependencyClosureResolver closureResolver)
    {
        _graphLoader = graphLoader;
        _affectedResolver = affectedResolver;
        _closureResolver = closureResolver;
    }

    public async Task<GenerateSolutionFilterResult> ExecuteAsync(
        SolutionPath solutionPath,
        IReadOnlyList<FilePath> changedFiles,
        bool includeClosure,
        string outputDirectory,
        CancellationToken ct)
    {
        var graph = await _graphLoader.LoadAsync(solutionPath, changedFiles, ct);

        var affectedResult = _affectedResolver.Resolve(
            graph.Projects, graph.ReverseDependencyMap, changedFiles);

        IReadOnlyList<ProjectInfo> includedProjects;
        IReadOnlyList<UnresolvedReference> unresolvedReferences;

        if (includeClosure)
        {
            var closure = _closureResolver.Resolve(
                affectedResult.AffectedProjects,
                graph.ForwardDependencyMap,
                graph.ProjectLookup);
            includedProjects = closure.Projects;
            unresolvedReferences = closure.UnresolvedReferences;
        }
        else
        {
            includedProjects = affectedResult.AffectedProjects.Values.ToList();
            unresolvedReferences = [];
        }

        var filter = SolutionFilterBuilder.Build(
            solutionPath, outputDirectory, includedProjects);

        return new GenerateSolutionFilterResult(
            filter,
            includedProjects,
            graph.Projects.Count,
            affectedResult.FailedToLoadProjects.Count,
            unresolvedReferences);
    }
}
