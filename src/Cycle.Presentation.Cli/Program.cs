using System.CommandLine;
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

        var rootCommand = new RootCommand("Generates a solution filter (.slnf) from a list of changed files")
        {
            solutionArg,
            outputFileArg,
            changedFilesOption,
            logLevelOption,
        };

        rootCommand.SetAction(async (parseResult, ct) =>
        {
            var solutionFile = parseResult.GetValue(solutionArg)!;
            var changedFilesFile = parseResult.GetValue(changedFilesOption);
            var outputFile = parseResult.GetValue(outputFileArg)!;
            var logLevel = parseResult.GetValue(logLevelOption) ?? "minimal";

            return await RunAsync(
                solutionFile,
                changedFilesFile,
                outputFile,
                logLevel,
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
        CancellationToken ct)
    {
        using var loggerFactory = CreateLoggerFactory(logLevel);
        var logger = loggerFactory.CreateLogger<ProjectResolver>();

        try
        {
            MsBuildBootstrap.Initialize();

            var changedFiles = await ReadChangedFilesAsync(changedFilesFile, Console.In, Console.IsInputRedirected, ct);

            var reader = new MsBuildSolutionReader();
            var resolver = new ProjectResolver(reader, logger);

            var affected = await resolver.ResolveAffectedProjectsAsync(
                solutionFile.FullName, changedFiles, ct);

            var outputDir = Path.GetDirectoryName(Path.GetFullPath(outputFile.FullName))!;
            if (!Directory.Exists(outputDir))
                Directory.CreateDirectory(outputDir);

            var filter = SolutionFilterBuilder.Build(solutionFile.FullName, outputDir, affected);

            await using var writer = new StreamWriter(outputFile.FullName);
            await SolutionFilterWriter.WriteAsync(filter, writer, ct);

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

        return lines
            .Select(l => l.Trim())
            .Where(l => l.Length > 0)
            .Select(l => FilePath.TryFromString(l, out var fp) ? fp.Value : (FilePath?)null)
            .Where(fp => fp is not null)
            .Select(fp => fp!.Value)
            .ToList();
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

    [LoggerMessage(Level = LogLevel.Error, Message = "Cycle failed")]
    private static partial void LogCycleFailed(ILogger logger, Exception ex);
}
