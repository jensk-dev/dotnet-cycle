using System.CommandLine;
using Cycle.Core;
using Cycle.Core.Export;
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

        var stdinOption = new Option<bool>("--stdin")
        {
            Description = "Read changed file paths from stdin (one per line)",
        };

        var outputOption = new Option<string>("--output")
        {
            Description = "Output format: json, build, or paths",
            DefaultValueFactory = _ => "json",
        };
        outputOption.AcceptOnlyFromAmong("json", "build", "paths");

        var outputFileOption = new Option<FileInfo?>("--output-file")
        {
            Description = "Write output to a file instead of stdout",
        };

        var includeOption = new Option<string[]>("--include")
        {
            Description = "Project types to include (csproj, fsproj, vbproj, sqlproj, dtproj, proj)",
            AllowMultipleArgumentsPerToken = true,
        };

        var includePropertyOption = new Option<string[]>("--include-property")
        {
            Description = "MSBuild properties to include in output",
            AllowMultipleArgumentsPerToken = true,
        };

        var logLevelOption = new Option<string>("--log-level")
        {
            Description = "Log verbosity: quiet, minimal, normal, verbose",
            DefaultValueFactory = _ => "minimal",
        };
        logLevelOption.AcceptOnlyFromAmong("quiet", "minimal", "normal", "verbose");

        var rootCommand = new RootCommand("Determines affected .NET projects from a list of changed files")
        {
            solutionArg,
            changedFilesOption,
            stdinOption,
            outputOption,
            outputFileOption,
            includeOption,
            includePropertyOption,
            logLevelOption,
        };

        rootCommand.SetAction(async (parseResult, ct) =>
        {
            var solutionFile = parseResult.GetValue(solutionArg)!;
            var changedFilesFile = parseResult.GetValue(changedFilesOption);
            var useStdin = parseResult.GetValue(stdinOption);
            var outputFormat = parseResult.GetValue(outputOption) ?? "json";
            var outputFile = parseResult.GetValue(outputFileOption);
            var includeTypes = parseResult.GetValue(includeOption);
            var includeProperties = parseResult.GetValue(includePropertyOption);
            var logLevel = parseResult.GetValue(logLevelOption) ?? "minimal";

            return await RunAsync(
                solutionFile,
                changedFilesFile,
                useStdin,
                outputFormat,
                outputFile,
                includeTypes,
                includeProperties,
                logLevel,
                ct);
        });

        var parseResult = rootCommand.Parse(args);
        return await parseResult.InvokeAsync();
    }

    private static async Task<int> RunAsync(
        FileInfo solutionFile,
        FileInfo? changedFilesFile,
        bool useStdin,
        string outputFormat,
        FileInfo? outputFile,
        string[]? includeTypes,
        string[]? includeProperties,
        string logLevel,
        CancellationToken ct)
    {
        using var loggerFactory = CreateLoggerFactory(logLevel);
        var logger = loggerFactory.CreateLogger<ProjectResolver>();

        try
        {
            MsBuildBootstrap.Initialize();

            var changedFiles = await ReadChangedFilesAsync(changedFilesFile, useStdin, ct);

            var includedProperties = includeProperties is { Length: > 0 }
                ? new HashSet<string>(includeProperties)
                : null;

            var reader = new MsBuildSolutionReader();
            var resolver = new ProjectResolver(reader, logger, includedProperties);

            var affected = await resolver.ResolveAffectedProjectsAsync(
                solutionFile.FullName, changedFiles, ct);

            // Filter by project type if specified
            if (includeTypes is { Length: > 0 })
            {
                var types = ParseProjectTypes(includeTypes);
                affected = affected.Where(p => types.Contains(p.Type)).ToList();
            }

            var exporter = CreateExporter(outputFormat);

            if (outputFile is not null)
            {
                var dir = outputFile.DirectoryName;
                if (dir is not null && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                await using var writer = new StreamWriter(outputFile.FullName);
                await exporter.ExportAsync(affected, writer, ct);
            }
            else
            {
                await exporter.ExportAsync(affected, Console.Out, ct);
            }

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

    private static async Task<IReadOnlyList<FilePath>> ReadChangedFilesAsync(
        FileInfo? changedFilesFile,
        bool useStdin,
        CancellationToken ct)
    {
        IEnumerable<string> lines;

        if (changedFilesFile is not null)
        {
            lines = await File.ReadAllLinesAsync(changedFilesFile.FullName, ct);
        }
        else if (useStdin || Console.IsInputRedirected)
        {
            var stdinLines = new List<string>();
            while (await Console.In.ReadLineAsync(ct) is { } line)
            {
                stdinLines.Add(line);
            }

            lines = stdinLines;
        }
        else
        {
            return [];
        }

        return lines
            .Select(l => l.Trim())
            .Where(l => l.Length > 0)
            .Select(l => FilePath.TryFromString(l, out var fp) ? fp.Value : (FilePath?)null)
            .Where(fp => fp is not null)
            .Select(fp => fp!.Value)
            .ToList();
    }

    private static HashSet<ProjectType> ParseProjectTypes(string[] types)
    {
        var result = new HashSet<ProjectType>();
        foreach (var t in types)
        {
            var ext = t.StartsWith('.') ? t : $".{t}";
            if (ProjectTypeExtensions.TryFromExtension(ext, out var pt))
                result.Add(pt);
        }

        return result;
    }

    private static IProjectExporter CreateExporter(string format) => format switch
    {
        "json" => new JsonExporter(),
        "build" => new BuildExporter(),
        "paths" => new PathsExporter(),
        _ => new JsonExporter(),
    };

    private static ILoggerFactory CreateLoggerFactory(string logLevel)
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

    [LoggerMessage(Level = LogLevel.Error, Message = "Cycle failed")]
    private static partial void LogCycleFailed(ILogger logger, Exception ex);
}
