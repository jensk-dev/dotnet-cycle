using System.Collections.Immutable;

namespace Cycle.Core;

public record ProjectInfo(
    string Name,
    FilePath FilePath,
    ProjectType Type,
    ImmutableDictionary<string, string>? Properties = null);
