using Cycle.Core;

namespace Cycle.Application;

public sealed class GenerateSolutionFilterUseCase
{
    private readonly IProjectResolver _resolver;

    public GenerateSolutionFilterUseCase(IProjectResolver resolver)
    {
        _resolver = resolver;
    }

    public async Task<GenerateSolutionFilterResult> ExecuteAsync(
        SolutionPath solutionPath,
        IReadOnlyList<FilePath> changedFiles,
        bool includeClosure,
        string outputDirectory,
        CancellationToken ct)
    {
        var resolution = await _resolver.ResolveAffectedProjectsAsync(
            solutionPath, changedFiles, includeClosure, ct);

        var filter = SolutionFilterBuilder.Build(
            solutionPath, outputDirectory, resolution.AffectedProjects);

        return new GenerateSolutionFilterResult(filter, resolution);
    }
}
