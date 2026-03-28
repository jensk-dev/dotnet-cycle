using System.CommandLine;
using Cycle.Application;
using Cycle.Core;
using Cycle.Infrastructure.MsBuild;
using Microsoft.Extensions.Logging;

namespace Cycle.Presentation.Cli;

public static partial class Program
{
    public static async Task<int> Main(string[] args)
    {
        var solutionArg = new Argument<FileInfo>("solution-path")
        {
            Description = "Path to the solution file (.sln or .slnx)",
        };

        var changedFilesOption = new Option<FileInfo?>("--changed-files")
        {
            Description = "Path to a file containing changed file paths (one per line)",
        };

        var outputFileArg = new Argument<FileInfo>("output-file")
        {
            Description = "Path to write the solution filter (.slnf)",
        };

        var logLevelOption = new Option<string>("--log-level")
        {
            Description = "Log verbosity: quiet, minimal, normal, verbose",
            DefaultValueFactory = _ => "minimal",
        };
        logLevelOption.AcceptOnlyFromAmong("quiet", "minimal", "normal", "verbose");

        var noClosureOption = new Option<bool>("--no-closure")
        {
            Description = "Exclude transitive build dependencies (ProjectReferences) from the filter",
        };

        var rootCommand = new RootCommand("Generates a solution filter (.slnf) from a list of changed files")
        {
            solutionArg,
            outputFileArg,
            changedFilesOption,
            logLevelOption,
            noClosureOption,
        };

        rootCommand.SetAction(async (parseResult, ct) =>
        {
            var solutionFile = parseResult.GetValue(solutionArg)!;
            var changedFilesFile = parseResult.GetValue(changedFilesOption);
            var outputFile = parseResult.GetValue(outputFileArg)!;
            var logLevel = parseResult.GetValue(logLevelOption) ?? "minimal";
            var includeClosure = !parseResult.GetValue(noClosureOption);

            return await RunAsync(
                solutionFile,
                changedFilesFile,
                outputFile,
                logLevel,
                includeClosure,
                ct);
        });

        var parseResult = rootCommand.Parse(args);
        return await parseResult.InvokeAsync();
    }

    internal static async Task<int> RunAsync(
        FileInfo solutionFile,
        FileInfo? changedFilesFile,
        FileInfo outputFile,
        string logLevel,
        bool includeClosure,
        CancellationToken ct)
    {
        using var loggerFactory = CreateLoggerFactory(logLevel);
        var logger = loggerFactory.CreateLogger(nameof(Program));

        try
        {
            MsBuildBootstrap.Initialize();
            var changedFiles = await ReadChangedFilesAsync(changedFilesFile, Console.In, Console.IsInputRedirected, logger, ct);

            var reader = new MsBuildSolutionReader();
            var affectedResolver = new AffectedProjectsResolver();
            var closureResolver = new DependencyClosureResolver();
            var resolver = new ProjectResolver(reader, affectedResolver, loggerFactory);

            var useCase = new GenerateSolutionFilterUseCase(resolver, closureResolver);

            var solutionPath = SolutionPath.FromString(solutionFile.FullName);
            var outputDir = Path.GetDirectoryName(Path.GetFullPath(outputFile.FullName))!;
            if (!Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }

            var result = await useCase.ExecuteAsync(
                solutionPath, changedFiles, includeClosure, outputDir, ct);

            await using var writer = new StreamWriter(outputFile.FullName);
            var filterWriter = new SolutionFilterWriter();
            await filterWriter.WriteAsync(result.Filter, writer, ct);

            foreach (var unresolved in result.UnresolvedReferences)
            {
                LogUnresolvedReference(logger, unresolved.ReferencePath.FullPath, unresolved.ReferencedBy.FullPath);
            }

            var included = result.IncludedProjects.Count;
            var filteredOut = result.TotalProjectCount - included;
            var failed = result.FailedProjectCount;
            await Console.Error.WriteLineAsync(
                $"Solution has {result.TotalProjectCount} projects, filter includes {included} ({failed} failed to load), filtered out {filteredOut}");

            return 0;
        }
        catch (OperationCanceledException)
        {
            return 1;
        }
        catch (Exception ex)
        {
            LogCycleFailed(logger, ex);
            return 1;
        }
    }

    internal static async Task<IReadOnlyList<FilePath>> ReadChangedFilesAsync(
        FileInfo? changedFilesFile,
        TextReader stdinReader,
        bool isInputRedirected,
        ILogger logger,
        CancellationToken ct)
    {
        IEnumerable<string> lines;

        if (changedFilesFile is not null)
        {
            lines = await File.ReadAllLinesAsync(changedFilesFile.FullName, ct);
        }
        else if (isInputRedirected)
        {
            var stdinLines = new List<string>();
            while (await stdinReader.ReadLineAsync(ct) is { } line)
            {
                stdinLines.Add(line);
            }

            lines = stdinLines;
        }
        else
        {
            return [];
        }

        var results = new List<FilePath>();
        foreach (var raw in lines)
        {
            var trimmed = raw.Trim();
            if (trimmed.Length == 0)
            {
                continue;
            }

            if (FilePath.TryFromString(trimmed, out var fp))
            {
                results.Add(fp.Value);
            }
            else
            {
                LogSkippingInvalidPath(logger, trimmed);
            }
        }

        return results;
    }

    internal static ILoggerFactory CreateLoggerFactory(string logLevel)
    {
        var level = logLevel switch
        {
            "quiet" => LogLevel.None,
            "minimal" => LogLevel.Warning,
            "normal" => LogLevel.Information,
            "verbose" => LogLevel.Debug,
            _ => LogLevel.Warning,
        };

        return LoggerFactory.Create(builder =>
        {
            builder.SetMinimumLevel(level);
            builder.AddSimpleConsole(options =>
            {
                options.SingleLine = true;
                options.UseUtcTimestamp = false;
            });
        });
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Skipping invalid path: {Path}")]
    private static partial void LogSkippingInvalidPath(ILogger logger, string path);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Unresolved project reference {ReferencePath} referenced by {ProjectPath}")]
    private static partial void LogUnresolvedReference(ILogger logger, string referencePath, string projectPath);

    [LoggerMessage(Level = LogLevel.Error, Message = "Cycle failed")]
    private static partial void LogCycleFailed(ILogger logger, Exception ex);
}
