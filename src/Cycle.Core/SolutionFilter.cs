namespace Cycle.Core;

public sealed record SolutionFilter(string SolutionPath, IReadOnlyList<string> Projects);
