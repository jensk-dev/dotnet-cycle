using Cycle.Core;

namespace Cycle.Application;

public sealed class GenerateSolutionFilterUseCase
{
    private readonly ISolutionReader _solutionReader;
    private readonly IProjectGraphLoader _graphLoader;
    private readonly ISolutionFilterWriter _filterWriter;

    public GenerateSolutionFilterUseCase(
        ISolutionReader solutionReader,
        IProjectGraphLoader graphLoader,
        ISolutionFilterWriter filterWriter)
    {
        _solutionReader = solutionReader;
        _graphLoader = graphLoader;
        _filterWriter = filterWriter;
    }

    public async Task<GenerateSolutionFilterResult> ExecuteAsync(
        SolutionPath solutionPath,
        IReadOnlyList<FilePath> changedFiles,
        bool includeClosure,
        string outputDirectory,
        TextWriter output,
        CancellationToken ct)
    {
        var projects = await _solutionReader.GetProjectsAsync(solutionPath, ct);
        var graph = _graphLoader.Load(projects, changedFiles, ct);

        var affectedResult = AffectedProjectsResolver.Resolve(
            graph.Projects, graph.ReverseDependencyMap, changedFiles);

        IReadOnlyList<ProjectInfo> includedProjects;
        IReadOnlyList<UnresolvedReference> unresolvedReferences;

        if (includeClosure)
        {
            var closure = DependencyClosureResolver.Resolve(
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

        await _filterWriter.WriteAsync(filter, output, ct);

        return new GenerateSolutionFilterResult(
            filter,
            includedProjects,
            graph.Projects.Count,
            affectedResult.FailedToLoadProjects.Count,
            unresolvedReferences);
    }
}
