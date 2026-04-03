namespace Cycle.Presentation.Cli.Tests;

public sealed class LogFormatParserTests
{
    [Theory]
    [InlineData("text", LogFormat.Text)]
    [InlineData("json", LogFormat.Json)]
    public void Parse_WithAcceptedValue_ReturnsExpectedFormat(string input, LogFormat expected)
    {
        LogFormatParser.Parse(input).ShouldBe(expected);
    }

    [Fact]
    public void Parse_WithUnknownValue_ThrowsArgumentOutOfRangeException()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => LogFormatParser.Parse("unknown"));
    }
}
