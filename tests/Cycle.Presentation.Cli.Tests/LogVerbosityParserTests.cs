namespace Cycle.Presentation.Cli.Tests;

public sealed class LogVerbosityParserTests
{
    [Theory]
    [InlineData("quiet", LogVerbosity.Quiet)]
    [InlineData("minimal", LogVerbosity.Minimal)]
    [InlineData("normal", LogVerbosity.Normal)]
    [InlineData("verbose", LogVerbosity.Verbose)]
    public void Parse_WithAcceptedValue_ReturnsExpectedVerbosity(string input, LogVerbosity expected)
    {
        LogVerbosityParser.Parse(input).ShouldBe(expected);
    }

    [Fact]
    public void Parse_WithUnknownValue_ThrowsArgumentOutOfRangeException()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => LogVerbosityParser.Parse("unknown"));
    }
}
