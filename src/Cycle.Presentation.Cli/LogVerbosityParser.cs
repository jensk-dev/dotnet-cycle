namespace Cycle.Presentation.Cli;

public static class LogVerbosityParser
{
    public static LogVerbosity Parse(string value) => value.ToLowerInvariant() switch
    {
        "quiet" => LogVerbosity.Quiet,
        "minimal" => LogVerbosity.Minimal,
        "normal" => LogVerbosity.Normal,
        "verbose" => LogVerbosity.Verbose,
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown log verbosity."),
    };
}
