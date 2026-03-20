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
        projectType = extension.ToLowerInvariant() switch
        {
            ".csproj" => ProjectType.CsProj,
            ".fsproj" => ProjectType.FsProj,
            ".vbproj" => ProjectType.VbProj,
            ".sqlproj" => ProjectType.SqlProj,
            ".dtproj" => ProjectType.DtProj,
            ".proj" => ProjectType.Proj,
            _ => (ProjectType)(-1),
        };

        return (int)projectType >= 0;
    }
}
