namespace Cycle.Core;

public enum ProjectType
{
    CsProj,
    FsProj,
    VbProj,
    SqlProj,
    DtProj,
    Proj,
}

public static class ProjectTypeExtensions
{
    public static string ToFileGlob(this ProjectType projectType) => projectType switch
    {
        ProjectType.CsProj => "*.csproj",
        ProjectType.FsProj => "*.fsproj",
        ProjectType.VbProj => "*.vbproj",
        ProjectType.SqlProj => "*.sqlproj",
        ProjectType.DtProj => "*.dtproj",
        ProjectType.Proj => "*.proj",
        _ => throw new ArgumentOutOfRangeException(nameof(projectType), projectType, null),
    };

    public static string ToFileExtension(this ProjectType projectType) => projectType switch
    {
        ProjectType.CsProj => ".csproj",
        ProjectType.FsProj => ".fsproj",
        ProjectType.VbProj => ".vbproj",
        ProjectType.SqlProj => ".sqlproj",
        ProjectType.DtProj => ".dtproj",
        ProjectType.Proj => ".proj",
        _ => throw new ArgumentOutOfRangeException(nameof(projectType), projectType, null),
    };

    public static bool TryFromExtension(string extension, out ProjectType projectType)
    {
        var result = extension.ToLowerInvariant() switch
        {
            ".csproj" => (ProjectType?)ProjectType.CsProj,
            ".fsproj" => (ProjectType?)ProjectType.FsProj,
            ".vbproj" => (ProjectType?)ProjectType.VbProj,
            ".sqlproj" => (ProjectType?)ProjectType.SqlProj,
            ".dtproj" => (ProjectType?)ProjectType.DtProj,
            ".proj" => (ProjectType?)ProjectType.Proj,
            _ => null,
        };

        projectType = result ?? default;
        return result.HasValue;
    }
}
