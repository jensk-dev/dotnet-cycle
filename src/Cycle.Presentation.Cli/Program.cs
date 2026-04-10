using System.CommandLine;
using Cycle.Application;
using Cycle.Core;
using Cycle.Infrastructure;
using Microsoft.Extensions.Logging;

namespace Cycle.Presentation.Cli;

public static partial class Program
{
    public static async Task<int> Main(string[] args)
    {
        var (rootCommand, solutionArg, outputFileArg, filesOption, logLevelOption, logFormatOption, noClosureOption, formatOption) =
            CreateRootCommand();

        rootCommand.SetAction(async (parseResult, ct) =>
        {
            var solutionFile = parseResult.GetValue(solutionArg)!;
            var inputFile = parseResult.GetValue(filesOption);
            var outputFile = parseResult.GetValue(outputFileArg)!;
            var logVerbosity = LogVerbosityParser.Parse(parseResult.GetValue(logLevelOption) ?? "minimal");
            var logFormat = LogFormatParser.Parse(parseResult.GetValue(logFormatOption) ?? "text");
            var includeClosure = !parseResult.GetValue(noClosureOption);
            var format = OutputFormatParser.Parse(parseResult.GetValue(formatOption) ?? "slnf");

            return await RunAsync(
                solutionFile,
                inputFile,
                outputFile,
                logVerbosity,
                logFormat,
                includeClosure,
                format,
                ct);
        });

        var parseResult = rootCommand.Parse(args);
        return await parseResult.InvokeAsync();
    }

    private static (RootCommand Root, Argument<FileInfo> Solution, Argument<FileInfo> Output,
        Option<FileInfo?> Files, Option<string> LogLevel, Option<string> LogFormat,
        Option<bool> NoClosure, Option<string> Format)
        CreateRootCommand()
    {
        var solutionArg = new Argument<FileInfo>("solution-path")
        {
            Description = "Path to the solution file (.sln, .slnx, or .slnf)",
        };

        var filesOption = new Option<FileInfo?>("--files")
        {
            Description = "Path to a file containing file paths (one per line)",
        };

        var outputFileArg = new Argument<FileInfo>("output-file")
        {
            Description = "Path to write the output file",
        };

        var logLevelOption = new Option<string>("--log-level")
        {
            Description = "Log verbosity: quiet, minimal, normal, verbose",
            DefaultValueFactory = _ => "minimal",
        };
        logLevelOption.AcceptOnlyFromAmong("quiet", "minimal", "normal", "verbose");

        var logFormatOption = new Option<string>("--log-format")
        {
            Description = "Log format: text (human readable) or json (structured, for CI systems)",
            DefaultValueFactory = _ => "text",
        };
        logFormatOption.AcceptOnlyFromAmong("text", "json");

        var noClosureOption = new Option<bool>("--no-closure")
        {
            Description = "Exclude transitive build dependencies (ProjectReferences) from the filter",
        };

        var formatOption = new Option<string>("--format")
        {
            Description = "Output format: slnf (solution filter) or txt (newline-delimited absolute paths)",
            DefaultValueFactory = _ => "slnf",
        };
        formatOption.AcceptOnlyFromAmong("slnf", "txt");

        var rootCommand = new RootCommand("Generates a solution filter or project list from a list of changed files")
        {
            solutionArg,
            outputFileArg,
            filesOption,
            logLevelOption,
            logFormatOption,
            noClosureOption,
            formatOption,
        };

        return (rootCommand, solutionArg, outputFileArg, filesOption, logLevelOption, logFormatOption, noClosureOption, formatOption);
    }

    internal static async Task<int> RunAsync(
        FileInfo solutionFile,
        FileInfo? inputFile,
        FileInfo outputFile,
        LogVerbosity logVerbosity,
        LogFormat logFormat,
        bool includeClosure,
        OutputFormat format,
        CancellationToken ct)
    {
        using var loggerFactory = CreateLoggerFactory(logVerbosity, logFormat);
        var logger = loggerFactory.CreateLogger(nameof(Program));

        try
        {
            MsBuildBootstrap.Initialize();
            var filesToTrace = await ReadFilesAsync(inputFile, Console.In, Console.IsInputRedirected, logger, ct);
            LogInputFilesRead(logger, filesToTrace.Count);

            var reader = new MsBuildSolutionReader(loggerFactory.CreateLogger<MsBuildSolutionReader>());
            var graphLoader = new MsBuildProjectGraphLoader(loggerFactory);

            var (solutionPath, projectScope) = await ResolveSolutionInputAsync(solutionFile, ct);
            LogProcessing(logger, solutionPath.FilePath.FileName, filesToTrace.Count);

            var streamFactory = new FileOutputStreamFactory();
            IResultWriter resultWriter = format is OutputFormat.Slnf
                ? new SlnfResultWriter(solutionPath, streamFactory)
                : new TxtResultWriter(streamFactory);

            var useCase = new GenerateSolutionFilterUseCase(
                reader, graphLoader, resultWriter,
                loggerFactory.CreateLogger<GenerateSolutionFilterUseCase>());

            var outputFilePath = FilePath.FromString(outputFile.FullName);
            var result = await useCase.ExecuteAsync(
                new GenerateSolutionFilterUseCaseOptions
                {
                    SolutionPath = solutionPath,
                    FilesToTrace = filesToTrace,
                    IncludeClosure = includeClosure,
                    OutputFile = outputFilePath,
                    ProjectScope = projectScope,
                },
                ct);

            foreach (var unresolved in result.UnresolvedReferences)
            {
                LogUnresolvedReference(logger, unresolved.ReferencePath.FullPath, unresolved.ReferencedBy.FullPath);
            }

            SummaryWriter.Write(Console.Out, result, solutionPath, outputFilePath, logVerbosity, filesToTrace.Count);

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

    internal static async Task<IReadOnlyList<FilePath>> ReadFilesAsync(
        FileInfo? inputFile,
        TextReader stdinReader,
        bool isInputRedirected,
        ILogger logger,
        CancellationToken ct)
    {
        IEnumerable<string> lines;

        if (inputFile is not null)
        {
            lines = await File.ReadAllLinesAsync(inputFile.FullName, ct);
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

    internal static ILoggerFactory CreateLoggerFactory(LogVerbosity logVerbosity, LogFormat logFormat)
    {
        var level = logVerbosity switch
        {
            LogVerbosity.Quiet => LogLevel.None,
            LogVerbosity.Minimal => LogLevel.Warning,
            LogVerbosity.Normal => LogLevel.Information,
            LogVerbosity.Verbose => LogLevel.Debug,
            _ => LogLevel.Warning,
        };

        return LoggerFactory.Create(builder =>
        {
            builder.SetMinimumLevel(level);

            if (logFormat is LogFormat.Json)
            {
                builder.AddJsonConsole(options =>
                {
                    options.UseUtcTimestamp = true;
                    options.TimestampFormat = "yyyy-MM-ddTHH:mm:ss.fffZ";
                });
            }
            else
            {
                builder.AddSimpleConsole(options =>
                {
                    options.SingleLine = true;
                    options.UseUtcTimestamp = false;
                });
            }

            builder.AddConsole(options =>
            {
                options.LogToStandardErrorThreshold = LogLevel.Trace;
            });
        });
    }

    private static async Task<(SolutionPath Path, IReadOnlySet<FilePath>? ProjectScope)>
        ResolveSolutionInputAsync(FileInfo solutionFile, CancellationToken ct)
    {
        if (solutionFile.Extension.Equals(".slnf", StringComparison.OrdinalIgnoreCase))
        {
            var slnfPath = FilePath.FromString(solutionFile.FullName);
            var (parentSolution, scope) = await SlnfInputReader.ReadAsync(slnfPath, ct);
            return (parentSolution, scope);
        }

        return (SolutionPath.FromString(solutionFile.FullName), null);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Read {Count} input file(s)")]
    private static partial void LogInputFilesRead(ILogger logger, int count);

    [LoggerMessage(Level = LogLevel.Information, Message = "Processing {SolutionName} with {FileCount} changed file(s)")]
    private static partial void LogProcessing(ILogger logger, string solutionName, int fileCount);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Skipping invalid path: {Path}")]
    private static partial void LogSkippingInvalidPath(ILogger logger, string path);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Unresolved project reference {ReferencePath} referenced by {ProjectPath}")]
    private static partial void LogUnresolvedReference(ILogger logger, string referencePath, string projectPath);

    [LoggerMessage(Level = LogLevel.Error, Message = "Cycle failed")]
    private static partial void LogCycleFailed(ILogger logger, Exception ex);
}
