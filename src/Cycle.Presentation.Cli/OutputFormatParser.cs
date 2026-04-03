using Cycle.Application;

namespace Cycle.Presentation.Cli;

public static class OutputFormatParser
{
    public static OutputFormat Parse(string value) => value.ToLowerInvariant() switch
    {
        "slnf" => OutputFormat.Slnf,
        "txt" => OutputFormat.Txt,
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown output format."),
    };
}
