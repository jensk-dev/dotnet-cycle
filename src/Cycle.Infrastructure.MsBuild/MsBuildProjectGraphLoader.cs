using Cycle.Core;
using Microsoft.Build.Definition;
using Microsoft.Build.Evaluation;
using Microsoft.Extensions.Logging;

using MsbProject = Microsoft.Build.Evaluation.Project;

namespace Cycle.Infrastructure.MsBuild;

public sealed partial class MsBuildProjectGraphLoader(
    ILoggerFactory loggerFactory)
    : IProjectGraphLoader
{
    private readonly ILogger<MsBuildProjectGraphLoader> _logger = loggerFactory.CreateLogger<MsBuildProjectGraphLoader>();

    public ProjectGraph Load(
        IReadOnlyList<ProjectInfo> projects,
        IReadOnlyList<FilePath> changedFiles,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var projectFilePaths = projects.Select(p => p.FilePath).ToHashSet();

        using var phantomFiles = new PhantomFileManager(loggerFactory.CreateLogger<PhantomFileManager>());
        phantomFiles.CreatePhantomFiles(changedFiles, projectFilePaths);

        using var projectCollection = new ProjectCollection();
        var loadedProjects = LoadProjects(projects, projectCollection, ct);
        var (reverseMap, forwardMap) = BuildDependencyMaps(loadedProjects, ct);

        var projectData = loadedProjects
            .Select(p => new LoadedProjectData(p.Info, p.ResolvedItemPaths, p.ImportPaths))
            .ToList();

        var projectLookup = projectData.ToDictionary(p => p.Info.FilePath, p => p.Info);

        return new ProjectGraph(
            projectData,
            forwardMap.ToDictionary(kvp => kvp.Key, kvp => (IReadOnlySet<FilePath>)kvp.Value),
            reverseMap.ToDictionary(kvp => kvp.Key, kvp => (IReadOnlySet<FilePath>)kvp.Value),
            projectLookup);
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
                LogProjectLoadFailed(projectInfo.FilePath.FullPath, ex.Message);
                LogProjectLoadFailedDetails(projectInfo.FilePath.FullPath, ex);
            }
        }

        return results;
    }

    private (Dictionary<FilePath, HashSet<FilePath>> ReverseMap, Dictionary<FilePath, HashSet<FilePath>> ForwardMap)
        BuildDependencyMaps(List<LoadedProject> loadedProjects, CancellationToken ct)
    {
        var projectsByPath = loadedProjects
            .Where(p => p.MsbProject is not null)
            .ToDictionary(p => p.Info.FilePath, p => p);

        var reverseMap = new Dictionary<FilePath, HashSet<FilePath>>();
        var forwardMap = new Dictionary<FilePath, HashSet<FilePath>>();

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

                // Forward map: project → its dependencies (always recorded for closure)
                if (!forwardMap.TryGetValue(info.FilePath, out var dependencies))
                {
                    dependencies = [];
                    forwardMap[info.FilePath] = dependencies;
                }

                dependencies.Add(referencedPath.Value);

                if (!projectsByPath.ContainsKey(referencedPath.Value))
                {
                    LogProjectReferenceNotFound(referencedPath.Value.FullPath, info.FilePath.FullPath);
                    continue;
                }

                // Reverse map: dependency → dependents (only for in-solution projects)
                if (!reverseMap.TryGetValue(referencedPath.Value, out var dependents))
                {
                    dependents = [];
                    reverseMap[referencedPath.Value] = dependents;
                }

                dependents.Add(info.FilePath);
            }
        }

        return (reverseMap, forwardMap);
    }

    private sealed record LoadedProject(
        ProjectInfo Info,
        MsbProject? MsbProject,
        HashSet<string>? ResolvedItemPaths,
        HashSet<string>? ImportPaths);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Loaded project {ProjectPath}")]
    private partial void LogProjectLoaded(string projectPath);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to load project {ProjectPath}: {Reason}. It will be treated as affected")]
    private partial void LogProjectLoadFailed(string projectPath, string reason);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Failed to load project {ProjectPath}")]
    private partial void LogProjectLoadFailedDetails(string projectPath, Exception ex);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Project {ReferencePath} is referenced by {ProjectPath} but was not found in the solution")]
    private partial void LogProjectReferenceNotFound(string referencePath, string projectPath);
}
