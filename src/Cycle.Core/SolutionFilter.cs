namespace Cycle.Core;

/// <summary>
/// Represents the content of a solution filter (.slnf) file.
/// </summary>
/// <param name="SolutionPath">Relative path from the .slnf file location to the solution file.</param>
/// <param name="Projects">Project paths relative to the solution directory.</param>
public sealed record SolutionFilter(string SolutionPath, IReadOnlyList<string> Projects);
