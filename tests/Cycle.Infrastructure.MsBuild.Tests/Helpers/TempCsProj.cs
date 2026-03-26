namespace Cycle.Infrastructure.MsBuild.Tests.Helpers;

public sealed class TempCsProj : IDisposable
{
    private const string DefaultCsProjContent = """
                                                <Project Sdk="Microsoft.NET.Sdk">
                                                  <PropertyGroup>
                                                  </PropertyGroup>
                                                </Project>
                                                """;

    private readonly HashSet<string> _createdPaths = [];
    private bool _created;
    private bool _disposed;

    public TempCsProj(string projectDirectory, string projectName)
    {
        ProjectDirectory = projectDirectory;
        ProjectFilePath = Path.Combine(ProjectDirectory, $"{projectName}.csproj");
    }

    public string ProjectDirectory { get; }

    public string ProjectFilePath { get; }

    public void Create()
    {
        _created = true;
        Directory.CreateDirectory(ProjectDirectory);

        if (File.Exists(ProjectFilePath))
            throw new InvalidOperationException($"File {ProjectFilePath} already exists");

        File.WriteAllText(ProjectFilePath, DefaultCsProjContent);
        _createdPaths.Add(ProjectFilePath);
    }

    public (string canonicalFilePath, string entryFilePath) AddFileToProject(
        string filePath,
        ProjectItemType itemType,
        bool isAbsolute,
        string? content = null)
    {
        var absolutePath = AddFile(filePath, isAbsolute, content);
        var entryPath = isAbsolute
            ? absolutePath
            : Path.GetRelativePath(ProjectDirectory, absolutePath);

        AddEntryToCsProj(entryPath, itemType);

        return (absolutePath, entryPath);
    }

    public string AddFile(string filePath, bool isAbsolute, string? content = null)
    {
        if (!_created)
            throw new InvalidOperationException("Project has not been created yet");

        var path = isAbsolute
            ? Path.GetFullPath(filePath)
            : Path.GetFullPath(Path.Combine(ProjectDirectory, filePath));

        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content ?? $"{Guid.NewGuid()}");
        _createdPaths.Add(path);

        return path;
    }

    public void AddEntryToCsProj(string entry, ProjectItemType itemType)
    {
        var csProjContent = File.ReadAllText(ProjectFilePath);

        var itemKey = itemType switch
        {
            ProjectItemType.Compile => "Compile",
            ProjectItemType.Content => "Content",
            ProjectItemType.None => "None",
            ProjectItemType.EmbeddedResource => "EmbeddedResource",
            _ => throw new ArgumentOutOfRangeException(nameof(itemType), itemType, null),
        };

        var newItemGroup = $"""
                            <ItemGroup>
                              <{itemKey} Include="{entry}" />
                            </ItemGroup>
                            """;

        csProjContent = csProjContent.Replace("</Project>", $"{newItemGroup}\n</Project>");
        File.WriteAllText(ProjectFilePath, csProjContent);
    }

    public void SetTargetFrameworks(string frameworks)
    {
        var csProjContent = File.ReadAllText(ProjectFilePath);
        csProjContent = csProjContent.Replace(
            "<PropertyGroup>",
            $"<PropertyGroup>\n    <TargetFrameworks>{frameworks}</TargetFrameworks>");
        File.WriteAllText(ProjectFilePath, csProjContent);
    }

    public string AddImport(string relativePath)
    {
        var absolutePath = Path.GetFullPath(Path.Combine(ProjectDirectory, relativePath));
        var csProjContent = File.ReadAllText(ProjectFilePath);
        csProjContent = csProjContent.Replace(
            "</Project>",
            $"<Import Project=\"{relativePath}\" />\n</Project>");
        File.WriteAllText(ProjectFilePath, csProjContent);
        return absolutePath;
    }

    public void CreateWithContent(string csprojContent)
    {
        _created = true;
        Directory.CreateDirectory(ProjectDirectory);

        if (File.Exists(ProjectFilePath))
            throw new InvalidOperationException($"File {ProjectFilePath} already exists");

        File.WriteAllText(ProjectFilePath, csprojContent);
        _createdPaths.Add(ProjectFilePath);
    }

    public void AddProjectReference(TempCsProj referenceToAdd)
    {
        var csProjContent = File.ReadAllText(ProjectFilePath);

        var relativePath = Path.GetRelativePath(ProjectDirectory, referenceToAdd.ProjectFilePath);

        var projectReference = $"""
                                <ItemGroup>
                                  <ProjectReference Include="{relativePath}" />
                                </ItemGroup>
                                """;

        csProjContent = csProjContent.Replace("</Project>", $"{projectReference}\n</Project>");
        File.WriteAllText(ProjectFilePath, csProjContent);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        foreach (var path in _createdPaths)
        {
            if (Directory.Exists(path))
                Directory.Delete(path, true);
            else if (File.Exists(path))
                File.Delete(path);
        }

        if (Directory.Exists(ProjectDirectory))
            Directory.Delete(ProjectDirectory, true);

        _disposed = true;
    }
}
