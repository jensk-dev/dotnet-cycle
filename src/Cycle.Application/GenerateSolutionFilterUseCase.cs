using Cycle.Core;

namespace Cycle.Application;

public sealed class GenerateSolutionFilterUseCase
{
    private readonly IProjectResolver _resolver;
    private readonly IDependencyClosureResolver _closureResolver;

    public GenerateSolutionFilterUseCase(
        IProjectResolver resolver,
        IDependencyClosureResolver closureResolver)
    {
        _resolver = resolver;
        _closureResolver = closureResolver;
    }

    public async Task<GenerateSolutionFilterResult> ExecuteAsync(
        SolutionPath solutionPath,
        IReadOnlyList<FilePath> changedFiles,
        bool includeClosure,
        string outputDirectory,
        CancellationToken ct)
    {
        var resolution = await _resolver.ResolveAffectedProjectsAsync(
            solutionPath, changedFiles, ct);

        IReadOnlyList<ProjectInfo> includedProjects;
        IReadOnlyList<UnresolvedReference> unresolvedReferences;

        if (includeClosure)
        {
            var closure = _closureResolver.Resolve(
                resolution.AffectedProjects,
                resolution.ForwardDependencyMap,
                resolution.ProjectLookup);
            includedProjects = closure.Projects;
            unresolvedReferences = closure.UnresolvedReferences;
        }
        else
        {
            includedProjects = resolution.AffectedProjects.Values.ToList();
            unresolvedReferences = [];
        }

        var filter = SolutionFilterBuilder.Build(
            solutionPath, outputDirectory, includedProjects);

        return new GenerateSolutionFilterResult(
            filter,
            includedProjects,
            resolution.TotalProjectCount,
            resolution.FailedProjectCount,
            unresolvedReferences);
    }
}
