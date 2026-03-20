using System.Collections.Immutable;
using Cycle.Core;
using Microsoft.Build.Definition;
using Microsoft.Build.Evaluation;
using Microsoft.Extensions.Logging;

using MsbProject = Microsoft.Build.Evaluation.Project;

namespace Cycle.Infrastructure.MsBuild;

public sealed partial class ProjectResolver(
    ISolutionReader solutionReader,
    ILogger<ProjectResolver> logger,
    IReadOnlySet<string>? includedProperties = null)
    : IProjectResolver
{
    private static readonly string[] RelevantItemTypes =
    [
        "Compile",
        "Content",
        "None",
        "EmbeddedResource",
        "ProjectReference",
        "PackageReference",
        "Reference",
    ];

    private readonly IReadOnlySet<string> _includedProperties = includedProperties ?? ImmutableHashSet<string>.Empty;

    public async Task<IReadOnlyList<ProjectInfo>> ResolveAffectedProjectsAsync(
        string solutionPath,
        IReadOnlyList<FilePath> changedFiles,
        CancellationToken ct)
    {
        var solutionProjects = await solutionReader.GetProjectsAsync(solutionPath, ct);
        var projectFilePaths = solutionProjects.Select(p => p.FilePath).ToHashSet();

        using var phantomFiles = new PhantomFileManager(logger);
        phantomFiles.CreatePhantomFiles(changedFiles, projectFilePaths);

        using var projectCollection = new ProjectCollection();
        var loadedProjects = LoadProjects(solutionProjects, projectCollection);
        var reverseMap = BuildReverseProjectMap(loadedProjects);

        var affected = new Dictionary<FilePath, ProjectInfo>();

        // Include projects that failed to load as affected (regression detection)
        foreach (var (info, _) in loadedProjects.Where(p => p.MsbProject is null))
        {
            affected.TryAdd(info.FilePath, info);
        }

        // Find directly affected projects and compute transitive closure
        foreach (var changedFile in changedFiles)
        {
            ct.ThrowIfCancellationRequested();
            FindAffectedProjects(changedFile, loadedProjects, reverseMap, affected);
        }

        return affected.Values.ToList();
    }

    private static void FindAffectedProjects(
        FilePath changedFile,
        List<LoadedProject> loadedProjects,
        Dictionary<FilePath, HashSet<FilePath>> reverseMap,
        Dictionary<FilePath, ProjectInfo> affected)
    {
        // Step 1: Find directly affected projects
        var directlyAffected = new HashSet<FilePath>();

        foreach (var (info, msbProject) in loadedProjects)
        {
            if (msbProject is null)
                continue;

            if (!IsFileRelevantToProject(changedFile, msbProject))
                continue;

            directlyAffected.Add(info.FilePath);
            affected.TryAdd(info.FilePath, info);
        }

        // Step 2: BFS through reverse dependency graph for transitive closure
        var queue = new Queue<FilePath>(directlyAffected);
        var projectLookup = loadedProjects.ToDictionary(p => p.Info.FilePath, p => p.Info);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();

            if (!reverseMap.TryGetValue(current, out var dependents))
                continue;

            foreach (var dependent in dependents)
            {
                if (affected.ContainsKey(dependent))
                    continue;

                if (!projectLookup.TryGetValue(dependent, out var info))
                    continue;

                affected.TryAdd(dependent, info);
                queue.Enqueue(dependent);
            }
        }
    }

    private static bool IsFileRelevantToProject(FilePath filePath, MsbProject project)
    {
        // Direct project file match
        if (FilePathsEqual(project.FullPath, filePath.FullPath))
            return true;

        // Import match (catches .props/.targets changes)
        foreach (var import in project.Imports)
        {
            if (FilePathsEqual(import.ImportedProject.FullPath, filePath.FullPath))
                return true;
        }

        // Item match with multi-target framework support
        var targetFrameworksProp = project.GetProperty("TargetFrameworks");
        var targetFrameworkProp = project.GetProperty("TargetFramework");

        if (targetFrameworksProp is null && targetFrameworkProp is null)
            return IsFileInAnyProjectItem(filePath.FullPath, project);

        var frameworks = targetFrameworksProp?.EvaluatedValue
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();

        if (frameworks is null || frameworks.Count == 0)
        {
            var singleTfm = targetFrameworkProp?.EvaluatedValue;
            frameworks = string.IsNullOrEmpty(singleTfm) ? [] : [singleTfm];
        }

        if (targetFrameworksProp is not null)
            project.RemoveProperty(targetFrameworksProp);

        foreach (var framework in frameworks)
        {
            project.SetProperty("TargetFramework", framework);
            project.ReevaluateIfNecessary();

            if (IsFileInAnyProjectItem(filePath.FullPath, project))
                return true;
        }

        return false;
    }

    private static bool IsFileInAnyProjectItem(string filePath, MsbProject project)
    {
        var projectDir = Path.GetDirectoryName(project.FullPath)!;

        foreach (var itemType in RelevantItemTypes)
        {
            foreach (var item in project.GetItems(itemType))
            {
                var resolvedPath = Path.IsPathRooted(item.EvaluatedInclude)
                    ? Path.GetFullPath(item.EvaluatedInclude)
                    : Path.GetFullPath(Path.Combine(projectDir, item.EvaluatedInclude));

                if (FilePathsEqual(resolvedPath, filePath))
                    return true;
            }
        }

        return false;
    }

    private List<LoadedProject> LoadProjects(IReadOnlyList<ProjectInfo> solutionProjects, ProjectCollection projectCollection)
    {
        var results = new List<LoadedProject>(solutionProjects.Count);

        foreach (var projectInfo in solutionProjects)
        {
            try
            {
                var msbProject = MsbProject.FromFile(projectInfo.FilePath.FullPath, new ProjectOptions
                {
                    ProjectCollection = projectCollection,
                });

                var properties = ExtractProperties(msbProject);
                var enrichedInfo = projectInfo with { Properties = properties };
                results.Add(new LoadedProject(enrichedInfo, msbProject));

                LogProjectLoaded(projectInfo.FilePath.FullPath);
            }
            catch (Exception ex)
            {
                results.Add(new LoadedProject(projectInfo, null));
                LogProjectLoadFailed(projectInfo.FilePath.FullPath, ex);
            }
        }

        return results;
    }

    private ImmutableDictionary<string, string>? ExtractProperties(MsbProject msbProject)
    {
        if (_includedProperties.Count == 0)
            return null;

        var builder = ImmutableDictionary.CreateBuilder<string, string>();

        foreach (var propertyName in _includedProperties)
        {
            var property = msbProject.GetProperty(propertyName);
            if (property is not null)
                builder.Add(property.Name, property.EvaluatedValue);
        }

        return builder.Count > 0 ? builder.ToImmutable() : null;
    }

    private Dictionary<FilePath, HashSet<FilePath>> BuildReverseProjectMap(List<LoadedProject> loadedProjects)
    {
        var projectsByPath = loadedProjects
            .Where(p => p.MsbProject is not null)
            .ToDictionary(p => p.Info.FilePath, p => p);

        var reverseMap = new Dictionary<FilePath, HashSet<FilePath>>();

        foreach (var (info, msbProject) in projectsByPath.Values)
        {
            if (msbProject is null)
                continue;

            foreach (var item in msbProject.GetItems("ProjectReference"))
            {
                if (!FilePath.TryFromCombinedStrings(
                        info.FilePath.DirectoryName,
                        item.EvaluatedInclude,
                        out var referencedPath))
                {
                    continue;
                }

                if (!projectsByPath.ContainsKey(referencedPath.Value))
                {
                    LogProjectReferenceNotFound(referencedPath.Value.FullPath, info.FilePath.FullPath);
                    continue;
                }

                if (!reverseMap.TryGetValue(referencedPath.Value, out var dependents))
                {
                    dependents = [];
                    reverseMap[referencedPath.Value] = dependents;
                }

                dependents.Add(info.FilePath);
            }
        }

        return reverseMap;
    }

    private static bool FilePathsEqual(string path1, string path2) =>
        string.Equals(path1, path2, FilePath.PathComparison);

    private sealed record LoadedProject(ProjectInfo Info, MsbProject? MsbProject);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Loaded project {ProjectPath}")]
    private partial void LogProjectLoaded(string projectPath);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to load project {ProjectPath}. It will be treated as affected")]
    private partial void LogProjectLoadFailed(string projectPath, Exception ex);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Project {ReferencePath} is referenced by {ProjectPath} but was not found in the solution")]
    private partial void LogProjectReferenceNotFound(string referencePath, string projectPath);
}
