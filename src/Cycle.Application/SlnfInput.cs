using Cycle.Core;

namespace Cycle.Application;

public sealed record SlnfInput(SolutionPath ParentSolution, IReadOnlySet<FilePath> ProjectScope);
