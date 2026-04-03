using Cycle.Application;

namespace Cycle.Presentation.Cli.Tests;

public sealed class OutputFormatParserTests
{
    [Theory]
    [InlineData("slnf", OutputFormat.Slnf)]
    [InlineData("txt", OutputFormat.Txt)]
    public void Parse_WithAcceptedFormatString_ReturnsExpectedFormat(string input, OutputFormat expected)
    {
        OutputFormatParser.Parse(input).ShouldBe(expected);
    }

    [Fact]
    public void Parse_WithUnknownFormatString_ThrowsArgumentOutOfRangeException()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => OutputFormatParser.Parse("unknown"));
    }
}
