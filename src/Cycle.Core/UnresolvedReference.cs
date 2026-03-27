namespace Cycle.Core;

public sealed record UnresolvedReference(FilePath ReferencedBy, FilePath ReferencePath);
