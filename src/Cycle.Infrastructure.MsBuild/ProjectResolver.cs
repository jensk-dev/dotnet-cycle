using Cycle.Core;
using Microsoft.Build.Definition;
using Microsoft.Build.Evaluation;
using Microsoft.Extensions.Logging;

using MsbProject = Microsoft.Build.Evaluation.Project;

namespace Cycle.Infrastructure.MsBuild;

public sealed partial class ProjectResolver(
    ISolutionReader solutionReader,
    ILoggerFactory loggerFactory)
    : IProjectResolver
{
    private readonly ILogger<ProjectResolver> _logger = loggerFactory.CreateLogger<ProjectResolver>();

    public async Task<IReadOnlyList<ProjectInfo>> ResolveAffectedProjectsAsync(
        string solutionPath,
        IReadOnlyList<FilePath> changedFiles,
        CancellationToken ct)
    {
        var solutionProjects = await solutionReader.GetProjectsAsync(solutionPath, ct);
        var projectFilePaths = solutionProjects.Select(p => p.FilePath).ToHashSet();

        using var phantomFiles = new PhantomFileManager(loggerFactory.CreateLogger<PhantomFileManager>());
        phantomFiles.CreatePhantomFiles(changedFiles, projectFilePaths);

        using var projectCollection = new ProjectCollection();
        var loadedProjects = LoadProjects(solutionProjects, projectCollection, ct);
        var reverseMap = BuildReverseProjectMap(loadedProjects, ct);

        var affected = new Dictionary<FilePath, ProjectInfo>();
        var projectLookup = loadedProjects.ToDictionary(p => p.Info.FilePath, p => p.Info);

        // Projects that fail to load are always added to the output to prevent regression from silently passing CI.
        foreach (var loaded in loadedProjects.Where(p => p.MsbProject is null))
        {
            affected.TryAdd(loaded.Info.FilePath, loaded.Info);
        }

        // Find directly affected projects and compute transitive closure
        foreach (var changedFile in changedFiles)
        {
            ct.ThrowIfCancellationRequested();
            FindAffectedProjects(changedFile, loadedProjects, reverseMap, projectLookup, affected);
        }

        return affected.Values.ToList();
    }

    private static void FindAffectedProjects(
        FilePath changedFile,
        List<LoadedProject> loadedProjects,
        Dictionary<FilePath, HashSet<FilePath>> reverseMap,
        Dictionary<FilePath, ProjectInfo> projectLookup,
        Dictionary<FilePath, ProjectInfo> affected)
    {
        var directlyAffected = new HashSet<FilePath>();

        foreach (var loaded in loadedProjects)
        {
            if (loaded.ResolvedItemPaths is null)
            {
                continue;
            }

            if (!loaded.ResolvedItemPaths.Contains(changedFile.FullPath)
                && !loaded.ImportPaths!.Contains(changedFile.FullPath))
            {
                continue;
            }

            directlyAffected.Add(loaded.Info.FilePath);
            affected.TryAdd(loaded.Info.FilePath, loaded.Info);
        }

        var queue = new Queue<FilePath>(directlyAffected);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();

            if (!reverseMap.TryGetValue(current, out var dependents))
            {
                continue;
            }

            foreach (var dependent in dependents)
            {
                if (affected.ContainsKey(dependent))
                {
                    continue;
                }

                if (!projectLookup.TryGetValue(dependent, out var info))
                {
                    continue;
                }

                affected.TryAdd(dependent, info);
                queue.Enqueue(dependent);
            }
        }
    }

    private static HashSet<string> CollectResolvedItemPaths(MsbProject project)
    {
        var paths = new HashSet<string>(FilePath.PathComparer);
        var projectDir = Path.GetDirectoryName(project.FullPath)!;

        paths.Add(project.FullPath);

        foreach (var item in project.AllEvaluatedItems)
        {
            var resolvedPath = Path.IsPathRooted(item.EvaluatedInclude)
                ? Path.GetFullPath(item.EvaluatedInclude)
                : Path.GetFullPath(Path.Combine(projectDir, item.EvaluatedInclude));

            paths.Add(resolvedPath);
        }

        return paths;
    }

    private static HashSet<string> CollectImportPaths(MsbProject project)
    {
        var paths = new HashSet<string>(FilePath.PathComparer);

        foreach (var import in project.Imports)
        {
            paths.Add(import.ImportedProject.FullPath);
        }

        return paths;
    }

    // todo: find a better solution. The current one is brittle. Furthermore, how do we deal
    // with conditionally referenced projects. e.g. if an item is only included for net472,
    // should only the net472 enabled project references be used. Or is this unnecessary
    private static HashSet<string> CollectMultiTfmResolvedItemPaths(MsbProject project)
    {
        var paths = new HashSet<string>(FilePath.PathComparer);
        var projectDir = Path.GetDirectoryName(project.FullPath)!;

        paths.Add(project.FullPath);

        var targetFrameworksProp = project.GetProperty("TargetFrameworks");
        var targetFrameworkProp = project.GetProperty("TargetFramework");

        var frameworks = targetFrameworksProp?.EvaluatedValue
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();

        if (frameworks is null || frameworks.Count == 0)
        {
            var singleTfm = targetFrameworkProp?.EvaluatedValue;
            frameworks = string.IsNullOrEmpty(singleTfm) ? [] : [singleTfm];
        }

        var originalTfmsValue = targetFrameworksProp?.EvaluatedValue;
        var originalTfmValue = targetFrameworkProp?.EvaluatedValue;

        try
        {
            if (targetFrameworksProp is not null)
            {
                project.RemoveProperty(targetFrameworksProp);
            }

            foreach (var framework in frameworks)
            {
                project.SetProperty("TargetFramework", framework);
                project.ReevaluateIfNecessary();

                foreach (var item in project.AllEvaluatedItems)
                {
                    var resolvedPath = Path.IsPathRooted(item.EvaluatedInclude)
                        ? Path.GetFullPath(item.EvaluatedInclude)
                        : Path.GetFullPath(Path.Combine(projectDir, item.EvaluatedInclude));

                    paths.Add(resolvedPath);
                }
            }
        }
        finally
        {
            if (originalTfmsValue is not null)
            {
                project.SetProperty("TargetFrameworks", originalTfmsValue);
                var tfmProp = project.GetProperty("TargetFramework");
                if (tfmProp is not null)
                {
                    project.RemoveProperty(tfmProp);
                }
            }
            else if (originalTfmValue is not null)
            {
                project.SetProperty("TargetFramework", originalTfmValue);
            }

            project.ReevaluateIfNecessary();
        }

        return paths;
    }

    private List<LoadedProject> LoadProjects(
        IReadOnlyList<ProjectInfo> solutionProjects,
        ProjectCollection projectCollection,
        CancellationToken ct)
    {
        var results = new List<LoadedProject>(solutionProjects.Count);

        foreach (var projectInfo in solutionProjects)
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                var msbProject = MsbProject.FromFile(projectInfo.FilePath.FullPath, new ProjectOptions
                {
                    ProjectCollection = projectCollection,
                });

                var importPaths = CollectImportPaths(msbProject);

                var targetFrameworksProp = msbProject.GetProperty("TargetFrameworks");
                var resolvedItemPaths = targetFrameworksProp is not null
                    ? CollectMultiTfmResolvedItemPaths(msbProject)
                    : CollectResolvedItemPaths(msbProject);

                results.Add(new LoadedProject(projectInfo, msbProject, resolvedItemPaths, importPaths));

                LogProjectLoaded(projectInfo.FilePath.FullPath);
            }
            catch (Exception ex)
            {
                results.Add(new LoadedProject(projectInfo, null, null, null));
                LogProjectLoadFailed(projectInfo.FilePath.FullPath, ex);
            }
        }

        return results;
    }

    private Dictionary<FilePath, HashSet<FilePath>> BuildReverseProjectMap(
        List<LoadedProject> loadedProjects,
        CancellationToken ct)
    {
        var projectsByPath = loadedProjects
            .Where(p => p.MsbProject is not null)
            .ToDictionary(p => p.Info.FilePath, p => p);

        var reverseMap = new Dictionary<FilePath, HashSet<FilePath>>();

        foreach (var (info, msbProject, _, _) in projectsByPath.Values)
        {
            ct.ThrowIfCancellationRequested();

            if (msbProject is null)
            {
                continue;
            }

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

    private sealed record LoadedProject(
        ProjectInfo Info,
        MsbProject? MsbProject,
        HashSet<string>? ResolvedItemPaths,
        HashSet<string>? ImportPaths);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Loaded project {ProjectPath}")]
    private partial void LogProjectLoaded(string projectPath);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to load project {ProjectPath}. It will be treated as affected")]
    private partial void LogProjectLoadFailed(string projectPath, Exception ex);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Project {ReferencePath} is referenced by {ProjectPath} but was not found in the solution")]
    private partial void LogProjectReferenceNotFound(string referencePath, string projectPath);
}
