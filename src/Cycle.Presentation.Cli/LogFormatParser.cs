namespace Cycle.Presentation.Cli;

public static class LogFormatParser
{
    public static LogFormat Parse(string value) => value.ToLowerInvariant() switch
    {
        "text" => LogFormat.Text,
        "json" => LogFormat.Json,
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown log format."),
    };
}
