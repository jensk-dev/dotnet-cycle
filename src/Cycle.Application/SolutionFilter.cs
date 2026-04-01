namespace Cycle.Application;

public sealed record SolutionFilter(string SolutionPath, IReadOnlyList<string> Projects);
