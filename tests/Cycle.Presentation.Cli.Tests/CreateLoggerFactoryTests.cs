using Microsoft.Extensions.Logging;

namespace Cycle.Presentation.Cli.Tests;

public sealed class CreateLoggerFactoryTests
{
    [Fact]
    public void Quiet_ReturnsFactoryWithNoneLevel()
    {
        using var factory = Program.CreateLoggerFactory(LogVerbosity.Quiet, LogFormat.Text);
        var logger = factory.CreateLogger("Test");

        logger.IsEnabled(LogLevel.Warning).ShouldBeFalse();
        logger.IsEnabled(LogLevel.Critical).ShouldBeFalse();
    }

    [Fact]
    public void Minimal_ReturnsFactoryWithWarningLevel()
    {
        using var factory = Program.CreateLoggerFactory(LogVerbosity.Minimal, LogFormat.Text);
        var logger = factory.CreateLogger("Test");

        logger.IsEnabled(LogLevel.Warning).ShouldBeTrue();
        logger.IsEnabled(LogLevel.Information).ShouldBeFalse();
    }

    [Fact]
    public void Normal_ReturnsFactoryWithInformationLevel()
    {
        using var factory = Program.CreateLoggerFactory(LogVerbosity.Normal, LogFormat.Text);
        var logger = factory.CreateLogger("Test");

        logger.IsEnabled(LogLevel.Information).ShouldBeTrue();
        logger.IsEnabled(LogLevel.Debug).ShouldBeFalse();
    }

    [Fact]
    public void Verbose_ReturnsFactoryWithDebugLevel()
    {
        using var factory = Program.CreateLoggerFactory(LogVerbosity.Verbose, LogFormat.Text);
        var logger = factory.CreateLogger("Test");

        logger.IsEnabled(LogLevel.Debug).ShouldBeTrue();
    }

    [Fact]
    public void JsonFormat_ReturnsWorkingFactory()
    {
        using var factory = Program.CreateLoggerFactory(LogVerbosity.Normal, LogFormat.Json);
        var logger = factory.CreateLogger("Test");

        logger.IsEnabled(LogLevel.Information).ShouldBeTrue();
    }
}
